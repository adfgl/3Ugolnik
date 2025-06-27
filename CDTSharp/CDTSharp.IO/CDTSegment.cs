namespace CDTSharp.IO
{
    public abstract class CDTSegment
    {
        protected CDTSegment()
        {
            Start = new CDTNode();
            End = new CDTNode();
        }

        protected CDTSegment(CDTNode start, CDTNode end)
        {
            Start = start; 
            End = end;
        }

        public CDTSegmentType Type { get; set; }
        public CDTNode Start { get; set; }
        public CDTNode End { get; set; }
        public int Segments { get; set; } = 1;
    }

    public class CDTLineSegment : CDTSegment
    {
        public CDTLineSegment() : base()
        {
            Type = CDTSegmentType.Line;
        }

        public CDTLineSegment(CDTNode start, CDTNode end) : base(start, end)
        {
            Type = CDTSegmentType.Line;
        }
    }

    public class CDTArcSegment : CDTSegment
    {
        public CDTArcSegment() : base()
        {
            Center = new CDTNode();
            Type = CDTSegmentType.Arc;
        }

        public CDTArcSegment(CDTNode start, CDTNode end, CDTNode center, bool clockwise) : base(start, end) 
        {
            Center = center;
            Clockwise = clockwise;
            Type = CDTSegmentType.Arc;
        }

        public CDTNode Center { get; set; }
        public bool Clockwise { get; set; }
    }

    public class CDTBezierSegment : CDTSegment
    {
        public CDTBezierSegment() : base()
        {
            ControlPoints = new List<CDTNode>();
            Type = CDTSegmentType.Bezier;
        }

        public CDTBezierSegment(IEnumerable<CDTNode> controlPoints) : base(null!, null!)
        {
            ControlPoints = controlPoints.ToList();
            Start = controlPoints.First();
            End = controlPoints.Last();
        }

        public List<CDTNode> ControlPoints { get; set; }
    }
}
