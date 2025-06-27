namespace CDTSharp.Geometry
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
}
