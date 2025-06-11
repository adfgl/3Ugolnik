using CDTlib.DataStructures;
using CDTlib.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace CDTlib
{
    public static class CDT
    {
        public const double EPS = 1e-6;

        public static List<Triangle> Triangulate(IList<Vec2> points)
        {
            (Rect bounds, List<Vec2> uniquePoints) = Preporcess(points);

            int count = uniquePoints.Count;
            if (count < 3)
            {
                throw new Exception($"Must have at least 3 points but got only {count} unique points.");
            }

            List<Triangle> triangles = new List<Triangle>();
            QuadTree nodes = new QuadTree(bounds);

            AddSuperStructure(triangles, nodes.Bounds);
            int superIndex = nodes.Count;

            foreach (Vec2 point in uniquePoints)
            {
                var (x, y) = point;
                Insert(triangles, nodes, x, y, out _); 
            }

            Refine(triangles, nodes, 25);
            return triangles;
        }

        public static string ToSvg(List<Triangle> triangles, float size = 1000f, float padding = 10f, string fillColor = "#ccc", string edgeColor = "#000")
        {
            // https://www.svgviewer.dev/

            if (triangles.Count == 0)
                return "<svg xmlns='http://www.w3.org/2000/svg'/>";

            var vertices = new HashSet<Node>();
            foreach (var tri in triangles)
            {
                foreach (var edge in tri)
                {
                    vertices.Add(edge.Origin);
                }
            }

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            foreach (var v in vertices)
            {
                if (v.X < minX) minX = v.X;
                if (v.X > maxX) maxX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Y > maxY) maxY = v.Y;
            }

            double scale = (size - 2 * padding) / Math.Max(maxX - minX, maxY - minY);

            var sb = new StringBuilder();
            sb.Append("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 ");
            sb.Append(size); sb.Append(' '); sb.Append(size); sb.Append("'>");

            foreach (var tri in triangles)
            {
                var a = tri.Edge.Origin;
                var b = tri.Edge.Next.Origin;
                var c = tri.Edge.Next.Next.Origin;

                var (x1, y1) = Project(a.X, a.Y);
                var (x2, y2) = Project(b.X, b.Y);
                var (x3, y3) = Project(c.X, c.Y);

                sb.Append($"<polygon points='{x1:F1},{y1:F1} {x2:F1},{y2:F1} {x3:F1},{y3:F1}' fill='{fillColor}' fill-opacity='0.5' stroke='{edgeColor}' stroke-width='1'/>");
            }

            sb.Append("</svg>");
            return sb.ToString();

            (double x, double y) Project(double x, double y)
            {
                double sx = (x - minX) * scale + padding;
                double sy = (y - minY) * scale + padding;
                return (sx, size - sy); // Y-flip for SVG coordinates
            }
        }

        public static void Refine(List<Triangle> triangles, QuadTree nodes, double maxArea)
        {
            HashSet<Segment> seen = new HashSet<Segment>();
            Queue<Triangle> triangleQueue = new Queue<Triangle>();
            Queue<Segment> segmentQueue = new Queue<Segment>();
            foreach (Triangle t in triangles)
            {
                if (IsBad(t, maxArea))
                {
                    triangleQueue.Enqueue(t);
                }

                foreach (Edge e in t)
                {
                    if (!e.Constrained) continue;

                    Segment segment = new Segment(e.Origin, e.Next.Origin);
                    if (seen.Add(segment) && Enchrouched(nodes, segment))
                    {
                        segmentQueue.Enqueue(segment);
                    }
                }
            }

            while (segmentQueue.Count > 0 || triangleQueue.Count > 0) //  
            {
                if (segmentQueue.Count > 0)
                {
                    Segment seg = segmentQueue.Dequeue();
                    Edge? existing = FindEdge(seg.a, seg.b);
                    if (existing is null)
                    {
                        throw new Exception($"Midpoint of segment ({seg.a},{seg.b}) not found on any edge.");
                    }

                    double x = seg.cx;
                    double y = seg.cy;

                    Node node = new Node(nodes.Count, x, y);
                    nodes.Add(node);

                    Segment s1 = new Segment(seg.a, node);
                    Segment s2 = new Segment(node, seg.b);
                    seen.Remove(seg);
                    seen.Add(s1);
                    seen.Add(s2);

                    if (IsVisibleFromInterior(seen, s1, x, y) && Enchrouched(nodes, s1))
                    {
                        segmentQueue.Enqueue(s1);
                    }
                    if (IsVisibleFromInterior(seen, s2, x, y) && Enchrouched(nodes, s2))
                    {
                        segmentQueue.Enqueue(s2);
                    }

                    SplitEdge(triangles.Count, existing, node, out Edge[] affected, out Triangle[] oldTris, out Triangle[] newTris);
                    AddNewTriangles(triangles, newTris, oldTris);
                    Legalize(triangles, affected);

                    if (segmentQueue.Count == 0)
                    {
                        foreach (Triangle t in triangles)
                        {
                            if (IsBad(t, maxArea))
                            {
                                triangleQueue.Enqueue(t);
                            }
                        }
                    }
                    continue;
                }

                if (triangleQueue.Count > 0)
                {
                    Triangle tri = triangleQueue.Dequeue();

                    if (!IsBad(tri, maxArea))
                    {
                        continue;
                    }

                    double x = 0;
                    double y = 0;
                    Circumcenter(tri, out x, out y);

                    bool encroaches = false;
                    foreach (Segment seg in seen)
                    {
                        double dx = seg.cx - x;
                        double dy = seg.cy - y;
                        bool insideCircle = dx * dx + dy * dy < seg.lengthSqr * 0.25;
                        if (insideCircle && IsVisibleFromInterior(seen, seg, x, y))
                        {
                            segmentQueue.Enqueue(seg);
                            encroaches = true;
                        }
                    }

                    if (encroaches)
                    {
                        continue;
                    }

                    Insert(triangles, nodes, x, y, out _);

                    foreach (Triangle t in triangles)
                    {
                        if (IsBad(t, maxArea))
                        {
                            triangleQueue.Enqueue(t);
                        }
                    }
                }
            }
        }

        public static bool IsVisibleFromInterior(IEnumerable<Segment> segments, Segment seg, double x, double y)
        {
            Vec2 pt = new Vec2(x, y);
            Vec2 mid = new Vec2(seg.cx, seg.cy);
            foreach (Segment s in segments)
            {
                if (s.Equals(seg))
                    continue;

                Node start = s.a;
                Node end = s.b;
                if (GeometryHelper.Intersect(mid, pt, new Vec2(start.X, start.Y), new Vec2(end.X, end.Y), out _))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool Enchrouched(QuadTree nodes, Segment segment)
        {
            Node a = segment.a;
            Node b = segment.b;

            double cx = segment.cx;
            double cy = segment.cy;

            double radiusSqr = segment.lengthSqr * 0.25;
            double radius = Math.Sqrt(radiusSqr);
            Rect bound = new Rect(cx - radius, cy - radius, cx + radius, cy + radius);

            List<Node> points = nodes.Query(bound);
            foreach (Node n in points)
            {
                if (n == a || n == b) continue;

                double dxn = n.X - cx;
                double dyn = n.Y - cy;
                double distSq = dxn * dxn + dyn * dyn;

                if (distSq < radiusSqr)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsBad(Triangle t, double maxAllowedArea)
        {
            if (t.Deleted)
            {
                return false;
            }

            if (t.Edge.Origin.Index < 0 || t.Edge.Next.Origin.Index < 0 || t.Edge.Prev.Origin.Index < 0)
            {
                return false;
            }

            if (t.Area > maxAllowedArea)
            {
                return true;
            }
            return false;
        }

        public static void InsertConstraint(List<Triangle> triangles, QuadTree nodes, Node a, Node b)
        {
            if (a == b)
                return;

            Vec2 start = new Vec2(a.X, a.Y);
            Vec2 end = new Vec2(b.X, b.Y);

            while (true)
            {
                Edge? existing = FindEdge(a, b);
                if (existing != null)
                {
                    SetConstraint(existing, true);
                    return;
                }

                Triangle? current = EntranceTriangle(a, b);
                if (current == null)
                    throw new Exception("Failed to locate entrance");

                bool changed = false;

                while (true)
                {
                    foreach (Edge edge in current)
                    {
                        Node n1 = edge.Origin;
                        Node n2 = edge.Next.Origin;

                        if (n1 == a || n2 == a || n1 == b || n2 == b)
                            continue;

                        Vec2 e1 = new Vec2(n1.X, n1.Y);
                        Vec2 e2 = new Vec2(n2.X, n2.Y);

                        if (!GeometryHelper.Intersect(start, end, e1, e2, out Vec2 inter))
                            continue;

                        if (edge.Constrained)
                        {
                            Insert(triangles, nodes, inter.x, inter.y, out _, current);
                        }
                        else
                        {
                            Edge flippedEdge = FlipEdge(edge, out Edge[] affected, out Triangle[] oldTris, out Triangle[] newTris);
                            SetConstraint(flippedEdge, true);
                            Legalize(triangles, affected);
                        }

                        changed = true;
                        break;
                    }

                    if (changed)
                        break;

                    Edge? nextEdge = null;
                    double mostNegativeCross = 0;
                    foreach (Edge edge in current)
                    {
                        Node a1 = edge.Origin;
                        Node a2 = edge.Next.Origin;

                        double cross = GeometryHelper.Cross(a1.X, a1.Y, a2.X, a2.Y, b.X, b.Y);
                        if (cross < mostNegativeCross || nextEdge == null)
                        {
                            mostNegativeCross = cross;
                            nextEdge = edge;
                        }
                    }

                    if (nextEdge?.Twin == null)
                    {
                        throw new Exception("Stuck during constraint insertion. Mesh may be invalid or degenerate.");
                    }
                    current = nextEdge.Twin.Triangle;
                }
            }
        }

        static Triangle? EntranceTriangle(Node a, Node b)
        {
            double x = b.X; 
            double y = b.Y;
            foreach (Edge edge in a.Around())
            {
                if (edge.Orient(x, y) == EOrientation.Left &&
                    edge.Next.Orient(x, y) == EOrientation.Left)
                {
                    return edge.Triangle;
                }
            }
            return null;
        }

        public static Node? Insert(List<Triangle> triangles, QuadTree nodes, double x, double y, out List<Triangle> affectedTris, Triangle? start = null)
        {
            Node? existing = nodes.TryGet(x, y);
            if (existing != null)
            {
                affectedTris = new List<Triangle>();
                return existing;
            }

            (Triangle? triangle, Edge? edge, Node? node) = FindContaining(triangles, x, y, EPS, start);
            if (node != null)
            {
                affectedTris = new List<Triangle>();
                return node;
            }

            if (triangle == null)
            {
                throw new Exception($"Point [{x} {y}] is outside of topology.");
            }
        
            Node newNode = new Node(nodes.Count, x, y);
            nodes.Add(newNode);

            Triangle[] newTris;
            Triangle[] oldTris;
            Edge[] affected;
            int baseIndex = triangles.Count;
            if (edge == null)
            {
                SplitTriangle(baseIndex, triangle, newNode, out affected, out oldTris, out newTris);
            }
            else
            {
                SplitEdge(baseIndex, edge, newNode, out affected, out oldTris, out newTris);
            }

            AddNewTriangles(triangles, newTris, oldTris);
            affectedTris = Legalize(triangles, affected);
            return newNode;
        }

        public static List<Triangle> Legalize(List<Triangle> triangles, Edge[] affected)
        {
            List<Triangle> affectedTris = new List<Triangle>();
            Stack<Edge> toLegalize = new Stack<Edge>(affected);
            while (toLegalize.Count > 0)
            {
                Edge edge = toLegalize.Pop();
                if (ShouldFlip(edge))
                {
                    Edge flipped = FlipEdge(edge, out Edge[] newAffected, out Triangle[] oldTris, out Triangle[] newTris);
                    AddNewTriangles(triangles, newTris, oldTris);
                    foreach (Edge e in newAffected)
                    {
                        toLegalize.Push(e);
                        affectedTris.Add(e.Triangle);
                    }
                }
            }
            return affectedTris;
        }

        public static bool ShouldFlip(Edge edge)
        {
            Edge? twin = edge.Twin;
            if (twin is null || edge.Constrained)
            {
                return false;
            }

            if (edge.Triangle.Deleted)
            {
                return false;
            }

            /*
                           v2            
                           /\             
                          /  \            
                         /    \           
                        /      \          
                       /        \         
                      /          \        
                     /            \       
                 v1 +--------------+ v3   
                     \            /       
                      \          /        
                       \        /         
                        \      /          
                         \    /           
                          \  /            
                           \/             
                           v0             
             */

            Node v0 = twin.Prev.Origin;
            Node v1 = edge.Origin;
            Node v2 = edge.Prev.Origin;
            Node v3 = twin.Origin;

            if (!GeometryHelper.ConvexQuad(v0.X, v0.Y, v1.X, v1.Y, v2.X, v2.Y, v3.X, v3.Y))
            {
                return false;
            }

            if (DelaunayCriteria.InCircle(v0.X, v0.Y, v1.X, v1.Y, v3.X, v3.Y, v2.X, v2.Y))
            {
                return true;
            }
            return false;
        }

        public static Edge FlipEdge(Edge edge, out Edge[] affected, out Triangle[] oldTris, out Triangle[] newTris)
        {
            /*
              v2 - is inserted point, we want to propagate flip away from it, otherwise we 
              are risking ending up in flipping degeneracy
                           v2                        v2
                           /\                        /|\
                          /  \                      / | \
                         /    \                    /  |  \
                        /      \                  /   |   \ 
                       /   t0   \                /    |    \
                      /          \              /     |     \ 
                     /            \            /      |      \
                 v0 +--------------+ v1    v0 +   t1  |  t0   + v1
                     \            /            \      |      /
                      \          /              \     |     /
                       \   t1   /                \    |    /
                        \      /                  \   |   / 
                         \    /                    \  |  /
                          \  /                      \ | /
                           \/                        \|/
                           v3                        v3
             */

            Edge? twin = edge.Twin;
            if (twin is null)
            {
                throw new Exception();
            }

            Node v0 = edge.Origin;
            Node v1 = edge.Next.Origin;
            Node v2 = edge.Prev.Origin;
            Node v3 = twin.Prev.Origin;

            Triangle t0 = edge.Triangle;
            Triangle t1 = twin.Triangle;

            Triangle new0 = BuildTriangle(v2, v3, v1, t0.Index);
            new0.Area = Area(new0);

            Triangle new1 = BuildTriangle(v3, v2, v0, t1.Index);
            new1.Area = t0.Area + t1.Area - new0.Area;

            SetTwins(new0.Edge, new1.Edge);

            SetTwins(new0.Edge.Next, twin.Prev.Twin);  
            SetTwins(new0.Edge.Prev, edge.Next.Twin);  

            SetTwins(new1.Edge.Next, edge.Prev.Twin);  
            SetTwins(new1.Edge.Prev, twin.Next.Twin);

            oldTris =[t0, t1];
            newTris =[new0, new1];
            affected =[new0.Edge.Next, new1.Edge.Prev];
            return new0.Edge;
        }

        public static void SplitEdge(int baseIndex, Edge edge, Node node, out Edge[] affected, out Triangle[] oldTris, out Triangle[] newTris)
        {
            /*
                        v2                          v2            
                        /\                          /|\             
                       /  \                        / | \           
                      /    \                      /  |  \          
                     /      \                    /   |   \      
                    /   t0   \                  /    |    \        
                   /          \                / t0  |  t1 \       
                  /            \              /      |      \      
              v0 +--------------+ v1      v0 +-------v4------+ v1  
                  \            /              \      |      /      
                   \          /                \ t3  |  t2 /       
                    \   t1   /                  \    |    /        
                     \      /                    \   |   /      
                      \    /                      \  |  /          
                       \  /                        \ | /           
                        \/                          \|/            
                        v3                          v3            
            */

            Edge? twin = edge.Twin;
            if (twin == null)
            {
                throw new Exception();
            }

            Triangle t0 = edge.Triangle;
            Triangle t1 = twin.Triangle;

            Triangle new0 = BuildTriangle(edge.Prev, node, t0.Index);
            Triangle new1 = BuildTriangle(edge.Next, node, baseIndex);
            Triangle new2 = BuildTriangle(twin.Prev, node, baseIndex + 1);
            Triangle new3 = BuildTriangle(twin.Next, node, t1.Index);

            new0.Area = Area(new0);
            new1.Area = t0.Area - new0.Area;
            new2.Area = Area(new0);
            new3.Area = t0.Area - new2.Area;

            bool constrained = edge.Constrained || twin.Constrained;
            new0.Edge.Next.Constrained = constrained;
            new1.Edge.Prev.Constrained = constrained;
            new2.Edge.Next.Constrained = constrained;
            new3.Edge.Prev.Constrained = constrained;

            oldTris = [t0, t1];
            newTris = [new0, new1, new2, new3];
            affected = [new0.Edge, new1.Edge, new2.Edge, new3.Edge];
            SetTwins(newTris);
        }

        public static void SplitTriangle(int baseIndex, Triangle triangle, Node node, out Edge[] affected, out Triangle[] oldTris, out Triangle[] newTris)
        {
            Triangle new0 = BuildTriangle(triangle.Edge, node, triangle.Index);
            Triangle new1 = BuildTriangle(triangle.Edge.Next, node, baseIndex);
            Triangle new2 = BuildTriangle(triangle.Edge.Prev, node, baseIndex + 1);

            new0.Area = Area(new0);
            new1.Area = Area(new1);
            new2.Area = triangle.Area - new0.Area - new1.Area;

            oldTris = [triangle];
            newTris = [new0, new1, new2];
            affected = [new0.Edge, new1.Edge, new2.Edge];
            SetTwins(newTris);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Area(Triangle t)
        {
            Node a = t.Edge.Origin;
            Node b = t.Edge.Next.Origin;
            Node c = t.Edge.Prev.Origin;
            return GeometryHelper.Area(a.X, a.Y, b.X, b.Y, c.X, c.Y);
        }

        public static void Circumcenter(Triangle t, out double x, out double y)
        {
            Node a = t.Edge.Origin;
            Node b = t.Edge.Next.Origin;
            Node c = t.Edge.Prev.Origin;
            GeometryHelper.Circumcenter(a.X, a.Y, b.X, b.Y, c.X, c.Y, out x, out y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTwins(Edge a, Edge? b)
        {
            a.Twin = b;
            if (b != null)
            {
                b.Twin = a;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetConstraint(Edge edge, bool value = true)
        {
            edge.Constrained = value;
            if (edge.Twin is not null)
            {
                edge.Twin.Constrained = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SetTwins(Triangle[] tris)
        {
            int n = tris.Length;
            for (int i = 0; i < n; i++)
            {
                Triangle curr = tris[i];
                Triangle next = tris[(i + 1) % n];
                SetTwins(curr.Edge.Next, next.Edge.Prev);
            }
        }

        static void AddNewTriangles(List<Triangle> triangles, Triangle[] newTris, Triangle[] oldTris)
        {
            for (int i = 0; i < newTris.Length; i++)
            {
                Triangle t = newTris[i];

                Edge? twin = t.Edge.Twin;
                if (twin is not null)
                {
                    twin.Twin = t.Edge;
                }

                int index = t.Index;
                if (index < triangles.Count)
                {
                    triangles[index] = t;
                }
                else
                {
                    triangles.Add(t);
                }
            }

            for (int i = 0; i < oldTris.Length; i++)
            {
                oldTris[i].Deleted = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Triangle BuildTriangle(Node a, Node b, Node c, int index)
        {
            Edge ab = new Edge(a);
            Edge bc = new Edge(b);
            Edge ca = new Edge(c);

            Triangle tri = new Triangle(index, ab);

            a.Edge = ab;
            b.Edge = bc;
            c.Edge = ca;

            ab.Next = bc;
            bc.Next = ca;
            ca.Next = ab;

            ab.Prev = ca;
            bc.Prev = ab;
            ca.Prev = bc;

            ab.Triangle = tri;
            bc.Triangle = tri;
            ca.Triangle = tri;

            return tri;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Triangle BuildTriangle(Edge edge, Node node, int index)
        {
            Triangle tri = BuildTriangle(edge.Origin, edge.Next.Origin, node, index);
            tri.Edge.Constrained = edge.Constrained;
            tri.Edge.Twin = edge.Twin;
            return tri;
        }

        public static (Triangle? t, Edge? e, Node? n) FindContaining(List<Triangle> triangles, double x, double y, double eps = 1e-6, Triangle? start = null)
        {
            int edgesChecked = 0;
            if (triangles.Count == 0)
            {
                return (null, null, null);
            }

            int maxSteps = triangles.Count * 3;
            int trianglesChecked = 0;

            Triangle current = start is null ? triangles[^1] : start;
            Edge? skipEdge = null;
            while (true)
            {
                if (trianglesChecked++ > maxSteps)
                {
                    throw new Exception("FindContaining exceeded max steps. Likely invalid topology.");
                }

                Edge? bestExit = null;
                double mostNegativeCross = 0;
                bool inside = true;
                foreach (Edge edge in current)
                {
                    if (edge == skipEdge)
                    {
                        continue;
                    }

                    edgesChecked++;

                    Node a = edge.Origin;
                    double ax = a.X, ay = a.Y;

                    Node b = edge.Next.Origin;
                    double bx = b.X, by = b.Y;

                    double adx = x - a.X;
                    double ady = y - a.Y;
                    if (adx * adx + ady * ady < eps)
                    {
                        return (current, edge, a);
                    }

                    double bdx = x - b.X;
                    double bdy = y - b.Y;
                    if (bdx * bdx + bdy * bdy < eps)
                    {
                        return (current, edge.Next, b);
                    }

                    double cross = GeometryHelper.Cross(a.X, a.Y, b.X, b.Y, x, y);
                    if (Math.Abs(cross) < eps)
                    {
                        double dx = bx - ax;
                        double dy = by - ay;
                        double dot = (x - ax) * dx + (y - ay) * dy;
                        double lenSq = dx * dx + dy * dy;

                        if (dot >= -eps && dot <= lenSq + eps)
                        {
                            return (current, edge, null);
                        }
                    }

                    if (cross < 0)
                    {
                        inside = false;
                        if (bestExit == null || cross < mostNegativeCross)
                        {
                            mostNegativeCross = cross;
                            bestExit = edge;
                        }
                    }
                }

                if (inside)
                {
                    return (current, null, null);
                }

                if (bestExit?.Twin == null)
                {
                    return (null, null, null);
                }

                skipEdge = bestExit.Twin;
                current = bestExit.Twin.Triangle;
            }
        }

        static (Rect bounds, List<Vec2> uniquePoints) Preporcess(IList<Vec2> points)
        {
            List<Vec2> uniquePoints = new List<Vec2>(points.Count);
            Rect rect = Rect.FromPoints(points);
            QuadTree qt = new QuadTree(rect);

            foreach (Vec2 point in points)
            {
                var (x, y) = point;
                if (qt.TryGet(x, y, 1e-6) == null)
                {
                    qt.Add(new Node(-1, x, y));
                    uniquePoints.Add(point);    
                }
            }
            return (rect, uniquePoints);
        }

        static void AddSuperStructure(List<Triangle> triangles, Rect bounds)
        {
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double scale = 2;

            double size = scale * dmax;

            List<Vec2> points = new List<Vec2>();
            points.Add(new Vec2(midx - size, midy - size));
            points.Add(new Vec2(midx + size, midy - size));
            points.Add(new Vec2(midx, midy + size));

            List<Node> nodes = new List<Node>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                var (x, y) = points[i];
                x = Math.Round(x, 4);
                y = Math.Round(y, 4);
                nodes.Add(new Node(i - points.Count, x, y));
            }

            Edge? prevShared = null;
            for (int i = 1; i < points.Count - 1; i++)
            {
                Node nodeA = nodes[0];
                Node nodeB = nodes[i];
                Node nodeC = nodes[i + 1];

                Edge ab = new Edge(nodeA);
                Edge bc = new Edge(nodeB);
                Edge ca = new Edge(nodeC);

                ab.Next = bc;
                bc.Next = ca;
                ca.Next = ab;

                ab.Prev = ca;
                bc.Prev = ab;
                ca.Prev = bc;

                Triangle tri = new Triangle(triangles.Count, ab);
                tri.Area = Area(tri);

                ab.Triangle = tri;
                bc.Triangle = tri;
                ca.Triangle = tri;

                nodeA.Edge = ab;
                nodeB.Edge = bc;
                nodeC.Edge = ca;

                if (prevShared != null)
                {
                    prevShared.Twin = ca;
                    ca.Twin = prevShared;
                }

                prevShared = bc;

                triangles.Add(tri);
            }
        }

        public static Edge? FindEdge(Node a, Node b)
        {
            foreach (Edge edge in a.Around())
            {
                Node s = edge.Origin;
                Node e = edge.Next.Origin;

                if (a == s && b == e || a == e && b == s)
                {
                    return edge;
                }
            }
            return null;
        }

        static IList<Triangle> RemoveAndBuild(List<Triangle> triangles, Triangle triangle, Node node)
        {
            HashSet<Triangle> removed = new HashSet<Triangle>();
            Stack<Triangle> stack = new Stack<Triangle>();

            stack.Push(triangle);
            double x = node.X;
            double y = node.Y;

            while (stack.Count > 0)
            {
                Triangle current = stack.Pop();
                if (!removed.Add(current))
                    continue;

                foreach (Edge edge in current)
                {
                    Node a = edge.Origin;
                    Node b = edge.Next.Origin;
                    Node c = edge.Prev.Origin;

                    if (DelaunayCriteria.InCircle(x, y, a.X, a.Y, b.X, b.Y, c.X, c.Y))
                    {
                        Edge? twin = edge.Twin;
                        if (twin != null)
                        {
                            Triangle neighbor = twin.Triangle;
                            if (!removed.Contains(neighbor))
                            {
                                stack.Push(neighbor);
                            }
                        }
                    }
                }
            }

            List<Edge> border = new List<Edge>(removed.Count * 2);
            foreach (Triangle tri in removed)
            {
                foreach (Edge edge in tri)
                {
                    if (edge.Twin is not null && removed.Contains(edge.Twin.Triangle))
                    {
                        border.Add(edge);
                    }
                }
            }



            List<Triangle> newTriangles = new List<Triangle>();
            return newTriangles;
        }
    }
}
