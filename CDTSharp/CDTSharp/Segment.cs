using System.Runtime.CompilerServices;

namespace CDTSharp
{
    public abstract class Segment
    {
        int _segments = 1;
        protected Node _start, _end;

        protected Segment(Node start, Node end)
        {
            _start = start;
            _end = end;
        }

        public Node Start => _start;
        public Node End => _end;

        public int Segments
        {
            get => _segments;
            set
            {
                _segments = Math.Max(_segments, 1);
            }
        }

        /// <summary>
        /// Gets a point on the segment at t in [0,1]
        /// </summary>
        /// <param name="t">offset along segment [0..1]</param>
        /// <returns></returns>
        public abstract Node PointAt(double t);
        public abstract double Length();
        public abstract Segment[] Split(int parts);
        public Segment[] Split() => Split(_segments);
    }

    public class LineSegment : Segment
    {
        public LineSegment(Node start, Node end) : base(start, end)
        {
        }

        public override Node PointAt(double t)
        {
            return new Node(-1,
                _start.X + t * (_end.X - _start.X),
                _start.Y + t * (_end.Y - _start.Y)
            );
        }

        public override double Length()
        {
            return GeometryHelper.Distance(_start, _end);
        }

        public override Segment[] Split(int parts)
        {
            parts = Math.Max(parts, 1);
            Segment[] segments = new Segment[parts];
            for (int i = 0; i < parts; i++)
            {
                double t0 = (double)i / parts;
                double t1 = (double)(i + 1) / parts;
            
                Node start = PointAt(t0);
                Node end = PointAt(t1);
                segments[i] = new LineSegment(start, end);
            }
            return segments;
        }
    }

    public class ArcSegment : Segment
    {
        public ArcSegment(Node start, Node end, Node center, bool clockwise) : base(start, end)
        {
            Center = center;
            Clockwise = clockwise;
        }

        public ArcSegment(double radius, double startAngle, double endAngle, Node center, bool clockwise)
       : base(
           new Node(-1, center.X + radius * Math.Cos(startAngle), center.Y + radius * Math.Sin(startAngle)),
           new Node(-1, center.X + radius * Math.Cos(endAngle), center.Y + radius * Math.Sin(endAngle))
         )
        {
            Center = center;
            Clockwise = clockwise;
        }

        public bool Clockwise { get; set; }
        public Node Center { get; set; }

        public override Node PointAt(double t)
        {
            double cx = Center.X;
            double cy = Center.Y;

            double dx1 = _start.X - cx;
            double dy1 = _start.Y - cy;
            double radius = Math.Sqrt(dx1 * dx1 + dy1 * dy1);

            double angleStart = Math.Atan2(dy1, dx1);
            double dx2 = _end.X - cx;
            double dy2 = _end.Y - cy;
            double angleEnd = Math.Atan2(dy2, dx2);

            angleStart = NormalizeAngle(angleStart);
            angleEnd = NormalizeAngle(angleEnd);

            double angleDelta;
            if (Clockwise)
            {
                if (angleStart < angleEnd)
                {
                    angleStart += 2 * Math.PI;
                }
                   
                angleDelta = angleStart - angleEnd;
            }
            else
            {
                if (angleEnd < angleStart)
                {
                    angleEnd += 2 * Math.PI;
                }
                    
                angleDelta = angleEnd - angleStart;
            }

            double angle = angleStart + (Clockwise ? -1 : 1) * angleDelta * t;
            double x = cx + radius * Math.Cos(angle);
            double y = cy + radius * Math.Sin(angle);
            return new Node(-1, x, y);
        }

        public override double Length()
        {
            double dx = _start.X - Center.X;
            double dy = _start.Y - Center.Y;
            double radius = Math.Sqrt(dx * dx + dy * dy);

            double angleStart = NormalizeAngle(Math.Atan2(dy, dx));
            double angleEnd = NormalizeAngle(Math.Atan2(_end.Y - Center.Y, _end.X - Center.X));

            double angleDelta;
            if (Clockwise)
            {
                if (angleStart < angleEnd)
                    angleStart += 2 * Math.PI;
                angleDelta = angleStart - angleEnd;
            }
            else
            {
                if (angleEnd < angleStart)
                    angleEnd += 2 * Math.PI;
                angleDelta = angleEnd - angleStart;
            }

            return radius * angleDelta;
        }

        public override Segment[] Split(int parts)
        {
            parts = Math.Max(parts, 1);
            Segment[] segments = new Segment[parts];
            for (int i = 0; i < parts; i++)
            {
                double t0 = (double)i / parts;
                double t1 = (double)(i + 1) / parts;
                Node start = PointAt(t0);
                Node end = PointAt(t1);
                segments[i] = new ArcSegment(start, end, Center, Clockwise);
            }
            return segments;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        static double NormalizeAngle(double angle)
        {
            while (angle < 0) angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
            return angle;
        }
    }

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
                length += GeometryHelper.Distance(prev, curr);
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
                segments[i] = new BezierSegment(subCurve);
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
