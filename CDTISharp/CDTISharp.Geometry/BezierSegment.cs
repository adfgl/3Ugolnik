
namespace CDTISharp.Geometry
{
    public class BezierSegment : Segment
    {
        private readonly List<Node> _controlPoints;

        public IReadOnlyList<Node> ControlPoints => _controlPoints;

        public BezierSegment(IEnumerable<Node> points) : base(null!, null!)
        {
            _controlPoints = points.ToList();
            if (_controlPoints.Count < 2)
                throw new ArgumentException("A Bezier segment must have at least two control points.");

            _start = _controlPoints.First();
            _end = _controlPoints.Last();
        }

        public override Node PointAt(double t)
        {
            t = Math.Clamp(t, 0, 1);
            return DeCasteljau(_controlPoints, t);
        }

        private static Node DeCasteljau(List<Node> points, double t)
        {
            while (points.Count > 1)
            {
                List<Node> next = new List<Node>(points.Count - 1);
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Node a = points[i];
                    Node b = points[i + 1];
                    next.Add(Interpolate(a, b, t));
                }
                points = next;
            }
            return points[0];
        }

        public override double Length()
        {
            const int resolution = 20;
            double length = 0;
            Node prev = PointAt(0);
            for (int i = 1; i <= resolution; i++)
            {
                double t = (double)i / resolution;
                Node curr = PointAt(t);
                length += Math.Sqrt(GeometryHelper.SquareLength(prev, curr));
                prev = curr;
            }
            return length;
        }

        public override Segment[] Split(int parts)
        {
            parts = Math.Max(parts, 1);
            Segment[] segments = new Segment[parts];
            for (int i = 0; i < parts; i++)
            {
                double t0 = (double)i / parts;
                double t1 = (double)(i + 1) / parts;

                List<Node> subCurve = Subdivide(_controlPoints, t0, t1, 20);
                segments[i] = new BezierSegment(subCurve) { Data = this.Data };
            }
            return segments;
        }

        static Node Interpolate(Node a, Node b, double t)
        {
            return new Node(-1,
                a.X + t * (b.X - a.X),
                a.Y + t * (b.Y - a.Y));
        }

        static List<Node> Subdivide(List<Node> controlPoints, double t0, double t1, int resolution)
        {
            resolution = Math.Max(1, resolution);
            List<Node> result = new List<Node>(resolution);
            for (int i = 0; i <= resolution; i++)
            {
                double t = t0 + (t1 - t0) * i / resolution;
                result.Add(DeCasteljau(controlPoints.ToList(), t));
            }
            return result;
        }
    }
}
