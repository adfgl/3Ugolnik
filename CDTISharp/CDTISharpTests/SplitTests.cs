using CDTISharp.Geometry;
using CDTISharp.Meshing;

namespace CDTISharpTests
{
    public class SplitTests
    {
        [Fact]
        public void TriangleSplit()
        {
            Mesh m = TestCases.Case1();

            Triangle t3 = m.Triangles[3];
            Assert.Equal(3, t3.index);

            Node toInsert = m.Center(t3); 
            toInsert.Index = m.Nodes.Count;

            Triangle[] tris = Splitting.Split(m.Triangles, m.Nodes, t3.index, toInsert);

        }
    }
}
