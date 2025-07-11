﻿using CDTISharp.Geometry;
using System.Collections.Generic;

namespace CDTISharp.Meshing
{
    public readonly struct Constraint : IEquatable<Constraint>
    {
        public readonly Node start, center, end;
        public readonly int type;
        public readonly Circle circle;
        public readonly Rectangle rectangle;

        public Constraint(Node a, Node b, int type)
        {
            this.circle = new Circle(a.X, a.Y, b.X, b.Y);
            this.type = type;
            this.center = new Node() { X = circle.x, Y = circle.y, Z = (a.Z + b.Z) * 0.5 };
            this.rectangle = new Rectangle(a, b);

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

        public bool Degenerate(double eps) => GeometryHelper.CloseOrEqual(start, end, eps);

        public bool VisibleFromInterior(IEnumerable<Constraint> segments, Node pt)
        {
            Rectangle rect = new Rectangle(pt, center);
            foreach (Constraint s in segments)
            {
                if (this.Equals(s) || !s.rectangle.Intersects(rect))
                    continue;

                if (GeometryHelper.Intersect(center, pt, s.start, s.end) is not null)
                {
                    return false;
                }
            }
            return true;
        }

        public bool Contains(Node node, double eps = 1e-6)
        {
            return GeometryHelper.CloseOrEqual(start, node, eps) || GeometryHelper.CloseOrEqual(end, node, eps);
        }

        public bool Enchrouched(List<Node> nodes, double eps = 1e-6)
        {
            foreach (Node item in nodes)
            {
                if (circle.Contains(item.X, item.Y) && !Contains(item, eps))
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
            return [new Constraint(this.start, node, type), new Constraint(node, this.end, type)];
        }

        public List<Constraint> Split(Constraint other, double eps = 1e-6)
        {
            if (this.Equals(other) || this.Contains(other.start, eps) || this.Contains(other.end, eps))
            {
                return [this];
            }

            Node? inter = GeometryHelper.Intersect(start, end, other.start, other.end);
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
