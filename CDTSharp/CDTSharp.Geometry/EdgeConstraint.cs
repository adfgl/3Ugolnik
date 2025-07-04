﻿using CDTSharp.Geometry;

namespace CDTSharp.Geometry
{
    public readonly struct EdgeConstraint : IEquatable<EdgeConstraint>
    {
        public readonly Node a, b;
        public readonly EConstraint type;
        public readonly Circle circle;

        public EdgeConstraint(Node a, Node b, EConstraint type)
        {
            this.circle = new Circle(a.X, a.Y, b.X, b.Y);
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

        public bool VisibleFromInterior(IEnumerable<EdgeConstraint> segments, double x, double y)
        {
            double cx = circle.x;
            double cy = circle.y;
            foreach (EdgeConstraint s in segments)
            {
                if (!this.Equals(s) && GeometryHelper.Intersect(cx, cy, x, y, s.a.X, s.a.Y, s.b.X, s.b.Y, out _, out _))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Enchrouched(QuadTree qt, double eps = 1e-6)
        {
            Rectangle bound = new Rectangle(circle);
            List<Node> points = qt.Query(bound);
            return Enchrouched(points, eps);
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

        public bool Contains(Node node, double eps = 1e-6)
        {
            return GeometryHelper.CloseOrEqual(a, node, eps) || GeometryHelper.CloseOrEqual(b, node, eps);
        }

        public List<EdgeConstraint> Split(Node node, double eps = 1e-6)
        {
            if (Contains(node, eps))
            {
                return [this];
            }
            return [new EdgeConstraint(this.a, node, type), new EdgeConstraint(node, this.b, type)];
        }

        public List<EdgeConstraint> Split(EdgeConstraint other, double eps = 1e-6)
        {
            if (this.Contains(other.a, eps) || this.Contains(other.b, eps) || other.Contains(a, eps) || other.Contains(b, eps))
            {
                return [this];
            }

            if (GeometryHelper.Intersect(a, b, other.a, other.b, out double x, out double y))
            {
                Node node = new Node() { X = x, Y = y };
                List<EdgeConstraint> result = Split(node, eps);
                result.AddRange(other.Split(node, eps));
                return result;
            }
            return [this];
        }

        public bool Equals(EdgeConstraint other)
        {
            return a.Index == other.a.Index && b.Index == other.b.Index;
        }

        public override bool Equals(object? obj)
        {
            return obj is EdgeConstraint other && Equals(other);
        }

        public override int GetHashCode() => HashCode.Combine(a.Index, b.Index);

        public override string ToString()
        {
            return $"[{type}] {a.Index} {b.Index}";
        }
    }
}
