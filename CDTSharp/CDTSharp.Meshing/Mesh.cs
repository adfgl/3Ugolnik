﻿using CDTSharp.Geometry;
using System.Diagnostics;
using System.Security.Cryptography;

namespace CDTSharp.Meshing
{
    public class Mesh
    {
        readonly List<Triangle> _triangles;
        readonly QuadTree _qt;
        readonly Rectangle _bounds;

        public List<Triangle> Triangles => _triangles;
        public List<Node> Nodes => _qt.Items;
        public Rectangle Bounds => _bounds;

        public Mesh(List<Triangle> tris, List<Node> nodes)
        {
            _bounds = Rectangle.FromPoints(nodes, o => o.X, o => o.Y);
            _triangles = tris;
            _qt = new QuadTree(_bounds);
            foreach (Node tri in nodes)
            {
                _qt.Add(tri);
            }
        }

        public Mesh(ClosedPolygon? polygon, List<(Node a, Node b)>? constraintEdges = null, List<Node>? costraintPoints = null)
        {
            List<EdgeConstraint> conEdges = new List<EdgeConstraint>();
            List<Node> conPoints = new List<Node>();

            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;

            void process(Node node)
            {
                double x = node.X;
                double y = node.Y;
                if (minX > x) minX = x;
                if (minY > y) minY = y;
                if (maxX < x) maxX = x;
                if (maxY < y) maxY = y;
            }

            void processPoly(ClosedPolygon polygon, EConstraint type)
            {
                for (int i = 0; i < polygon.Points.Count - 1; i++)
                {
                    Node a = polygon.Points[i];
                    Node b = polygon.Points[i + 1];

                    process(a);
                    conEdges.Add(new EdgeConstraint(a, b, type));
                }
            }

            if (polygon is not null)
            {
                processPoly(polygon, EConstraint.Contour);
                foreach (ClosedPolygon hole in polygon.Holes)
                {
                    processPoly(hole, EConstraint.Hole);
                }
            }

            if (constraintEdges is not null)
            {
                foreach ((Node a, Node b) in constraintEdges)
                {
                    process(a);
                    process(b);
                    conEdges.Add(new EdgeConstraint(a, b, EConstraint.User));
                }
            }

            if (costraintPoints is not null)
            {
                foreach (Node node in costraintPoints)
                {
                    process(node);
                    conPoints.Add(node);
                }
            }

            _bounds = new Rectangle(minX, minY, maxX, maxY);
            _qt = new QuadTree(_bounds);
            _triangles = new List<Triangle>();

            AddSuperStructure(_bounds, 5);


            try
            {
                foreach (EdgeConstraint edge in conEdges)
                {
                    Node a = edge.a;
                    Node b = edge.b;
                    List<Edge> added = Add(a.X, a.Y, b.X, b.Y, edge.type);
                }
            }
            catch (Exception)
            {
                Panic();
                throw;
            }

            foreach (Node node in conPoints)
            {
                Node added = Add(node.X, node.Y, out _);
            }

            if (polygon is not null)
            {
                foreach (Triangle item in _triangles)
                {
                    item.Center(out double x, out double y);
                    item.Hole = !polygon.Contains(x, y);
                }
            }
        }

        Mesh AddSuperStructure(Rectangle bounds, double scale)
        {
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double size = Math.Max(scale, 2) * dmax;

            Node a = new Node(-3, midx - size, midy - size);
            Node b = new Node(-2, midx + size, midy - size);
            Node c = new Node(-1, midx, midy + size);

            Triangle triangle = new Triangle(0, a, b, c);
            _triangles.Add(triangle);

            return this;
        }

