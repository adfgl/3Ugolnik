using System.Text;

namespace CDTlib
{
    public class Mesh
    {
        readonly List<Face> _faces;
        double _eps = 1e-6;

        public Mesh(int capacity = 4)
        {
            _faces = new List<Face>(Math.Max(capacity, 4));
        }

        public List<Face> Faces => _faces;

        public double Eps
        {
            get => _eps;
            set => _eps = value;
        }

        public Edge? FindEdgeBrute(Node a, Node b)
        {
            foreach (Face face in _faces)
            {
                foreach (Edge edge in face)
                {
                    Node s = edge.Origin;
                    Node e = edge.Next.Origin;
                    if (a == s && b == e || a == e && b == s)
                    {
                        return edge;
                    }
                }
            }
            return null;
        }

        public Edge? FindEdge(Node a, Node b)
        {
            foreach (Edge edge in a.Forward())
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

        public Face? EntranceTriangle(Node a, Node b)
        {
            foreach (Edge edge in a.Forward())
            {
                var (start, end) = edge;
                if (Node.Cross(start, end, b) > 0)
                {
                    (start, end) = edge.Next;
                    if (Node.Cross(start, end, b) > 0)
                    {
                        return edge.Face;
                    }
                }
            }
            return null;
        }

        public Mesh AddSuperStructure(Rectangle bounds)
        {
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double size = 2 * dmax;

            Node a = new Node(-3, midx - size, midy - size);
            Node b = new Node(-2, midx + size, midy - size);
            Node c = new Node(-1, midx, midy + size);

            Face superFace = new Face(0, a, b, c);
            _faces.Add(superFace);

            return this;
        }

        public Mesh Add(TopologyChange source)
        {
            int n = source.NewFaces.Length;
            if (n != source.AffectedEdges.Length)
            {
                throw new Exception();
            }

            for (int i = 0; i < n; i++)
            {
                Face f = source.NewFaces[i];
                Edge e = source.AffectedEdges[i];

                Edge? twin = e.Twin;
                if (twin is not null)
                {
                    twin.Twin = e;
                    twin.Origin.Edge = twin;
                }

                e.Origin.Edge = e;

                if (f.Index < 0)
                {
                    f.Index = _faces.Count;
                    _faces.Add(f);
                }
                else
                {
                    _faces[f.Index] = f;
                }
            }

            foreach (Face f in source.OldFaces)
            {
                f.Dead = true;

            }
            return this;
        }

        public (Face? t, Edge? e, Node? n) FindContaining(double x, double y, Face? start = null)
        {
            if (_faces.Count == 0)
            {
                return (null, null, null);
            }

            int maxSteps = _faces.Count * 3;
            int trianglesChecked = 0;

            Node pt = new Node(-1, x, y);
            Face current = start is null ? _faces[^1] : start;
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


                    Node a = edge.Origin;
                    double ax = a.X, ay = a.Y;

                    Node b = edge.Next.Origin;
                    double bx = b.X, by = b.Y;

                    double adx = x - a.X;
                    double ady = y - a.Y;
                    if (adx * adx + ady * ady < _eps)
                    {
                        return (current, edge, a);
                    }

                    double bdx = x - b.X;
                    double bdy = y - b.Y;
                    if (bdx * bdx + bdy * bdy < _eps)
                    {
                        return (current, edge.Next, b);
                    }

                    double cross = Node.Cross(a, b, pt);
                    if (Math.Abs(cross) < _eps)
                    {
                        double dx = bx - ax;
                        double dy = by - ay;
                        double dot = (x - ax) * dx + (y - ay) * dy;
                        double lenSq = dx * dx + dy * dy;

                        if (dot >= -_eps && dot <= lenSq + _eps)
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
                current = bestExit.Twin.Face;
            }
        }


        public string ToSvg(float size = 1000f, float padding = 10f, string fillColor = "#ccc", string edgeColor = "#000")
        {
            // https://www.svgviewer.dev/

            var faces = Faces;
            if (faces.Count == 0)
                return "<svg xmlns='http://www.w3.org/2000/svg'/>";

            var vertices = new HashSet<Node>();
            foreach (var tri in faces)
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

            foreach (var tri in faces)
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

    }
}
