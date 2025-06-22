
namespace CDTlib
{
    public class CDTBezierSegment : CDTSegment
    {
        public IReadOnlyList<CDTNode> ControlPoints { get; }

        public override CDTNode Start => ControlPoints[0];
        public override CDTNode End => ControlPoints[^1];

        public override double Length
        {
            get
            {
                const int samples = 16;
                double length = 0;
                CDTNode prev = PointAt(0);
                for (int i = 1; i <= samples; i++)
                {
                    double t = (double)i / samples;
                    CDTNode next = PointAt(t);
                    length += Distance(prev, next);
                    prev = next;
                }
                return length;
            }
        }

        public CDTBezierSegment(params CDTNode[] controlPoints)
        {
            if (controlPoints.Length < 2)
                throw new ArgumentException("A Bézier segment must have at least two control points.");

            ControlPoints = controlPoints;
        }

        public override CDTNode PointAt(double t)
        {
            // De Casteljau's algorithm
            var points = ControlPoints.Select(p => new CDTNode { X = p.X, Y = p.Y, Z = p.Z }).ToArray();
            int count = points.Length;

            for (int r = 1; r < count; r++)
            {
                for (int i = 0; i < count - r; i++)
                {
                    points[i].X = (1 - t) * points[i].X + t * points[i + 1].X;
                    points[i].Y = (1 - t) * points[i].Y + t * points[i + 1].Y;
                    points[i].Z = (1 - t) * points[i].Z + t * points[i + 1].Z;
                }
            }

            return points[0];
        }

        public override IReadOnlyList<CDTSegment> Split(int parts)
        {
            var list = new List<CDTSegment>(parts);
            for (int i = 0; i < parts; i++)
            {
                double t0 = (double)i / parts;
                double t1 = (double)(i + 1) / parts;
                CDTNode p0 = PointAt(t0);
                CDTNode p1 = PointAt(t1);
                list.Add(new CDTLineSegment(p0, p1));
            }
            return list;
        }
    }
}