        public Mesh RemoveSuperStructure()
        {
            foreach (Triangle tri in _triangles)
            {
                bool discard = tri.Hole || tri.ContainsSuper();
                if (!discard) continue;

                tri.Removed = true;
                foreach (Edge edge in tri.Forward())
                {
                    Node origin = edge.Origin;
                    if (origin.Edge != edge)
                    {
                        continue;
                    }

                    foreach (Edge item in origin.Around())
                    {
                        if (item.Triangle.Removed) continue;

                        origin.Edge = item;
                        break;
                    }
                }
            }

            int write = 0;
            int count = 0;
            for (int read = 0; read < _triangles.Count; read++)
            {
                Triangle tri = _triangles[read];

                if (tri.Removed)
                {
                    foreach (Edge edge in tri.Forward())
                    {
                        if (edge.Twin is not null)
                        {
                            edge.Twin.Twin = null;
                            edge.Twin = null;
                        }
                        edge.Triangle = null!;
                    }
                }
                else
                {
                    tri.Index = count++;
                    _triangles[write++] = tri;
                }
            }

            if (write < _triangles.Count)
            {
                _triangles.RemoveRange(write, _triangles.Count - write);
            }

            return this;
        }

        public Mesh Refine(Quality quality)
        {
            HashSet<EdgeConstraint> seen = new HashSet<EdgeConstraint>();

            Queue<Triangle> triangleQueue = new Queue<Triangle>();
            Queue<EdgeConstraint> segmentQueue = new Queue<EdgeConstraint>();
            foreach (Triangle t in _triangles)
            {
                if (Bad(t, quality))
                {
                    triangleQueue.Enqueue(t);
                }

                foreach (Edge e in t.Forward())
                {
                    if (e.Constrained == EConstraint.None)
                    {
                        continue;
                    }

                    EdgeConstraint segment = new EdgeConstraint(e.Origin, e.Next.Origin, e.Constrained);
                    if (seen.Add(segment) && segment.Enchrouched(_qt))
                    {
                        segmentQueue.Enqueue(segment);
                    }
                }
            }

            while (segmentQueue.Count > 0 || triangleQueue.Count > 0)
            {
                if (segmentQueue.Count > 0)
                {
                    EdgeConstraint seg = segmentQueue.Dequeue();
                    Edge? edge = FindEdge(seg.a, seg.b, true);
                    if (edge is null)
                    {
                        continue;
                    }

                    Node node = new Node(_qt.Count, seg.circle.x, seg.circle.y);
                    Triangle[] tris = Split(edge, node);
                    List<Triangle> affected = Add(node, tris);
                    foreach (Triangle t in affected)
                    {
                        triangleQueue.Enqueue(t);
                    }

                    seen.Remove(seg);
                    foreach (EdgeConstraint e in seg.Split(node))
                    {
                        seen.Add(e);
                        if (e.Enchrouched(_qt) && e.VisibleFromInterior(seen, node.X, node.Y))
                        {
                            segmentQueue.Enqueue(e);
                        }
                    }
                }

                if (triangleQueue.Count > 0)
                {
                    Triangle t = triangleQueue.Dequeue();
                    if (!Bad(t, quality))
                    {
                        continue;
                    }

                    double x = t.Circle.x;
                    double y = t.Circle.y;
                    if (!_bounds.Contains(x, y))
                    {
                        continue;
                    }

                    bool encroaches = false;
                    foreach (EdgeConstraint seg in seen)
                    {
                        if (seg.circle.Contains(x, y) && seg.VisibleFromInterior(seen, x, y))
                        {
                            segmentQueue.Enqueue(seg);
                            encroaches = true;
                        }
                    }

                    if (!encroaches)
                    {
                        Node node = Add(x, y, out List<Triangle> affected);
                        foreach (Triangle item in affected)
                        {
                            triangleQueue.Enqueue(item);
                        }
                    }
                }
            }
            return this;
        }

        public bool Bad(Triangle t, Quality q)
        {
            if (t.Removed)
            {
                return false;
            }

            double edgeLenLimSqr = q.MaxEdgeLength;
            edgeLenLimSqr *= edgeLenLimSqr;

            double minEdgeSqr = double.MaxValue;
            foreach (Edge e in t.Forward())
            {
                if (e.Origin.Index < 0)
                {
                    return false;
                }

                double lenSqr = e.SquareLength();
                if (lenSqr > edgeLenLimSqr)
                {
                    return true;
                }

                if (minEdgeSqr > lenSqr)
                {
                    minEdgeSqr = lenSqr;
                }

                double rad = GeometryHelper.Angle(e.Prev.Origin, e.Origin, e.Next.Origin);
                if (rad > q.MinAngle)
                {
                    return true;
                }
            }

            if (t.Area() > q.MaxArea)
            {
                return true;
            }
            return t.Circle.radiusSqr / minEdgeSqr > 2;
        }

