namespace CDTlib
{
    public class CDTPolygon
    {
        public int Index { get; set; } = -1;
        public List<CDTSegment> Contour { get; set; } = new List<CDTSegment>();
        public List<CDTPolygon> Holes { get; set; } = new List<CDTPolygon>();
        public List<CDTNode> Points { get; set; } = new List<CDTNode>();
        public List<CDTSegment> Segments { get; set; } = new List<CDTSegment>();

        public List<CDTNode> GetPoints()
        {
            List<CDTNode> points = new List<CDTNode>();
            foreach (CDTSegment segment in Contour)
            {
                foreach (CDTSegment s in segment.Split())
                {
                    points.Add(s.Start);
                }
            }
            return points;
        }
    }
}
