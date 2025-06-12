using CDTlib.Segments;

namespace CDTlib
{
    public class CDTPolygon
    {
        public List<CDTSegment> Contour { get; set; } 
        public List<CDTPoint>? ConstraintPoints { get; set; }
        public List<CDTSegment>? Constraints { get; set; }
    }
}
