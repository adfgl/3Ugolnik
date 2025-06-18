namespace CDTlib
{
    public readonly struct Affected
    {
        public readonly Triangle triangle;
        public readonly int edge;

        public Affected(Triangle triangle, int edge)
        {
            this.triangle = triangle;
            this.edge = edge;
        }
    }

    public class Mesh
    {
        public readonly static int[] NEXT = [1, 2, 0], PREV = [2, 0, 1];

        readonly List<Node> _nodes = new List<Node>();
        readonly List<Triangle> _triangles = new List<Triangle>();

        public List<Triangle> Triangles => _triangles;
        public List<Node> Nodes => _nodes;

        public Mesh AddSuperStructure(Rectangle bounds)
        {
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double size = 2 * dmax;

            Node a = new Node(-3, midx - size, midy - size);
            Node b = new Node(-2, midx + size, midy - size);
            Node c = new Node(-1, midx, midy + size);

            Triangle triangle = new Triangle(0, a, b, c, Area(a, b, c));
            _triangles.Add(triangle);

            a.Triangle = b.Triangle = c.Triangle = triangle.index;
            return this;
        }

        static double Area(Node a, Node b, Node c)
        {
            double area = Node.Cross(a, b, c) * 0.5;
            if (area <= 0)
            {
                throw new Exception("Invalid triangle");
            }
            return area;
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

                    double cross = Node.Cross(a, b, pt);
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

                Triangle nextTriangle = _triangles[next];
                int aStart = currentTriangle.indices[bestExit];
                int bEnd = currentTriangle.indices[NEXT[bestExit]];
                skipEdge = nextTriangle.IndexOf(bEnd, aStart);
                current = next;
            }
        }

        public void FindEdgeBrute(int a, int b, out int triangle, out int edge)
        {
            for (int i = 0; i < _triangles.Count; i++)
            {
                int e = _triangles[i].IndexOf(a, b);
                if (e != -1)
                {
                    triangle = i;
                    edge = e;
                    return;
                }
            }

            triangle = -1;
            edge = -1;
        }

        public void FindEdge(int a, int b, out int triangle, out int edge)
        {
            Node nodeA = _nodes[a];
            TriangleWalker walker = new TriangleWalker(_triangles, nodeA.Triangle, nodeA.Index);
            do
            {
                int current = walker.Current;
                int e = _triangles[current].IndexOf(a, b);
                if (e != -1)
                {
                    triangle = current;
                    edge = e;
                    return;
                }
                    
            }
            while (walker.MoveNextCW());

            triangle = -1;
            edge = -1;
        }

        public void Add(Affected[] tris)
        {
            foreach (Affected item in tris)
            {
                Triangle triangle = item.triangle;
                int edge = item.edge;

                int index = triangle.index;
                int adj = triangle.adjacent[edge];
                if (adj != -1)
                {
                    int start = triangle.indices[edge];
                    int end = triangle.indices[NEXT[edge]];
                    Triangle twin = _triangles[adj];
                    twin.adjacent[twin.IndexOf(end, start)] = index;
                }

                if (index < _triangles.Count)
                {
                    _triangles[index] = triangle;
                }
                else
                {
                    _triangles.Add(triangle);
                }
            }
        }

        public Affected[] Split(Triangle triangle, Node node)
        {
            Triangle abc = triangle;

            Node a = _nodes[abc.indices[0]];
            Node b = _nodes[abc.indices[1]];
            Node c = _nodes[abc.indices[2]];
            Node d = node;

            int t0 = abc.index;
            int t1 = _triangles.Count;
            int t2 = t1 + 1;

            Triangle dab = new Triangle(t0, d, a, b, Area(d, a, b));
            Triangle dbc = new Triangle(t1, d, b, c, Area(d, b, c));
            Triangle dca = new Triangle(t2, d, c, a, abc.area - dab.area - dbc.area);

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

            return [new Affected(dab, 1), new Affected(dbc, 1), new Affected(dca, 1)];
        }

        public Affected[] Split(Triangle triangle, int edge, Node node)
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

            Triangle acd = triangle;
            Triangle cab = _triangles[adj];

            int t0 = triangle.index;
            int t1 = adj;
            int t2 = _triangles.Count;
            int t3 = _triangles.Count + 1;

            int ac = edge;
            int cd = NEXT[ac];
            int da = PREV[ac];

            int ca = cab.IndexOf(cd, ac);
            int ab = NEXT[ca];
            int bc = PREV[ca];

            Node a = _nodes[acd.indices[ac]];
            Node b = _nodes[cab.indices[bc]];
            Node c = _nodes[acd.indices[cd]];
            Node d = _nodes[acd.indices[da]];
            Node e = node;

            Triangle eda = new Triangle(t0, e, d, a, Area(e, d, a));
            Triangle ecd = new Triangle(t1, e, c, d, acd.area - eda.area);
            Triangle ebc = new Triangle(t2, e, b, c, Area(e, b, c));
            Triangle eab = new Triangle(t3, e, a, b, cab.area - ebc.area);

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

            bool constrained = acd.constrained[ca];
            eda.constrained[2] = ecd.constrained[0] = ebc.constrained[2] = eab.constrained[0] = constrained;

            eda.constrained[1] = acd.constrained[da];
            ecd.constrained[1] = acd.constrained[cd];
            ebc.constrained[1] = cab.constrained[bc];
            eab.constrained[1] = cab.constrained[ab];

            a.Triangle = d.Triangle = e.Triangle = t0;
            c.Triangle = t1;
            b.Triangle = t2;

            return [new Affected(eda, 1), new Affected(ecd, 1), new Affected(ebc, 1), new Affected(eab, 1)];
        }

        Affected[] SplitNoAdjacent(Triangle triangle, int edge, Node node)
        {
            /*
                         c                            c        
                         /\                          /|\        
                        /  \                        / | \       
                       /    \                      /  |  \      
                      /      \                    /   |   \      
                     /  f0    \                  /    |    \    
                    /          \                /     |     \   
                   /            \              /  f0  |  f1  \  
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

            Triangle dca = new Triangle(t0, d, c, a, Area(d, c, a));
            Triangle cdb = new Triangle(t1, c, d, b, abc.area - dca.area);

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

            return [new Affected(dca, 1), new Affected(cdb, 2)];
        }

        public Affected[] Flip(Triangle triangle, int edge)
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
            Triangle cba = _triangles[adj];

            int ac = edge;
            int cd = NEXT[ac];
            int da = PREV[ac];

            int ca = cba.IndexOf(cd, ac);
            int ab = NEXT[ca];
            int bc = PREV[ca];

            Node a = _nodes[acd.indices[ac]];
            Node b = _nodes[cba.indices[bc]];
            Node c = _nodes[acd.indices[cd]];
            Node d = _nodes[acd.indices[da]];

            int t0 = acd.index;
            int t1 = cba.index;

            Triangle bda = new Triangle(t0, b, d, a, Area(b, d, a));
            Triangle dbc = new Triangle(t1, d, b, c, acd.area + cba.area - bda.area);

            bda.adjacent[0] = t1;
            bda.adjacent[1] = acd.adjacent[da];
            bda.adjacent[2] = cba.adjacent[ab];

            dbc.adjacent[0] = t0;
            dbc.adjacent[1] = cba.adjacent[bc];
            dbc.adjacent[2] = acd.adjacent[cd];

            bool constrained = cba.constrained[ac];
            bda.constrained[0] = dbc.constrained[1] = constrained;

            a.Triangle = b.Triangle = c.Triangle = t0;
            c.Triangle = t1;

            return [new Affected(bda, 2), new Affected(dbc, 1)];
        }
    }

    public struct TriangleWalker
    {
        readonly List<Triangle> _triangles;
        readonly int _start;
        readonly int _vertex;
        int _current;

        public int Current => _current;

        public TriangleWalker(List<Triangle> triangles, int triangleIndex, int globalVertexIndex)
        {
            _triangles = triangles;
            _vertex = globalVertexIndex;
            _current = _start = triangleIndex;
        }

        public bool MoveNextCCW()
        {
            Triangle tri = _triangles[_current];
            int next = tri.adjacent[tri.IndexOf(_vertex)];
            if (next == _start) return false;
            _current = next;
            return true;
        }

        public bool MoveNextCW()
        {
            Triangle tri = _triangles[_current];
            int indexOfVertex = tri.IndexOf(_vertex);
            if (indexOfVertex == -1)
            {
                return false;
            }

            int next = tri.adjacent[Mesh.PREV[indexOfVertex]];
            if (next == _start) return false;
            _current = next;
            return true;
        }
    }
}
