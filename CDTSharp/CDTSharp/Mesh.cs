using System;

namespace CDTSharp
{
    public class TopologyChange
    {
        public Triangle[] Triangles { get; set; } = Array.Empty<Triangle>();
        public TriangleEdge[] Edges {  get; set; } = Array.Empty<TriangleEdge>();
    }

    public class Mesh
    {
        public const int SUPER_INDEX = 3;
        
        public readonly static int[] NEXT = [1, 2, 0], PREV = [2, 0, 1];

        readonly List<Node> _nodes = new List<Node>();
        readonly List<Triangle> _triangles = new List<Triangle>();

        public List<Triangle> Triangles => _triangles;
        public List<Node> Nodes => _nodes;

        public bool CanFlip(Triangle triangle, int edge)
        {
            if (triangle.constrained[edge] || triangle.adjacent[edge] == -1)
            {
                return false;
            }

            int[] points = Quad(triangle, edge, out _);
            return GeometryHelper.QuadConvex(
                _nodes[points[0]], 
                _nodes[points[1]], 
                _nodes[points[2]],
                _nodes[points[3]]);
        }

        public void Legalize(List<int> affected, TopologyChange change)
        {
            Stack<TriangleEdge> toLegalize = new Stack<TriangleEdge>(change.Edges);

            while (toLegalize.Count > 0)
            {
                TriangleEdge te = toLegalize.Pop();
                if (!te.shouldLegalize)
                {
                    continue;
                }

                Triangle t = _triangles[te.triangle];
                TopologyChange flipped = Flip(t, te.edge);

                affected.Add(t.index);
                foreach (TriangleEdge item in flipped.Edges)
                {
                    toLegalize.Push(item);
                }
            }
        }

        public bool ShouldFlip(Triangle triangle, int edge)
        {
            triangle.Edge(edge, out int a, out int b);

            Triangle adj = _triangles[triangle.adjacent[edge]];
            int adjEdge = adj.IndexOf(b, a);

            int oppositeIndex = adj.indices[PREV[adjEdge]];
            Node opposite = _nodes[oppositeIndex];
            return triangle.circle.Contains(opposite.X, opposite.Y);
        }


        public void FindContaining(double x, double y, out int triangle, out int edge, out int node, double eps = 1e-6, int searchStart = -1)
        {
            triangle = edge = node = -1;
            if (_triangles.Count == 0)
            {
                return;
            }

            int maxSteps = _triangles.Count * 3;
            int trianglesChecked = 0;

            Node pt = new Node() { X = x, Y = y };

            int skipEdge = -1;
            int current = searchStart == -1 ? _triangles.Count - 1 : searchStart;

            while (true)
            {
                if (trianglesChecked++ > maxSteps)
                {
                    throw new Exception("FindContaining exceeded max steps. Likely invalid topology.");
                }

                int bestExit = -1;
                double worstCross = 0;
                bool inside = true;

                Triangle currentTriangle = _triangles[current];
                for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    if (edgeIndex == skipEdge)
                    {
                        continue;
                    }

                    int aIndex = currentTriangle.indices[edgeIndex];
                    Node a = _nodes[aIndex];
                    double adx = x - a.X;
                    double ady = y - a.Y;
                    if (adx * adx + ady * ady < eps)
                    {
                        triangle = current;
                        edge = edgeIndex;
                        node = a.Index;
                        return;
                    }

                    int bIndex = currentTriangle.indices[NEXT[edgeIndex]];
                    Node b = _nodes[bIndex];
                    double bdx = x - b.X;
                    double bdy = y - b.Y;
                    if (bdx * bdx + bdy * bdy < eps)
                    {
                        triangle = current;
                        edge = NEXT[edgeIndex];
                        node = b.Index;
                        return;
                    }

                    double cross = GeometryHelper.Cross(a, b, pt);
                    if (Math.Abs(cross) < eps)
                    {
                        double dx = b.X - a.X;
                        double dy = b.Y - a.Y;
                        double dot = adx * dx + ady * dy;
                        double lenSq = dx * dx + dy * dy;

                        if (dot >= -eps && dot <= lenSq + eps)
                        {
                            triangle = current;
                            edge = edgeIndex;
                            node = -1;
                            return;
                        }
                    }

                    if (cross < 0)
                    {
                        inside = false;
                        if (bestExit == -1 || cross < worstCross)
                        {
                            worstCross = cross;
                            bestExit = edgeIndex;
                        }
                    }
                }

                if (inside)
                {
                    triangle = current;
                    edge = node = -1;
                    return;
                }

                int next = currentTriangle.adjacent[bestExit];
                if (next == -1)
                {
                    triangle = edge = node = -1;
                    return;
                }

                currentTriangle.Edge(bestExit, out int aStart, out int bEnd);
                skipEdge = _triangles[next].IndexOf(bEnd, aStart);
                current = next;
            }
        }

