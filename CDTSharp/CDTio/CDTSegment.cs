namespace CDTio
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

        public CDTNode Start { get; set; }
        public CDTNode End { get; set; }
    }

    public class CDTLineSegment : CDTSegment
    {
        public CDTLineSegment() : base()
        {
            
        }

        public CDTLineSegment(CDTNode start, CDTNode end) : base(start, end)
        {
        }
    }

    public class CDTArcSegment : CDTSegment
    {
        public CDTArcSegment() : base()
        {
            Center = new CDTNode();
        }

        public CDTArcSegment(CDTNode start, CDTNode end, CDTNode center, bool clockwise) : base(start, end) 
        {
            Center = center;
            Clockwise = clockwise;
        }

        public CDTNode Center { get; set; }
        public bool Clockwise { get; set; }
    }

    public class CDTBezierSegment : CDTSegment
    {
        public CDTBezierSegment() : base()
        {
            ControlPoints = new List<CDTNode>();
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
