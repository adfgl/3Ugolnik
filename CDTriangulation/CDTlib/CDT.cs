using System.Xml.Linq;

namespace CDTlib
{
    public static class CDT
    {
        public static List<Face> Triangulate<T>(IEnumerable<T> points, Func<T, double> getX, Func<T, double> getY)
        {
            Rectangle rectangle = Rectangle.FromPoints(points, getX, getY);

            Mesh mesh = new Mesh();
            QuadTree nodes = new QuadTree(rectangle);
            foreach (T pt in points)
            {
                double x = getX(pt);
                double y = getY(pt);
                Node node = Insert(mesh, nodes, x, y, out _);
            }


            List<Face> faces = new List<Face>();
            return faces;
        }

        public static void Refine(Mesh mesh, QuadTree nodes, double maxArea)
        {
            HashSet<Segment> seen = new HashSet<Segment>();
            Queue<Face> triangleQueue = new Queue<Face>();
            Queue<Segment> segmentQueue = new Queue<Segment>();

            foreach (Face face in mesh.Faces)
            {
                if (IsBad(face, maxArea))
                {
                    triangleQueue.Enqueue(face);
                }

                foreach (Edge edge in face)
                {
                    if (!edge.Constrained) continue;

                    Segment segment = new Segment(edge);
                    if (seen.Add(segment) && Enchrouched(nodes, segment))
                    {
                        segmentQueue.Enqueue(segment);
                    }
                }
            }

            while (segmentQueue.Count > 0 || triangleQueue.Count > 0)
            {
                if (segmentQueue.Count > 0)
                {
                    Segment seg = segmentQueue.Dequeue();
                    Edge? existing = mesh.FindEdge(seg.a, seg.b);
                    if (existing is null)
                    {
                        throw new Exception($"Midpoint of segment ({seg.a},{seg.b}) not found on any edge.");
                    }

                    double x = seg.circle.x;
                    double y = seg.circle.y;

                    Node inserted = Insert(mesh, nodes, x, y, out List<Face> affected, existing.Face);

                    Segment s1 = new Segment(seg.a, inserted);
                    Segment s2 = new Segment(inserted, seg.b);
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

                    foreach (Face f in affected)
                    {
                        if (IsBad(f, maxArea))
                        {
                            triangleQueue.Enqueue(f);
                        }
                    }
                }

                if (triangleQueue.Count > 0)
                {
                    Face tri = triangleQueue.Dequeue();
                    if (!IsBad(tri, maxArea))
                    {
                        continue;
                    }

                    double x = tri.Circle.x;
                    double y = tri.Circle.y;

                    bool encroaches = false;
                    foreach (Segment seg in seen)
                    {
                        if (seg.circle.Contains(x, y) && IsVisibleFromInterior(seen, seg, x, y))
                        {
                            segmentQueue.Enqueue(seg);
                            encroaches = true;
                        }
                    }

                    if (encroaches)
                    {
                        continue;
                    }

                    Node inserted = Insert(mesh, nodes, x, y, out List<Face> affected);
                    foreach (Face f in affected)
                    {
                        if (IsBad(f, maxArea))
                        {
                            triangleQueue.Enqueue(f);
                        }
                    }
                }
            }
        }


        public static void InsertConstraint(Mesh mesh, QuadTree nodes, Node a, Node b)
        {
            if (a == b)
                return;

            while (true)
            {
                Edge? existing = mesh.FindEdge(a, b);
                if (existing != null)
                {
                    existing.SetConstraint(true);
                    return;
                }

                Face? current = mesh.EntranceTriangle(a, b);
                if (current == null)
                {
                    throw new Exception("Failed to locate entrance");
                }

                bool changed = false;
                while (true)
                {
                    foreach (Edge edge in current)
                    {
                        var (n1, n2) = edge;
                        if (n1 == a || n2 == a || n1 == b || n2 == b)
                        {
                            continue;
                        }

                        Node? inter = Node.Intersect(a, b, n1, n2);
                        if (inter is null)
                        {
                            continue;
                        }

                        if (edge.Constrained)
                        {
                            Insert(mesh, nodes, inter.X, inter.Y, out _, current);
                        }
                        else
                        {
                            TopologyChange flipped = edge.Flip();
                            mesh.Add(flipped);
                            Legalize(mesh, flipped);
                        }

                        changed = true;
                        break;
                    }

                    if (changed)
                    {
                        break;
                    }


                    Edge? nextEdge = null;
                    double mostNegativeCross = 0;
                    foreach (Edge edge in current)
                    {
                        var (n1, n2) = edge;
                        double cross = Node.Cross(n1, n2, b);
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
                    current = nextEdge.Twin.Face;
                }
            }
        }

        public static Node Insert(Mesh mesh, QuadTree nodes, double x, double y, out List<Face> affected, Face? start = null)
        {
            Node? newNode = nodes.TryGet(x, y);
            if (newNode != null)
            {
                affected = new List<Face>();
                return newNode;
            }

            (Face? face, Edge? edge, Node? node) = mesh.FindContaining(x, y, start);
            if (node != null)
            {
                throw new Exception("Logic error. Should not have duplicate points at this point.");
            }

            if (face == null)
            {
                throw new Exception($"Point [{x} {y}] is outside of topology.");
            }

            newNode = new Node(nodes.Count, x, y);
            nodes.Add(newNode);

            TopologyChange topologyChange;
            if (edge == null)
            {
                topologyChange = face.Split(newNode);
            }
            else
            {
                topologyChange = edge.Split(newNode);
            }

            // do something before adding?

            mesh.Add(topologyChange);
            affected = Legalize(mesh, topologyChange);
            return newNode;
        }

        public static List<Face> Legalize(Mesh mesh, TopologyChange topologyChange)
        {
            List<Face> affected = new List<Face>();
            Stack<Edge> toLegalize = new Stack<Edge>(topologyChange.AffectedEdges);
            while (toLegalize.Count > 0)
            {
                Edge edge = toLegalize.Pop();
                if (!edge.ShouldFlip())
                {
                    continue;
                }

                TopologyChange flipped = edge.Flip();
                mesh.Add(flipped);

                foreach (Edge item in flipped.AffectedEdges)
                {
                    toLegalize.Push(item);
                }
            }
            return affected;
        }


        public static bool Enchrouched(QuadTree nodes, Segment segment)
        {
            Node a = segment.a;
            Node b = segment.b;

            Rectangle bound = new Rectangle(segment.circle);
            List<Node> points = nodes.Query(bound);
            foreach (Node n in points)
            {
                if (n == a || n == b) continue;
                if (segment.circle.Contains(n.X, n.Y))
                {
                    return true;
                }
            }
            return false;
        }


        public static bool IsVisibleFromInterior(IEnumerable<Segment> segments, Segment segment, double x, double y)
        {
            Node node = new Node(-1, x, y);
            Node mid = new Node(-1, segment.circle.x, segment.circle.y);
            foreach (Segment seg in segments)
            {
                if (seg.Equals(segment))
                    continue;

                if (Node.Intersect(mid, node, seg.a, seg.b) is not null)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsBad(Face t, double maxAllowedArea)
        {
            if (t.Dead)
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
    }
}