        public Node Add(double x, double y, out List<Triangle> affected)
        {
            object? element = FindContaining(x, y);
            if (element == null)
            {
                throw new Exception($"Point [{x} {y}] is outside of domain.");
            }

            if (element is Node)
            {
                affected = new List<Triangle>();
                return (Node)element;
            }

            Node node = new Node(_qt.Count, x, y);

            Triangle[] newTriangles = element switch
            {
                Edge edge => Split(edge, node),
                Triangle tri => Split(tri, node),
                _ => throw new Exception("Invalid mesh element returned.")
            };

            // do something before adding?

            affected = Add(node, newTriangles);
            return node;
        }

        void Panic()
        {
            Console.WriteLine();
            Console.WriteLine(this.ToSvg());
        }

        public List<Edge> Add(double x0, double y0, double x1, double y1, EConstraint type)
        {
            Queue<(Node, Node)> toInsert = new Queue<(Node, Node)>();
            Node added0 = Add(x0, y0, out _);
            Node added1 = Add(x1, y1, out _);

            //toInsert.Enqueue((added0, added1));

            List<Edge> segments = new List<Edge>();
            while (toInsert.Count > 0)
            {
                var (start, end) = toInsert.Dequeue();
                if (start == end)
                {
                    continue;
                }
         
                while (true)
                {
                    Edge? existing = FindEdge(start, end, true);
                    if (existing is not null)
                    {
                        existing.SetConstraint(type);
                        segments.Add(existing);
                        break;
                    }

                    Triangle entrance = Entrance(start, end);
                    if (WalkAndInsert(entrance, start, end, toInsert))
                    {
                        break;
                    }
                }
            }
            return segments;
        }

        bool WalkAndInsert(Triangle triangle, Node start, Node end, Queue<(Node, Node)> toInsert)
        {
            Triangle current = triangle;
            while (true)
            {
                foreach (Edge edge in current.Forward())
                {
                    if (TrySplitOrFlip(edge, start, end, toInsert))
                    {
                        return true;
                    }
                }

                Edge exit = FindExitEdge(current, end);
                if (exit.Twin is null)
                {
                    throw new Exception("Constraint insertion failed: no adjacent triangle during walk.");
                }
                current = exit.Twin.Triangle;
            }
        }

        Edge FindExitEdge(Triangle triangle, Node target)
        {
            double x = target.X;
            double y = target.Y;

            Edge exit = null!;
            double bestCross = 0;
            foreach (Edge edge in triangle.Forward())
            {
                double cross = edge.Orientation(x, y);
                if (exit is null || cross < bestCross)
                {
                    bestCross = cross;
                    exit = edge;
                }
            }

            if (exit == null)
            {
                throw new Exception("Failed to find exit edge.");
            }

            return exit;
        }

        bool TrySplitOrFlip(Edge edge, Node start, Node end, Queue<(Node, Node)> toInsert)
        {
            if (edge.Contains(start) || edge.Contains(end))
            {
                return false;
            }

            var (a, b) = edge;
            if (!GeometryHelper.Intersect(
                a.X, a.Y, b.X, b.Y, 
                start.X, start.Y, end.X, end.Y, 
                out double x, out double y))
            {
                return false;
            }

            if (CanFlip(edge))
            {
                Triangle[] tris = Flip(edge);
                Add(tris);
            }
            else
            {
                Node node = new Node(_qt.Count, x, y);
                Triangle[] tris = Split(edge, node);

                toInsert.Enqueue((start, node));
                toInsert.Enqueue((node, end));
                Add(node, tris);
            }
            return true;
        }

