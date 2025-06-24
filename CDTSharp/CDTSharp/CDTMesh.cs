namespace CDTSharp
{
    public class CDTMesh
    {
        public CDTMesh()
        {
            
        }
        public List<CDTTriangle> Triangles { get; set; } = new List<CDTTriangle>();
        public List<CDTNode> Nodes { get; set; } = new List<CDTNode>();
        public CDTSummary? Summary {  get; set; } = null;
    }
}
