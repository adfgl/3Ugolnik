namespace CDTlib
{
    public class CDTPolygon
    {
        public List<CDTSegment> Contour { get; set; } = new List<CDTSegment>();
        public List<CDTPolygon> Holes { get; set; } = new List<CDTPolygon>();
        public List<CDTPoint> Points { get; set; } = new List<CDTPoint>();
        public List<CDTSegment> Segments { get; set; } = new List<CDTSegment>();

        public List<CDTPoint> GetPoints()
        {
            List<CDTPoint> points = new List<CDTPoint>();
            foreach (CDTSegment segment in Segments)
            {
                foreach (CDTSegment s in segment.Split(segment.NumSegments))
                {
                    points.Add(s.Start);
                }
            }
            return points;
        }
    }
}