        public void SetConstraint(int triangle, int edge, bool value)
        {
            Triangle t = _triangles[triangle];
            t.constrained[edge] = value;

            int adjIndex = t.adjacent[edge];
            if (adjIndex == -1)
            {
                return;
            }

            Triangle adj = _triangles[adjIndex];
            t.Edge(edge, out int a, out int b);
            adj.constrained[adj.IndexOf(b, a)] = value;
        }

        public TriangleEdge FindEdge(int a, int b)
        {
            Node nodeA = _nodes[a];
            TriangleWalker walker = new TriangleWalker(_triangles, nodeA.Triangle, nodeA.Index);
            do
            {
                int triIndex = walker.Current;
                int e = _triangles[triIndex].IndexOf(a, b);
                if (e != -1)
                {
                    return new TriangleEdge(triIndex, e);
                }
            }
            while (walker.MoveNextCW());

            return new TriangleEdge();
        }

        public TriangleEdge FindEdgeBrute(int a, int b)
        {
            foreach (Triangle t in _triangles)
            {
                int e = t.IndexOf(a, b);
                if (e != -1)
                {
                    return new TriangleEdge(t.index, e);
                }
            }
            return new TriangleEdge();
        }

        public int[] Quad(Triangle triangle, int edge, out int twinEdge)
        {
            int adj = triangle.adjacent[edge];
            if (adj == -1)
            {
                twinEdge = -1;
                return [-1, -1, -1, -1];
            }
            /*
                     d             
                     /\            
                    /  \           
                   /    \          
                  /      \         
                 /        \        
                /          \       
               /            \      
            a +--------------+ c   
               \            /      
                \          /       
                 \        /        
                  \      /         
                   \    /          
                    \  /           
                     \/            
                     b             
         */

            Triangle acd = triangle;
            int a = acd.indices[edge];
            int c = acd.indices[NEXT[edge]];
            int d = acd.indices[PREV[edge]];

            Triangle cab = _triangles[adj];
            twinEdge = cab.IndexOf(c, a);
            int b = cab.indices[PREV[twinEdge]];

            return [a, b, c, d];
        }

        public void AddOrUpdate(TopologyChange change)
        {
            foreach (Triangle t in change.Triangles)
            {
                int index = t.index;
                if (index < _triangles.Count)
                {
                    _triangles[index] = t;
                }
                else
                {
                    _triangles.Add(t);
                }
            }

            foreach (TriangleEdge te in change.Edges)
            {
                Triangle t = _triangles[te.triangle];

                int adjIndex = t.adjacent[te.edge];
                if (adjIndex == -1)
                {
                    continue;
                }

                Triangle adj = _triangles[adjIndex];
                t.Edge(te.edge, out int a, out int b);
                adj.adjacent[adj.IndexOf(b, a)] = t.index;
            }
        }

