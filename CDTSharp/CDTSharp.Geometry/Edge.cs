namespace CDTSharp.Geometry
{
    public class Edge
    {
        public Edge(Node origin)
        {
            Origin = origin;
        }

        public void Deconstruct(out Node start, out Node end)
        {
            start = Origin;
            end = Next.Origin;
        }

        public Node Origin { get; }
        public Edge Next { get; set; } = null!;
        public Edge Prev { get; set; } = null!;
        public Edge? Twin { get; set; } = null;
        public Triangle Triangle { get; set; } = null!;
        public EConstraint Constrained { get; set; } = EConstraint.None;

        public double SquareLength()
        {
            var (sx, sy) = Origin;
            var (ex, ey) = Next.Origin;
            double dx = ex - sx;
            double dy = ey - sy;
            return dx * dx + dy * dy;
        }

        public bool Contains(Node node)
        {
            return Origin == node || Next.Origin == node;
        }

        public double Orientation(double x, double y)
        {
            return GeometryHelper.Cross(Origin, Next.Origin, x, y);
        }

        public void SetConstraint(EConstraint value)
        {
            Constrained = value;
            if (Twin is not null)
            {
                Twin.Constrained = value;
            }
        }

        public override string ToString()
        {
            return $"t{Triangle.Index} [{Origin.Index}, {Next.Origin.Index}]";
        }
    }
}
