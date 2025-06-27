namespace CDTSharp.IO
{
    public class CDTMesh
    {
        public List<CDTTriangle> Triangles { get; set; } = new List<CDTTriangle>();
        public List<CDTNode> Nodes { get; set; } = new List<CDTNode>();
        public CDTSummary? Summary {  get; set; } = null;
    }
}
