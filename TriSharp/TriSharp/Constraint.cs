namespace TriSharp
{
    public readonly struct Constraint : IEquatable<Constraint>
    {
        public readonly Vertex start, center, end;
        public readonly int type;
        public readonly Circle circle;
        public readonly Rectangle rectangle;

        public Constraint(Vertex a, Vertex b, int type)
        {
            this.type = type;
            this.circle = new Circle(a.X, a.Y, b.X, b.Y);
            this.rectangle = new Rectangle(a, b);
            this.center = new Vertex() 
            {
                X = circle.x, 
                Y = circle.y, 
                Z = (a.Z + b.Z) * 0.5 
            };

            if (a.Index < b.Index)
            {
                this.start = a;
                this.end = b;
            }
            else
            {
                this.start = b;
                this.end = a;
            }

        }

        public bool Degenerate(double eps) => Vertex.CloseOrEqual(start, end, eps);

        public bool VisibleFromInterior(IEnumerable<Constraint> segments, Vertex pt)
        {
            Rectangle rect = new Rectangle(pt, center);
            foreach (Constraint s in segments)
            {
                if (this.Equals(s) || !s.rectangle.Intersects(rect))
                    continue;

                if (Vertex.Intersect(center, pt, s.start, s.end) is not null)
                {
                    return false;
                }
            }
            return true;
        }

        public bool Contains(Vertex node, double eps)
        {
            return Vertex.CloseOrEqual(start, node, eps) || Vertex.CloseOrEqual(end, node, eps);
        }

        public bool Enchrouched(List<Vertex> nodes, double eps)
        {
            foreach (Vertex item in nodes)
            {
                if (circle.Contains(item.X, item.Y) && !Contains(item, eps))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Enchrouched(QuadTree qt, double eps)
        {
            Rectangle bound = new Rectangle(circle);
            List<Vertex> points = qt.Query(bound);
            return Enchrouched(points, eps);
        }

        public List<Constraint> Split(Vertex node, double eps)
        {
            if (Contains(node, eps))
            {
                return [this];
            }
            return [new Constraint(this.start, node, type), new Constraint(node, this.end, type)];
        }

        public List<Constraint> Split(Constraint other, double eps)
        {
            if (this.Equals(other) || this.Contains(other.start, eps) || this.Contains(other.end, eps))
            {
                return [this];
            }

            Vertex? inter = Vertex.Intersect(start, end, other.start, other.end);
            if (inter == null)
            {
                return [this];
            }

            List<Constraint> result = this.Split(inter, eps);
            result.AddRange(other.Split(inter, eps));
            return result;
        }

        public bool Equals(Constraint other)
        {
            return start.Index == other.start.Index && end.Index == other.end.Index;
        }

        public override bool Equals(object? obj)
        {
            return obj is Constraint other && Equals(other);
        }

        public override int GetHashCode() => HashCode.Combine(start.Index, end.Index);

        public override string ToString()
        {
            return $"[{type}] {start.Index} {end.Index}";
        }
    }
}
