namespace CDTSharp
{
    public class HeEdge
    {
        public HeEdge(HeNode origin)
        {
            Origin = origin;
        }

        public HeNode Origin { get; }
        public HeEdge Next { get; set; } = null!;
        public HeEdge? Twin { get; set; } = null;
        public HeTriangle Triangle { get; set; } = null!;
        public bool Constrained { get; set; } = false;

        public double LengthSquared()
        {
            HeNode start = Origin;
            HeNode end = Next.Origin;

            double dx = end.X - start.X;
            double dy = end.Y - start.Y;

            return dx * dx + dy * dy;
        }

        public double Length()
        {
            return Math.Sqrt(LengthSquared());
        }

        public EOrientation Orientation(double x, double y, double eps = 0)
        {
            var (sx, sy) = Origin;
            var (ex, ey) = Next.Origin;

            double cross = GeometryHelper.Cross(sx, sy, ex, ey, x, y);
            if (Math.Abs(cross) <= eps)
            {
                return EOrientation.Colinear;
            }
            return cross > 0 ? EOrientation.Left : EOrientation.Right;
        }
    }
}
