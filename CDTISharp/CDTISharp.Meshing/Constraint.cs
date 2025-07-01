using System.Collections.Generic;

namespace CDTISharp.Meshing
{
    public readonly struct Constraint : IEquatable<Constraint>
    {
        public readonly Node a, b;
        public readonly int type;
        public readonly Circle circle;

        public Constraint(Node a, Node b, int type)
        {
            this.circle = new Circle(a.X, a.Y, b.X, b.Y);
            this.type = type;
            if (a.Index < b.Index)
            {
                this.a = a;
                this.b = b;
            }
            else
            {
                this.a = b;
                this.b = a;
            }
        }

        public bool Contains(Node node, double eps = 1e-6)
        {
            return GeometryHelper.CloseOrEqual(a, node, eps) || GeometryHelper.CloseOrEqual(b, node, eps);
        }

        public bool Enchrouched(List<Node> nodes, double eps = 1e-6)
        {
            foreach (Node item in nodes)
            {
                if (!Contains(item, eps) && circle.Contains(item.X, item.Y))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Enchrouched(QuadTree qt, double eps = 1e-6)
        {
            Rectangle bound = new Rectangle(circle);
            List<Node> points = qt.Query(bound);
            return Enchrouched(points, eps);
        }

        public List<Constraint> Split(Node node, double eps = 1e-6)
        {
            if (Contains(node, eps))
            {
                return [this];
            }
            return [new Constraint(this.a, node, type), new Constraint(node, this.b, type)];
        }

        public List<Constraint> Split(Constraint other, double eps = 1e-6)
        {
            if (this.Contains(other.a, eps) || this.Contains(other.b, eps) || other.Contains(a, eps) || other.Contains(b, eps))
            {
                return [this];
            }

            if (GeometryHelper.Intersect(a.X, a.Y, b.X, b.Y, other.a.X, other.a.Y, other.b.X, other.b.Y, out double x, out double y))
            {
                Node node = new Node() { X = x, Y = y };
                List<Constraint> result = Split(node, eps);
                result.AddRange(other.Split(node, eps));
                return result;
            }
            return [this];
        }

        public bool Equals(Constraint other)
        {
            return a.Index == other.a.Index && b.Index == other.b.Index;
        }

        public override bool Equals(object? obj)
        {
            return obj is Constraint other && Equals(other);
        }

        public override int GetHashCode() => HashCode.Combine(a.Index, b.Index);

        public override string ToString()
        {
            return $"[{type}] {a.Index} {b.Index}";
        }
    }
}