        public TopologyChange Flip(Triangle triangle, int edge)
        {
            int adj = triangle.adjacent[edge];
            if (adj == -1)
            {
                throw new Exception("Can't flip edge with not twin");
            }

            /*
              b - is inserted point, we want to propagate flip away from it, otherwise we 
              are risking ending up in flipping degeneracy
                           d                          d
                           /\                        /|\
                          /  \                      / | \
                         /    \                    /  |  \
                        /      \                  /   |   \ 
                       /   t0   \                /    |    \
                      /          \              /     |     \ 
                     /            \            /      |      \
                  a +--------------+ c      a +   t0  |  t1   + c
                     \            /            \      |      /
                      \          /              \     |     /
                       \   t1   /                \    |    /
                        \      /                  \   |   / 
                         \    /                    \  |  /
                          \  /                      \ | /
                           \/                        \|/
                            b                         b
             */

            Triangle acd = triangle;
            Triangle cab = _triangles[adj];

            bool constrained = acd.constrained[edge];

            int[] abcd = Quad(triangle, edge, out int edgeTwin);
            Node a = _nodes[abcd[0]];
            Node b = _nodes[abcd[1]];
            Node c = _nodes[abcd[2]];
            Node d = _nodes[abcd[3]];

            int t0 = acd.index;
            int t1 = cab.index;

            List<int> parents = new List<int>(acd.parents);
            foreach (int item in cab.parents)
            {
                if (!parents.Contains(item))
                {
                    parents.Add(item);
                }
            }


            Triangle bda = new Triangle(t0, b, d, a, null, parents);
            Triangle dbc = new Triangle(t1, d, b, c, acd.area + cab.area - bda.area, parents);

            int cd = NEXT[edge];
            int da = PREV[edge];
            int ab = NEXT[edgeTwin];
            int bc = PREV[edgeTwin];

            bda.adjacent[0] = t1;
            bda.adjacent[1] = acd.adjacent[da];
            bda.adjacent[2] = cab.adjacent[ab];

            dbc.adjacent[0] = t0;
            dbc.adjacent[1] = cab.adjacent[bc];
            dbc.adjacent[2] = acd.adjacent[cd];

            // arguable by nature.. but in case of forced flip should do
            bda.constrained[0] = dbc.constrained[0] = constrained; 

            bda.constrained[1] = acd.constrained[da];
            bda.constrained[2] = cab.constrained[ab];

            dbc.constrained[1] = cab.constrained[bc];
            dbc.constrained[2] = acd.constrained[cd];

            a.Triangle = d.Triangle = b.Triangle = t0;
            c.Triangle = t1;

            return new TopologyChange()
            {
                Triangles = [bda, dbc],
                Edges = [
                    new TriangleEdge(t0, 1, false),
                    new TriangleEdge(t0, 2, true),
                    new TriangleEdge(t1, 1, true),
                    new TriangleEdge(t1, 2, false)
                ],
            };
        }

        public TopologyChange Split(Triangle triangle, int edge, Node node)
        {
            int adj = triangle.adjacent[edge];
            if (adj == -1)
            {
                return SplitNoAdjacent(triangle, edge, node);
            }

            /*
                      d                            d            
                      /\                          /|\             
                     /  \                        / | \           
                    /    \                      /  |  \          
                   /      \                    /   |   \       
                  /   f0   \                  /    |    \        
                 /          \                / f0  |  f1 \       
                /            \              /      |      \      
             a +--------------+ c        a +-------e-------+ c  
                \            /              \      |      /      
                 \          /                \ f3  |  f2 /       
                  \   f1   /                  \    |    /        
                   \      /                    \   |   /      
                    \    /                      \  |  /          
                     \  /                        \ | /           
                      \/                          \|/            
                      b                            b            
            */

            bool constrained = triangle.constrained[edge];
            Triangle acd = triangle;
            Triangle cab = _triangles[adj];

            int t0 = triangle.index;
            int t1 = _triangles.Count;
            int t2 = _triangles.Count + 1;
            int t3 = adj;

            int[] abcd = Quad(triangle, edge, out int twinEdge);
            Node a = _nodes[abcd[0]];
            Node b = _nodes[abcd[1]];
            Node c = _nodes[abcd[2]];
            Node d = _nodes[abcd[3]];
            Node e = node;

            Triangle eda = new Triangle(t0, e, d, a, null, acd.parents);
            Triangle ecd = new Triangle(t1, e, c, d, acd.area - eda.area, acd.parents);
            Triangle ebc = new Triangle(t2, e, b, c, null, cab.parents);
            Triangle eab = new Triangle(t3, e, a, b, cab.area - ebc.area, cab.parents);

            int ac = edge;
            int cd = NEXT[ac];
            int da = PREV[ac];

            int ca = twinEdge;
            int ab = NEXT[ca];
            int bc = PREV[ca];

            eda.adjacent[0] = t1;
            eda.adjacent[1] = acd.adjacent[da];
            eda.adjacent[2] = t3;

            ecd.adjacent[0] = t2;
            ecd.adjacent[1] = acd.adjacent[cd];
            ecd.adjacent[2] = t0;

            ebc.adjacent[0] = t3;
            ebc.adjacent[1] = cab.adjacent[bc];
            ebc.adjacent[2] = t1;

            eab.adjacent[0] = t0;
            eab.adjacent[1] = cab.adjacent[ab];
            eab.adjacent[2] = t2;

            eda.constrained[1] = acd.constrained[da];
            eda.constrained[2] = constrained;

            ecd.constrained[0] = constrained;
            ecd.constrained[1] = acd.constrained[cd];

            ebc.constrained[1] = cab.constrained[bc];
            ebc.constrained[2] = constrained;

            eab.constrained[0] = constrained;
            eab.constrained[1] = cab.constrained[ab];

            a.Triangle = d.Triangle = e.Triangle = t0;
            c.Triangle = t1;
            b.Triangle = t2;

            return new TopologyChange()
            {
                Triangles = [eda, ecd, ebc, eab],
                Edges = [
                    new TriangleEdge(t0, 1, true), 
                    new TriangleEdge(t1, 1, true), 
                    new TriangleEdge(t2, 1, true), 
                    new TriangleEdge(t3, 1, true)
                ],
            };
        }

