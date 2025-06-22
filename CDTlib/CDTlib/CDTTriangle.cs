namespace CDTlib
{
    public class CDTTriangle
    {
        public int Index { get; set; } = -1;
        public double Area { get; set; }    
        public CDTNode[] Nodes { get; set; } = Array.Empty<CDTNode>();
        public List<CDTPolygon> Parents { get; set; } = new List<CDTPolygon>();
    }
}
