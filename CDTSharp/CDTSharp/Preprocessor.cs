using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public class Preprocessor
    {
    }

    public enum EConstraint
    {
        User, Contour, Hole
    }

    public readonly struct Constraint
    {
        public readonly Node a, b;
        public readonly EConstraint type;

        public Constraint(Node a, Node b, EConstraint type)
        {
            this.a = a;
            this.b = b;
            this.type = type;
        }

        static bool CloseOrEqual(Node a, Node b, double eps = 1e-6)
        {
            if (a.Equals(b)) return true;
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return dx * dx + dy * dy < eps;
        }

        public List<Constraint> Split(Node other)
        {
            if (CloseOrEqual(a, other) || CloseOrEqual(b, other))
            {
                return [this];
            }
            return [new Constraint(a, other, type), new Constraint(other, b, type)];
        }

        public List<Constraint> Split(Constraint other)
        {
            Node? inter = GeometryHelper.Intersect(a, b, other.a, other.b);
            if (inter == null ||
                CloseOrEqual(a, other.a) || CloseOrEqual(b, other.b) ||
                CloseOrEqual(a, other.b) || CloseOrEqual(b, other.a))
            {
                return [this];
            }

            List<Constraint> segments = Split(inter);
            segments.AddRange(other.Split(inter));
            return segments;
        }

        public override string ToString()
        {
            return $"[{type}] {a.Index} {b.Index}";
        }
    }
}
