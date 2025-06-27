namespace CDTSharp.IO
{
    public class CDTInput
    {
        public List<CDTSegment> Contour { get; set; }
        public List<List<CDTSegment>> Holes { get; set; }
        public List<CDTSegment> ConstraintEdges { get; set; }
        public List<CDTNode> ConstraintNodes { get; set; }
        public CDTQuality? Quality { get; set; } = null;
    }
}