        public Triangle Entrance(Node start, Node end)
        {
            foreach (Edge e in start.Around())
            {
                Triangle tri = e.Triangle;
                int count = 0;
                foreach (Edge edge in tri.Forward())
                {
                    var (a, b) = edge;
                    if (a == start || b == start)
                    {
                        double cross = edge.Orientation(end.X, end.Y);
                        if (cross >= 0)
                        {
                            count++;
                        }
                    }

                    if (count == 2)
                    {
                        return e.Triangle;
                    }
                }
            }

            throw new Exception("Could not find entrance triangle.");
        }

        public List<Triangle> Add(Node node, Triangle[] triangles)
        {
            _qt.Add(node);
            return Add(triangles);
        }

        public List<Triangle> Legalize(Triangle[] triangles)
        {
            List<Triangle> affected = new List<Triangle>();

            Stack<Triangle> toLegalize = new Stack<Triangle>(triangles);
            while (toLegalize.Count > 0)
            {
                Triangle triangle = toLegalize.Pop();
                if (triangle.Removed) continue;

                Edge edge = triangle.Edge;

                affected.Add(triangle);

                if (!CanFlip(edge) || !CanFlip(edge))
                {
                    continue;
                }

                Triangle[] flipped = Flip(edge);
                Add(flipped);

                foreach (Triangle t in flipped)
                {
                    toLegalize.Push(t);
                }
            }
            return affected;
        }

        public object? FindContaining(double x, double y, double eps = 1e-6, int searchStart = -1)
        {
            if (_triangles.Count == 0) return null;

            int maxSteps = _triangles.Count * 3;
            int trianglesChecked = 0;

            Triangle current = _triangles.Last();
            if (searchStart >= 0 && searchStart < _triangles.Count)
            {
                current = _triangles[searchStart];
            }

            Edge? skipEdge = null;
            while (true)
            {
                if (trianglesChecked++ > maxSteps)
                {
                    throw new Exception("FindContaining exceeded max steps. Likely invalid topology.");
                }

                Edge? bestExit = null;
                double worstCross = 0;
                bool inside = true;
                foreach (Edge edge in current.Forward())
                {
                    if (edge == skipEdge)
                    {
                        continue;
                    }

                    var (a, b) = edge;
                    if (a.Close(x, y, eps))
                    {
                        return a;
                    }

                    if (b.Close(x, y, eps))
                    {
                        return b;
                    }

                    double cross = edge.Orientation(x, y);
                    if (Math.Abs(cross) <= eps)
                    {
                        double dx = b.X - a.X;
                        double dy = b.Y - a.Y;
                        double dot = (x - a.X) * dx + (y - a.Y) * dy;
                        double lenSq = dx * dx + dy * dy;

                        if (dot >= -eps && dot <= lenSq + eps)
                        {
                            return edge;
                        }
                    }

                    if (cross < 0)
                    {
                        inside = false;
                        if (bestExit is null || cross < worstCross)
                        {
                            worstCross = cross;
                            bestExit = edge;
                        }
                    }
                }

                if (inside)
                {
                    return current;
                }

                if (bestExit is null || bestExit.Twin is null)
                {
                    return null;
                }

                skipEdge = bestExit.Twin;
                current = skipEdge.Triangle;
            }
        }

        public Edge? FindEdge(Node a, Node b, bool invariant)
        {
            foreach (Edge e in a.Around())
            {
                var (start, end) = e;
                if (start == a && end == b)
                {
                    return e;
                }

                if (invariant && start == b && end == a)
                {
                    return e.Twin;
                }
            }
            return null;
        }

        public Edge? FindEdgeBrute(Node a, Node b)
        {
            foreach (Triangle t in _triangles)
            {
                foreach (Edge e in t.Forward())
                {
                    var (start, end) = e;
                    if (start == a && end == b)
                    {
                        return e;
                    }
                }
            }
            return null;
        }

        static void Dispose(Edge edge)
        {
            edge.Triangle = null!;
            edge.Next = edge.Prev = null!;

            if (edge.Twin is not null)
            {
                edge.Twin.Twin = null;
                edge.Twin = null;
            }

            Node origin = edge.Origin;
            if (origin.Edge == edge)
            {
                origin.Edge = null!;
            }
        }

