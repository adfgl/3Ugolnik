namespace CDTISharp.IO
{
    public class CDTMesh
    {
        public CDTTriangle[] Triangles { get; set; } = Array.Empty<CDTTriangle>();
        public CDTNode[] Nodes { get; set; } = Array.Empty<CDTNode>();
        public CDTSummary? Summary { get; set; } = null;
    }
}
