namespace CDTSharp.IO
{
    public class CDTInput
    {
        public List<CDTSegment> Contour { get; set; } = new List<CDTSegment>();
        public List<List<CDTSegment>> Holes { get; set; } = new List<List<CDTSegment>>();
        public List<CDTSegment> ConstraintEdges { get; set; } = new List<CDTSegment>();
        public List<CDTNode> ConstraintNodes { get; set; } = new List<CDTNode>();
        public CDTQuality? Quality { get; set; } = null;
    }
}
