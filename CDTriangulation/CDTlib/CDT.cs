using System.Collections;
using System.Collections.Generic;
using System.Drawing;

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
    }
}