        static void Link(Edge edge)
        {
            if (edge.Twin is not null && edge.Twin.Twin is null)
            {
                edge.Twin.Twin = edge;
            }

            Node origin = edge.Origin;
            if (origin.Edge is null)
            {
                origin.Edge = edge;
            }
        }

        public List<Triangle> Add(Triangle[] triangles)
        {
            foreach (Triangle t in triangles)
            {
                int index = t.Index;
                if (index < _triangles.Count)
                {
                    Triangle old = _triangles[index];
                    old.Edges(out Edge ab, out Edge bc, out Edge ca);
                    Dispose(ab);
                    Dispose(bc);
                    Dispose(ca);

                    old.Removed = true;
                    old.Edge = null!;

                    _triangles[index] = t;
                }
                else
                {
                    _triangles.Add(t);
                }
            }

            foreach (Triangle t in triangles)
            {
                t.Edges(out Edge ab, out Edge bc, out Edge ca);
                Debug.Assert(ab.Twin?.Twin is null);

                Link(ab);
                Link(bc);
                Link(ca);
            }
            return Legalize(triangles);
        }

        public bool CanFlip(Edge edge)
        {
            if (edge.Constrained != EConstraint.None || edge.Twin is null)
            {
                return false;
            }

            /*
                           d           
                           /\          
                          /  \         
                         /    \        
                        /      \       
                       /   t0   \      
                      /          \     
                     /            \    
                  a +--------------+ c 
                     \            /    
                      \          /     
                       \   t1   /      
                        \      /       
                         \    /        
                          \  /         
                           \/          
                            b          
             */

            var (a, c) = edge;
            Node d = edge.Prev.Origin;
            Node b = edge.Twin.Prev.Origin;

            return GeometryHelper.QuadConvex(a, b, c, d);
        }

        public bool ShouldFlip(Edge edge)
        {
            Node opposite = edge.Twin!.Prev.Origin;
            return edge.Triangle.Circle.Contains(opposite.X, opposite.Y);
        }

