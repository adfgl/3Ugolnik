
using System;
using System.Data;
using System.Runtime.Intrinsics.X86;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CDTISharp.Meshing
{
    public class Mesh
    {
        public readonly static int[] NEXT = [1, 2, 0];
        public readonly static int[] PREV = [2, 0, 1];

        readonly List<Triangle> _triangles;
        readonly QuadTree _qt;
        readonly Rectangle _bounds;

        public List<Triangle> Triangles => _triangles;
        public List<Node> Nodes => _qt.Items;
        public Rectangle Bounds => _bounds;

        public Mesh(Rectangle rectangle)
        {
            _triangles = new List<Triangle>();
            _bounds = rectangle;
            _qt = new QuadTree(rectangle);
        }


        public Node? Add(Node point)
        {
            FindContaining(point, out int trianlge, out int edge, out int node);
            if (trianlge == -1)
            {
                return null;
            }

            if (node != -1)
            {
                return Nodes[node];
            }

            point.Index = _qt.Items.Count;

            Triangle[] triangles;
            if (edge != -1)
            {
                triangles = Split(trianlge, edge, point);
            }
            else
            {
                triangles = Split(trianlge, point);
            }

            // do something before adding?

            _qt.Add(point);
            Add(triangles);
            Legalize(triangles);
            return point;
        }

        public void Add(Node start, Node end)
        {
            Node? a = Add(start);
            Node? b = Add(end);

            if (a is null || b is null || a == b)
            {
                return;
            }

            Queue<(Node, Node)> toInsert = new Queue<(Node, Node)>();
            
        }

        public Triangle EntranceTriangle(int start, int end)
        {
            Node nodeA = Nodes[start];
            Node nodeB = Nodes[end];

            double x = nodeB.X;
            double y = nodeB.Y;

            TriangleWalker walker = new TriangleWalker(_triangles, nodeA.Triangle, nodeA.Index);
            do
            {
                Triangle current = _triangles[walker.Current];
                int e0 = walker.Edge0;
                Node a0 = Nodes[current.indices[e0]];
                Node b0 = Nodes[current.indices[NEXT[e0]]];
                if (GeometryHelper.Cross(a0, b0, x, y) < 0)
                {
                    continue;
                }

                int e1 = walker.Edge1;
                Node a1 = Nodes[current.indices[e1]];
                Node b1 = Nodes[current.indices[NEXT[e1]]];
                if (GeometryHelper.Cross(a1, b1, x, y) < 0)
                {
                    continue;
                }
                return current;
            }
            while (walker.MoveNext());

            throw new Exception("Could not find entrance triangle.");
        }

        public List<int> Legalize(Triangle[] triangles)
        {
            List<int> affected = new List<int>();
            Stack<Triangle> toLegalize = new Stack<Triangle>(triangles);
            while (toLegalize.Count > 0)
            {
                Triangle t = toLegalize.Pop();
                if (!CanFlip(t.index, 0) || !ShouldFlip(t.index, 0))
                {
                    continue;
                }

                Triangle[] flipped = Flip(t.index, 0);
                Add(flipped);

                foreach (Triangle f in flipped)
                {
                    affected.Add(f.index);
                    toLegalize.Push(f);
                }
            }
            return affected;
        }

        public void FindContaining(Node pt, out int triangle, out int edge, out int node, double eps = 1e-6, int searchStart = -1)
        {
            triangle = edge = node = -1;
            if (_triangles.Count == 0)
            {
                return;
            }

            double x = pt.X;
            double y = pt.Y;

            int maxSteps = _triangles.Count * 3;
            int trianglesChecked = 0;

            int skipEdge = -1;
            int current = searchStart == -1 ? _triangles.Count - 1 : searchStart;
            while (true)
            {
                if (trianglesChecked++ > maxSteps)
                {
                    throw new Exception("FindContaining exceeded max steps. Likely invalid topology.");
                }

                Triangle t = _triangles[current];

                int bestExit = -1;
                double worstCross = 0;
                bool inside = true;
                for (int i = 0; i < 3; i++)
                {
                    if (i == skipEdge)
                    {
                        continue;
                    }

                    Node start = Nodes[t.indices[i]];
                    if (GeometryHelper.CloseOrEqual(start, pt, eps))
                    {
                        triangle = current;
                        edge = i;
                        node = start.Index;
                        return;
                    }

                    Node end = Nodes[t.indices[NEXT[i]]];
                    if (GeometryHelper.CloseOrEqual(start, pt, eps))
                    {
                        triangle = current;
                        edge = NEXT[i];
                        node = end.Index;
                        return;
                    }

                    double cross = GeometryHelper.Cross(start, end, x, y);
                    if (Math.Abs(cross) < eps)
                    {
                        double dx = end.X - start.X;
                        double dy = end.Y - start.Y;
                        double dot = (x - start.X) * dx + (y - start.Y) * dy;
                        double lenSq = dx * dx + dy * dy;

                        if (dot >= -eps && dot <= lenSq + eps)
                        {
                            triangle = current;
                            edge = i;
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
                            bestExit = i;
                        }
                    }
                }

                if (inside)
                {
                    triangle = current;
                    edge = node = -1;
                    return;
                }

                int next = t.adjacent[bestExit];
                if (next == -1)
                {
                    triangle = edge = node = -1;
                    return;
                }

                int bestStart = t.indices[bestExit];
                int bestEnd = t.indices[NEXT[bestExit]];

                skipEdge = _triangles[next].IndexOf(bestEnd, bestStart);
                current = next;
            }
        }

        public void FindEdgeBrute(int a, int b, out int triangle, out int edge)
        {
            foreach (Triangle t in _triangles)
            {
                int e = t.IndexOf(a, b);
                if (e != -1)
                {
                    triangle = t.index;
                    edge = e;
                    return;
                }
            }

            triangle = -1;
            edge = -1;
        }

        public void FindEdge(int a, int b, out int triangle, out int edge)
        {
            Node nodeA = Nodes[a];
            TriangleWalker walker = new TriangleWalker(_triangles, nodeA.Triangle, nodeA.Index);

            do
            {
                Triangle t = _triangles[walker.Current];
                triangle = t.index;

                int e0 = walker.Edge0;
                if (t.indices[e0] == a && t.indices[NEXT[e0]] == b)
                {
                    edge = e0;
                    return;
                }

                int e1 = walker.Edge1;
                if (t.indices[e1] == a && t.indices[NEXT[e1]] == b)
                {
                    edge = e1;
                    return;
                }
            }
            while (walker.MoveNext());

            triangle = edge = -1;
        }

        public void Add(Triangle[] tris)
        {
            int n = tris.Length;
            for (int i = 0; i < n; i++)
            {
                Triangle t = tris[i];

                int index = t.index;
                if (index < _triangles.Count)
                {
                    SetConnectivity(_triangles[index], false);
                    _triangles[index] = t;
                }
                else
                {
                    _triangles.Add(t);
                }
            }

            for (int i = 0; i < n; i++)
            {
                SetConnectivity(tris[i], true);
            }
        }

        void SetConnectivity(Triangle t, bool connect)
        {
            int ti = t.index;
            for (int i = 0; i < 3; i++)
            {
                int start = t.indices[i];
                Node origin = Nodes[start];

                if (connect)
                {
                    if (origin.Triangle == -1)
                    {
                        origin.Triangle = ti;
                    }
                }
                else
                {
                    if (origin.Triangle == ti)
                    {
                        origin.Triangle = -1;
                    }
                }

                int adjIndex = t.adjacent[i];
                if (adjIndex == -1) continue;

                Triangle adj = _triangles[adjIndex];
                int end = t.indices[NEXT[i]];
                int twin = adj.IndexOf(end, start);

                if (twin == -1) continue;

                if (connect)
                {
                    if (adj.adjacent[twin] == -1)
                    {
                        adj.adjacent[twin] = ti;
                    }
                }
                else
                {
                    if (adj.adjacent[twin] == ti)
                    {
                        adj.adjacent[twin] = -1;
                    }
                }
            }
        }


        public bool ShouldFlip(int triangle, int edge)
        {
            Triangle t0 = _triangles[triangle];
            int adjIndex = t0.adjacent[edge];
            Triangle t1 = _triangles[adjIndex];

            Node a = Nodes[t0.indices[0]];
            Node b = Nodes[t0.indices[1]];
            int twin = t1.IndexOf(b.Index, a.Index);

            Node d = Nodes[t1.indices[PREV[twin]]];
            return t0.circle.Contains(d.X, d.Y);
        }

        public bool CanFlip(int triangle, int edge)
        {
            /*
                           c           
                           /\          
                          /  \         
                         /    \        
                        /      \       
                       /   t0   \      
                      /          \     
                     /            \     
                  a +--------------+ b 
                     \            /    
                      \          /     
                       \   t1   /      
                        \      /       
                         \    /        
                          \  /         
                           \/          
                            d          
             */


            Triangle t0 = _triangles[triangle];
            if (t0.constraints[edge] != -1)
            {
                return false;
            }
            
            int adjIndex = t0.adjacent[edge];
            if (adjIndex == -1)
            {
                return false;
            }

            Triangle t1 = _triangles[adjIndex];

            Node a = Nodes[t0.indices[0]];
            Node b = Nodes[t0.indices[1]];
            Node c = Nodes[t0.indices[2]];

            int twin = t1.IndexOf(b.Index, a.Index);
            Node d = Nodes[t1.indices[PREV[twin]]];

            return GeometryHelper.QuadConvex(b, c, a, d);
        }

        public Triangle[] Flip(int triangle, int edge)
        {
            Triangle old0 = _triangles[triangle];
            int adjIndex = old0.adjacent[edge];
            Triangle old1 = _triangles[adjIndex];

            /*
                  d - is inserted point, we want to propagate flip away from it, otherwise we 
                  are risking ending up in flipping degeneracy
                       c                          c
                       /\                        /|\
                      /  \                      / | \
                     /    \                    /  |  \
                    /      \                  /   |   \ 
                   /   t0   \                /    |    \
                  /          \              /     |     \ 
                 /            \            /      |      \
              a +--------------+ b      a +   t0  |  t1   + b
                 \            /            \      |      /
                  \          /              \     |     /
                   \   t1   /                \    |    /
                    \      /                  \   |   / 
                     \    /                    \  |  /
                      \  /                      \ | /
                       \/                        \|/
                        d                         d
            */

            Node a = Nodes[old0.indices[0]];
            Node b = Nodes[old0.indices[1]];
            Node c = Nodes[old0.indices[2]];

            int twin = old1.IndexOf(b.Index, a.Index);

            Node d = Nodes[old1.indices[PREV[twin]]];

            int t0 = old0.index;
            int t1 = old1.index;

            int bc = NEXT[edge];
            int ca = PREV[edge];
            int ad = NEXT[twin];
            int db = PREV[twin];

            Triangle new0 = new Triangle(t0, a, d, c);
            new0.adjacent[0] = old1.adjacent[ad];
            new0.adjacent[1] = t1;
            new0.adjacent[2] = old0.adjacent[ca];

            new0.constraints[0] = old1.constraints[ad];
            new0.constraints[1] = -1;
            new0.constraints[2] = old0.constraints[ca];

            Triangle new1 = new Triangle(t1, d, b, c);
            new1.adjacent[0] = old1.adjacent[db];
            new1.adjacent[1] = old0.adjacent[bc];
            new1.adjacent[2] = t0;

            new1.constraints[0] = old1.constraints[db];
            new1.constraints[1] = old0.constraints[bc];
            new1.constraints[2] = -1;

            return [new0, new1];
        }

        public Triangle[] Split(int triangle, int edge, Node node)
        {
            Triangle old0 = _triangles[triangle];
            int constraint = old0.constraints[edge];

            Node a = Nodes[old0.indices[0]];
            Node b = Nodes[old0.indices[1]];
            Node c = Nodes[old0.indices[2]];
            Node e = node;

            int bc = NEXT[edge];
            int ca = PREV[edge];

            int adjIndex = old0.adjacent[edge];

            Triangle[] triangles;
            if (adjIndex == -1)
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
                                                          e
                 */

                int t0 = triangle;
                int t1 = _triangles.Count;

                Triangle new0 = new Triangle(t0, c, a, e);
                new0.adjacent[0] = old0.adjacent[ca];
                new0.adjacent[1] = -1;
                new0.adjacent[2] = t1;

                new0.constraints[0] = old0.constraints[ca];
                new0.constraints[1] = constraint;
                new0.constraints[2] = -1;

                Triangle new1 = new Triangle(t1, b, c, e);
                new1.adjacent[0] = old0.adjacent[bc];
                new1.adjacent[1] = t0;
                new1.adjacent[2] = -1;

                new1.constraints[0] = old0.constraints[bc];
                new1.constraints[1] = -1;
                new1.constraints[2] = constraint;

                triangles = [new0, new1];
            }
            else
            {
                Triangle old1 = _triangles[adjIndex];


                /*
                         c                            c            
                         /\                          /|\             
                        /  \                        / | \           
                       /    \                      /  |  \          
                      /      \                    /   |   \       
                     /   f0   \                  /    |    \        
                    /          \                / f0  |  f1 \       
                   /            \              /      |      \      
                a +--------------+ b        a +-------e-------+ b  
                   \            /              \      |      /      
                    \          /                \ f3  |  f2 /       
                     \   f1   /                  \    |    /        
                      \      /                    \   |   /      
                       \    /                      \  |  /          
                        \  /                        \ | /           
                         \/                          \|/            
                         d                            d            
             */

                int twin = old1.IndexOf(b.Index, a.Index);
                Node d = Nodes[old1.indices[PREV[twin]]];

                int ad = NEXT[edge];
                int db = PREV[edge];

                int t0 = old0.index;
                int t1 = old1.index;
                int t2 = _triangles.Count;
                int t3 = t2 + 1;

                Triangle new0 = new Triangle(t0, c, a, e);
                new0.adjacent[0] = old0.adjacent[ca];
                new0.adjacent[1] = t3;
                new0.adjacent[2] = t1;

                new0.constraints[0] = old0.constraints[ca];
                new0.constraints[1] = constraint;
                new0.constraints[2] = -1;

                Triangle new1 = new Triangle(t1, b, c, e);
                new1.adjacent[0] = old0.adjacent[bc];
                new1.adjacent[1] = t0;
                new1.adjacent[2] = t2;

                new1.constraints[0] = old0.constraints[bc];
                new1.constraints[1] = -1;
                new1.constraints[2] = constraint;

                Triangle new2 = new Triangle(t2, d, b, e);
                new2.adjacent[0] = old1.adjacent[db];
                new2.adjacent[1] = t1;
                new2.adjacent[2] = t3;

                new2.constraints[0] = old0.constraints[db];
                new2.constraints[1] = constraint;
                new2.constraints[2] = -1;

                Triangle new3 = new Triangle(t3, a, d, e);
                new3.adjacent[0] = old1.adjacent[ad];
                new3.adjacent[1] = t2;
                new3.adjacent[2] = t0;

                new3.constraints[0] = old0.constraints[ad];
                new3.constraints[1] = -1;
                new3.constraints[2] = constraint;

                triangles = [new0, new1, new2, new3];
            }
            return triangles;
        }

        public Triangle[] Split(int triangle, Node node)
        {
            /*
                         *C



                    /           ^
                   ^      *D     \



               A*        ->         *B

            */

            Triangle old = _triangles[triangle];
            Node a = Nodes[old.indices[0]];
            Node b = Nodes[old.indices[1]];
            Node c = Nodes[old.indices[2]];
            Node d = node;

            int t0 = triangle;
            int t1 = _triangles.Count;
            int t2 = t1 + 1;

            Triangle new0 = new Triangle(t0, a, b, d);
            new0.adjacent[0] = old.adjacent[0];
            new0.adjacent[1] = t1;
            new0.adjacent[2] = t2;

            new0.constraints[0] = old.constraints[0];

            Triangle new1 = new Triangle(t1, b, c, d);
            new1.adjacent[0] = old.adjacent[1];
            new1.adjacent[1] = t2;
            new1.adjacent[2] = t0;

            new1.constraints[0] = old.constraints[1];

            Triangle new2 = new Triangle(t2, b, c, d);
            new2.adjacent[0] = old.adjacent[2];
            new2.adjacent[1] = t0;
            new2.adjacent[2] = t1;

            new2.constraints[0] = old.constraints[2];

            return [new0, new1, new2];
        }
    }

    public struct TriangleWalker
    {
        readonly List<Triangle> _triangles;
        readonly int _start, _vertex;
        int _current, _edge0, _edge1;

        public TriangleWalker(List<Triangle> triangles, int triangleIndex, int globalVertexIndex)
        {
            _triangles = triangles;
            _vertex = globalVertexIndex;
            _current = _start = triangleIndex;

            _edge0 = _triangles[_current].IndexOf(_vertex);
            _edge1 = Mesh.PREV[_edge0];
        }

        public int Current => _current;
        public int Edge0 => _edge0;
        public int Edge1 => _edge1;

        public bool MoveNext()
        {
            Triangle tri = _triangles[_current];
            int next = tri.adjacent[_edge0];
            if (next == _start || next == -1)
            {
                return false;
            }

            _current = next;
            Triangle nextTri = _triangles[_current];
            _edge0 = nextTri.IndexOf(_vertex);
            _edge1 = Mesh.PREV[_edge0];
            return true;
        }
    }
}
