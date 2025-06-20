﻿using System;
using System.Text;

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

        public void Deconstruct(out Triangle triangle, out int edge)
        {
            triangle = this.triangle;
            edge = this.edge;
        }
    }

    public class Mesh
    {
        public readonly static int[] NEXT = [1, 2, 0], PREV = [2, 0, 1];

        readonly List<Node> _nodes = new List<Node>();
        readonly List<Triangle> _triangles = new List<Triangle>();

        public List<Triangle> Triangles => _triangles;
        public List<Node> Nodes => _nodes;

        public Mesh AddSuperStructure(Rectangle bounds, double scale)
        {
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double size = Math.Max(scale, 2) * dmax;

            Node a = new Node(0, midx - size, midy - size, 0);
            Node b = new Node(1, midx + size, midy - size, 0);
            Node c = new Node(2, midx, midy + size, 0);

            _nodes.Add(a);
            _nodes.Add(b);
            _nodes.Add(c);

            Triangle triangle = new Triangle(0, a, b, c, Area(a, b, c), null);
            _triangles.Add(triangle);

            a.Triangle = b.Triangle = c.Triangle = triangle.index;
            return this;
        }

        static double Area(Node a, Node b, Node c)
        {
            double cross = Node.Cross(a, b, c);
            if (cross <= 0)
            {
                throw new Exception("Invalid triangle");
            }
            return cross * 0.5;
        }

        public Triangle EntranceTriangle(int start, int end)
        {
            Node nodeA = _nodes[start];
            Node nodeB = _nodes[end];

            TriangleWalker walker = new TriangleWalker(_triangles, nodeA.Triangle, nodeA.Index);
            do
            {
                Triangle current = _triangles[walker.Current];

                int count = 0;
                for (int i = 0; i < 3; i++)
                {
                    current.Edge(i, out int a, out int b);
                    if (a == start || b == start)
                    {
                        double cross = Node.Cross(_nodes[a], _nodes[b], nodeB);
                        if (cross >= 0)
                        {
                            count++;
                        }
                    }

                    if (count == 2)
                    {
                        return current;
                    }
                }
            }
            while (walker.MoveNextCW());

            throw new Exception("Could not find entrance triangle.");
        }

        public void SetConstraint(int triangle, int edge, bool value)
        {
            Triangle tri = _triangles[triangle];
            tri.constrained[edge] = value;

            int adjIndex = tri.adjacent[edge];
            if (adjIndex == -1)
            {
                return;
            }

            tri.Edge(edge, out int a, out int b);

            Triangle adj = _triangles[adjIndex];
            adj.constrained[adj.IndexOf(b, a)] = value;
        }

        public List<int> Legalize(List<int> affected, Affected[] newElements)
        {
            Stack<Affected> toLegalize = new Stack<Affected>(newElements);
            while (toLegalize.Count > 0)
            {
                var (triangle, edge) = toLegalize.Pop();
                if (!CanFlip(triangle, edge) || !ShouldFlip(triangle, edge))
                {
                    continue;
                }

                Affected[] flipped = Flip(triangle, edge);
                Add(flipped);
                affected.Add(triangle.index);

                foreach (Affected item in flipped)
                {
                    toLegalize.Push(item);
                }
            }
            return affected;
        }

        public int[] Quad(Triangle triangle, int edge)
        {
            int adj = triangle.adjacent[edge];
            if (adj == -1)
            {
                return [-1, -1, -1, -1];
            }

            Triangle acd = triangle;
            Triangle cab = _triangles[adj];

            int ac = edge;
            int cd = NEXT[ac];
            int da = PREV[ac];

            int ca = cab.IndexOf(cd, ac);
            int ab = NEXT[ca];
            int bc = PREV[ca];

            return 
            [
                acd.indices[ac],
                cab.indices[bc],
                acd.indices[cd],
                acd.indices[da]
            ];
        }

        public bool CanFlip(Triangle triangle, int edge)
        {
            if (triangle.constrained[edge])
            {
                return false;
            }

            int[] points = Quad(triangle, edge);
            Node a = _nodes[points[0]];
            Node b = _nodes[points[1]];
            Node c = _nodes[points[2]];
            Node d = _nodes[points[3]];
            if (!QuadConvex(a, b, c, d))
            {
                return false;
            }
            return true;
        }

        public bool ShouldFlip(Triangle triangle, int edge)
        {
            int adjIndex = triangle.adjacent[edge];

            triangle.Edge(edge, out int a, out int b);

            Triangle adj = _triangles[adjIndex];
            int adjEdge = adj.IndexOf(b, a);

            Node opposite = _nodes[adj.indices[PREV[adjEdge]]];
            if (triangle.circle.Contains(opposite.X, opposite.Y))
            {
                return true;
            }
            return false;
        }

        public static bool QuadConvex(Node a, Node b, Node c, Node d)
        {
            double ab_bc = Node.Cross(a, b, c);
            double bc_cd = Node.Cross(b, c, d);
            double cd_da = Node.Cross(c, d, a);
            double da_ab = Node.Cross(d, a, b);
            return ab_bc > 0 && bc_cd > 0 && cd_da > 0 && da_ab > 0;
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

                currentTriangle.Edge(bestExit, out int aStart, out int bEnd);
                skipEdge = _triangles[next].IndexOf(bEnd, aStart);
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
                int adjIndex = triangle.adjacent[edge];
                if (adjIndex != -1)
                {
                    triangle.Edge(edge, out int a, out int b);
                    Triangle adj = _triangles[adjIndex];
                    adj.adjacent[adj.IndexOf(b, a)] = index;
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

        public List<int> SplitAndAdd(Triangle triangle, Node node)
        {
            _nodes.Add(node);
            Affected[] affected = Split(triangle, node);
            Add(affected);
            return Legalize(new List<int>(), affected);
        }

        public List<int> SplitAndAdd(Triangle triangle, int edge, Node node)
        {
            Affected[] affected = Split(triangle, edge, node);
            Add(affected);
            return Legalize(new List<int>(), affected);
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

            Triangle dab = new Triangle(t0, d, a, b, Area(d, a, b), abc.parents);
            Triangle dbc = new Triangle(t1, d, b, c, Area(d, b, c), abc.parents);
            Triangle dca = new Triangle(t2, d, c, a, abc.area - dab.area - dbc.area, abc.parents);

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

            Triangle eda = new Triangle(t0, e, d, a, Area(e, d, a), acd.parents);
            Triangle ecd = new Triangle(t1, e, c, d, acd.area - eda.area, acd.parents);
            Triangle ebc = new Triangle(t2, e, b, c, Area(e, b, c), cab.parents);
            Triangle eab = new Triangle(t3, e, a, b, cab.area - ebc.area, cab.parents);

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

            Triangle dca = new Triangle(t0, d, c, a, Area(d, c, a), triangle.parents);
            Triangle cdb = new Triangle(t1, c, d, b, abc.area - dca.area, triangle.parents);

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

            List<int> parents = new List<int>(acd.parents);
            foreach (var item in cba.parents)
            {
                if (!parents.Contains(item))
                {
                    parents.Add(item);
                }
            }

            Triangle bda = new Triangle(t0, b, d, a, Area(b, d, a), parents);
            Triangle dbc = new Triangle(t1, d, b, c, acd.area + cba.area - bda.area, parents);

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

        public string ToSvg(int size = 1000, float padding = 10f, bool fill = true, bool drawConstraints = false, bool drawCircles = false)
        {
            // https://www.svgviewer.dev/

            if (Nodes.Count == 0 || Triangles.Count == 0)
                return "<svg xmlns='http://www.w3.org/2000/svg'/>";

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            foreach (var v in Nodes)
            {
                double x = v.X;
                double y = v.Y;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            double scale = (size - 2 * padding) / Math.Max(maxX - minX, maxY - minY);

            var sb = new StringBuilder();
            sb.Append("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 ");
            sb.Append(size); sb.Append(' '); sb.Append(size); sb.Append("'>");

            if (fill)
            {
                foreach (var tri in Triangles)
                {
                    var a = Nodes[tri.indices[0]];
                    var b = Nodes[tri.indices[1]];
                    var c = Nodes[tri.indices[2]];
                    var (x1, y1) = Project(a.X, a.Y);
                    var (x2, y2) = Project(b.X, b.Y);
                    var (x3, y3) = Project(c.X, c.Y);

                    string color = BlendColorsFromIds(tri.parents);
                    sb.Append($"<polygon points='{x1:F1},{y1:F1} {x2:F1},{y2:F1} {x3:F1},{y3:F1}' fill='{color}' fill-opacity='0.5' stroke='#000' stroke-width='1'/>");
                }
            }
            else
            {
                var drawn = new HashSet<(int, int)>();
                sb.Append("<path d='");
                foreach (var tri in Triangles)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int a = tri.indices[i];
                        int b = tri.indices[(i + 1) % 3];
                        int lo = Math.Min(a, b), hi = Math.Max(a, b);
                        if (!drawn.Add((lo, hi))) continue;

                        var va = Nodes[a];
                        var vb = Nodes[b];
                        var (x1, y1) = Project(va.X, va.Y);
                        var (x2, y2) = Project(vb.X, vb.Y);
                        sb.Append($"M{x1:F1},{y1:F1}L{x2:F1},{y2:F1}");
                    }
                }
                sb.Append("' fill='none' stroke='#000' stroke-width='1'/>");
            }

            if (drawConstraints)
            {
                var drawn = new HashSet<(int, int)>();
                sb.Append("<path d='");
                foreach (var tri in Triangles)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (!tri.constrained[i]) continue;

                        int a = tri.indices[i];
                        int b = tri.indices[(i + 1) % 3];
                        int lo = Math.Min(a, b), hi = Math.Max(a, b);
                        if (!drawn.Add((lo, hi))) continue;

                        var va = Nodes[a];
                        var vb = Nodes[b];
                        var (x1, y1) = Project(va.X, va.Y);
                        var (x2, y2) = Project(vb.X, vb.Y);
                        sb.Append($"M{x1:F1},{y1:F1}L{x2:F1},{y2:F1}");
                    }
                }
                sb.Append("' fill='none' stroke='red' stroke-width='2.5'/>");
            }

            if (drawCircles)
            {
                foreach (var tri in Triangles)
                {
                    var (cx, cy) = Project(tri.circle.x, tri.circle.y);
                    double r = Math.Sqrt(tri.circle.radiusSqr) * scale;
                    sb.Append($"<circle cx='{cx:F1}' cy='{cy:F1}' r='{r:F1}' fill='none' stroke='blue' stroke-opacity='0.6' stroke-width='1'/>");
                }
            }


            sb.Append("</svg>");
            return sb.ToString();

            (double x, double y) Project(double x, double y)
            {
                double sx = (x - minX) * scale + padding;
                double sy = (y - minY) * scale + padding;
                return (sx, size - sy); // Y-flip
            }
        }

        static string BlendColorsFromIds(List<int> ids)
        {
            if (ids.Count == 0)
                return "#CCCCCC"; // fallback gray

            float rSum = 0, gSum = 0, bSum = 0;
            foreach (int id in ids)
            {
                string hex = ColorFromId(id);
                int r = Convert.ToInt32(hex.Substring(1, 2), 16);
                int g = Convert.ToInt32(hex.Substring(3, 2), 16);
                int b = Convert.ToInt32(hex.Substring(5, 2), 16);

                rSum += r;
                gSum += g;
                bSum += b;
            }

            int R = (int)(rSum / ids.Count);
            int G = (int)(gSum / ids.Count);
            int B = (int)(bSum / ids.Count);

            return $"#{R:X2}{G:X2}{B:X2}";
        }

        static string ColorFromId(int id)
        {
            // Generate a visually distinct pastel color using HSV mapping
            float hue = (id * 137) % 360; // 137 = golden angle approximation
            float s = 0.5f, v = 0.95f;

            float c = v * s;
            float x = c * (1 - MathF.Abs((hue / 60f % 2) - 1));
            float m = v - c;

            float r, g, b;
            if (hue < 60) (r, g, b) = (c, x, 0);
            else if (hue < 120) (r, g, b) = (x, c, 0);
            else if (hue < 180) (r, g, b) = (0, c, x);
            else if (hue < 240) (r, g, b) = (0, x, c);
            else if (hue < 300) (r, g, b) = (x, 0, c);
            else (r, g, b) = (c, 0, x);

            int R = (int)((r + m) * 255);
            int G = (int)((g + m) * 255);
            int B = (int)((b + m) * 255);

            return $"#{R:X2}{G:X2}{B:X2}";
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