        public Triangle[] Flip(Edge edge)
        {
            Edge? twin = edge.Twin;
            if (twin is null || edge.Constrained != EConstraint.None)
            {
                return [];
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

            Triangle old0 = edge.Triangle;
            Triangle old1 = twin.Triangle;

            Edge cd = edge.Next;
            Edge da = cd.Next;
            Edge ab = twin.Next;
            Edge bc = ab.Next;

            Node a = ab.Origin;
            Node b = bc.Origin;
            Node c = cd.Origin;
            Node d = da.Origin;

            Triangle new0 = new Triangle(old0.Index, a, b, d);
            Triangle new1 = new Triangle(old1.Index, b, c, d);

            new0.Edge.Twin = ab.Twin;
            new0.Edge.Next.Twin = new1.Edge.Prev;
            new0.Edge.Prev.Twin = da.Twin;

            new1.Edge.Twin = bc.Twin;
            new1.Edge.Next.Twin = cd.Twin;
            new1.Edge.Prev.Twin = new0.Edge.Next;

            new0.Edge.Constrained = ab.Constrained;
            new0.Edge.Prev.Constrained = da.Constrained;
            new1.Edge.Constrained = bc.Constrained;
            new1.Edge.Next.Constrained = cd.Constrained;

            return [new0, new1];
        }

        public Triangle[] Split(Edge edge, Node node)
        {
            Edge? twin = edge.Twin;
            if (twin is null)
            {
                return SplitNoTwin(edge, node);
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

            Triangle old0 = edge.Triangle;
            Triangle old1 = twin.Triangle;

            Edge cd = edge.Next;
            Edge da = cd.Next;
            Edge ab = twin.Next;
            Edge bc = ab.Next;

            var (a, c) = edge;
            Node b = bc.Origin;
            Node d = da.Origin;
            Node e = node;

            int baseIndex = _triangles.Count;
            Triangle new0 = new Triangle(old0.Index, d, a, e);
            Triangle new1 = new Triangle(old1.Index, c, d, e);
            Triangle new2 = new Triangle(baseIndex, b, c, e);
            Triangle new3 = new Triangle(baseIndex + 1, a, b, e);

            // twins
            new0.Edge.Twin = da.Twin;
            new0.Edge.Next.Twin = new3.Edge.Prev;
            new0.Edge.Prev.Twin = new1.Edge.Next;

            new1.Edge.Twin = cd.Twin;
            new1.Edge.Next.Twin = new0.Edge.Prev;
            new1.Edge.Prev.Twin = new2.Edge.Next;

            new2.Edge.Twin = bc.Twin;
            new2.Edge.Next.Twin = new1.Edge.Prev;
            new2.Edge.Prev.Twin = new3.Edge.Next;

            new3.Edge.Twin = ab.Twin;
            new3.Edge.Next.Twin = new2.Edge.Prev;
            new3.Edge.Prev.Twin = new0.Edge.Next;

            // constraints
            new0.Edge.Constrained = da.Constrained;
            new1.Edge.Constrained = cd.Constrained;
            new2.Edge.Constrained = bc.Constrained;
            new3.Edge.Constrained = ab.Constrained;

            EConstraint constrained = edge.Constrained;
            if (constrained != EConstraint.None)
            {
                new0.Edge.Next.Constrained = new3.Edge.Prev.Constrained = constrained;
                new1.Edge.Prev.Constrained = new2.Edge.Next.Constrained = constrained;
            }
            return [new0, new1, new2, new3];
        }

        public Triangle[] SplitNoTwin(Edge edge, Node node)
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

            Triangle old0 = edge.Triangle;

            old0.Edges(out Edge ab, out Edge bc, out Edge ca);
            Node a = ab.Origin;
            Node b = bc.Origin;
            Node c = ca.Origin;
            Node d = node;

            Triangle new0 = new Triangle(old0.Index, c, a, d);
            Triangle new1 = new Triangle(_triangles.Count, b, c, d);

            // twins
            new0.Edge.Twin = ca.Twin;
            new0.Edge.Next.Twin = null;
            new0.Edge.Prev.Twin = new1.Edge.Next;

            new1.Edge.Twin = bc.Twin;
            new1.Edge.Next.Twin = new0.Edge.Prev;
            new1.Edge.Prev.Twin = null;

            // constraints
            new0.Edge.Constrained = ca.Constrained;
            new1.Edge.Constrained = bc.Constrained;
            new0.Edge.Next.Constrained = new1.Edge.Prev.Constrained = edge.Constrained;

            return [new0, new1];
        }

        public Triangle[] Split(Triangle triangle, Node node)
        {
            /*
                        *C



                   /           ^
                  ^      *D     \



              A*        ->         *B

            */

            triangle.Edges(out Edge ab, out Edge bc, out Edge ca);
            Node a = ab.Origin;
            Node b = bc.Origin;
            Node c = ca.Origin;
            Node d = node;

            Triangle new0 = new Triangle(triangle.Index, a, b, d);
            Triangle new1 = new Triangle(_triangles.Count, b, c, d);
            Triangle new2 = new Triangle(_triangles.Count + 1, c, a, d);

            // twins
            new0.Edge.Twin = ab.Twin;
            new0.Edge.Next.Twin = new1.Edge.Prev;
            new0.Edge.Prev.Twin = new2.Edge.Next;

            new1.Edge.Twin = bc.Twin;
            new1.Edge.Next.Twin = new2.Edge.Prev;
            new1.Edge.Prev.Twin = new0.Edge.Next;

            new2.Edge.Twin = ca.Twin;
            new2.Edge.Next.Twin = new0.Edge.Prev;
            new2.Edge.Prev.Twin = new1.Edge.Next;

            // constraints
            new0.Edge.Constrained = ab.Constrained;
            new1.Edge.Constrained = bc.Constrained;
            new2.Edge.Constrained = ca.Constrained;

            return [new0, new1, new2];
        }
    }
}
