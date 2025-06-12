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

        public IReadOnlyList<Face> Faces => _faces;

        public double Eps
        {
            get => _eps;
            set => _eps = value;
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

            Face superFace = new Face(0, a, b, c).ComputeArea();
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
                //foreach (Edge e in f)
                //{
                //    e.Face = null!;
                //    if (e.Twin?.Face == f)
                //    {
                //        e.Twin.Twin = null;
                //    }
                //    if (e.Origin.Edge == e)
                //    {
                //        e.Origin.Edge = null!;
                //    }

                //    e.Next = null!;
                //    e.Prev = null!;
                //    e.Twin = null!;
                //}
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

    }
}