        public TopologyChange SplitNoAdjacent(Triangle triangle, int edge, Node node)
        {
            /*
                       c                            c        
                       /\                          /|\        
                      /  \                        / | \       
                     /    \                      /  |  \      
                    /      \                    /   |   \      
                   /  t0    \                  /    |    \    
                  /          \                /     |     \   
                 /            \              /  t0  |  t1  \  
              a +--------------+ b        a +-------+-------+ b
                                                    d
           */

            Triangle abc = triangle;

            Node a = _nodes[triangle.indices[0]];
            Node b = _nodes[triangle.indices[1]];
            Node c = _nodes[triangle.indices[2]];
            Node d = node;

            int t0 = triangle.index;
            int t1 = _triangles.Count;

            List<int> parents = triangle.parents;
            Triangle dca = new Triangle(t0, d, c, a, null, parents);
            Triangle cdb = new Triangle(t1, c, d, b, abc.area - dca.area, parents);

            dca.adjacent[0] = t1;
            dca.adjacent[1] = abc.adjacent[2];
            dca.adjacent[2] = -1;

            cdb.adjacent[0] = t0;
            cdb.adjacent[1] = -1;
            cdb.adjacent[2] = abc.adjacent[1];

            dca.constrained[1] = abc.constrained[2];
            cdb.constrained[2] = abc.constrained[1];

            bool constrained = abc.constrained[0];
            dca.constrained[2] = cdb.constrained[1] = constrained;

            a.Triangle = c.Triangle = d.Triangle = t0;
            b.Triangle = t1;

            return new TopologyChange()
            {
                Triangles = [dca, cdb],
                Edges = [
                    new TriangleEdge(t0, 1, true), 
                    new TriangleEdge(t1, 2, true)
                ],
            };
        }

        public TopologyChange Split(Triangle triangle, Node node)
        {
            /*
                         *C
                        


                    /           ^
                   ^      *D     \

                
              
               A*        ->         *B
              
             */

            Triangle abc = triangle;
            Node a = _nodes[abc.indices[0]];
            Node b = _nodes[abc.indices[1]];
            Node c = _nodes[abc.indices[2]];
            Node d = node;

            int t0 = abc.index;
            int t1 = _triangles.Count;
            int t2 = t1 + 1;

            List<int> parents = abc.parents;
            Triangle dab = new Triangle(t0, d, a, b, null, parents);
            Triangle dbc = new Triangle(t1, d, b, c, null, parents);
            Triangle dca = new Triangle(t2, d, c, a, abc.area - dab.area - dbc.area, parents);

            dab.adjacent[0] = t2;
            dab.adjacent[1] = abc.adjacent[0];
            dab.adjacent[2] = t1;

            dbc.adjacent[0] = t0;
            dbc.adjacent[1] = abc.adjacent[1];
            dbc.adjacent[2] = t2;

            dca.adjacent[0] = t1;
            dca.adjacent[1] = abc.adjacent[2];
            dca.adjacent[2] = t0;

            dab.constrained[1] = abc.constrained[0];
            dbc.constrained[1] = abc.constrained[1];
            dca.constrained[1] = abc.constrained[2];

            a.Triangle = d.Triangle = t0;
            b.Triangle = t1;
            c.Triangle = t2;

            return new TopologyChange()
            {
                Triangles = [dab, dbc, dca],
                Edges = [
                    new TriangleEdge(t0, 1, true),
                    new TriangleEdge(t1, 1, true),
                    new TriangleEdge(t2, 1, true)
                ],
            };
        }
    }
}
