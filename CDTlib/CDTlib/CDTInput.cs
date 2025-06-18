namespace CDTlib
{
    public class CDTInput
    {
        public CDTQuality Quality { get; set; } = new CDTQuality();
        public List<CDTPolygon> Polygons { get; set; } = new List<CDTPolygon>();
        public List<CDTPoint> Points { get; set; } = new List<CDTPoint>();
        public List<CDTSegment> Segments { get; set; } = new List<CDTSegment>();
    }
}
