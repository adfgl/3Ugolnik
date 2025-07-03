using CDTISharp.Meshing;

namespace CDTISharpTests
{
    public class TriangleWalkerTests
    {
        [Fact]
        public void TriangleWalker_VisitsTrianglesCorrectly()
        {
            Mesh m = TestCases.Case2();
            List<Triangle> tris = TriangleWalker.GetTriangles(m.Triangles, m.Nodes[4]);
            Assert.Equal(6, tris.Count);
        }

    }
}
