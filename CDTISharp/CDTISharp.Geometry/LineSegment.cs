namespace CDTISharp.Geometry
{
    public class LineSegment : Segment
    {
        public LineSegment(Node start, Node end) : base(start, end)
        {
        }

        public override Node PointAt(double t)
        {
            return new Node()
            {
                X = _start.X + t * (_end.X - _start.X),
                Y = _start.Y + t * (_end.Y - _start.Y)
            };
        }

        public override double Length()
        {
            return Math.Sqrt(GeometryHelper.SquareLength(_start, _end));
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
                segments[i] = new LineSegment(start, end) { Data = this.Data };
            }
            return segments;
        }
    }
}
