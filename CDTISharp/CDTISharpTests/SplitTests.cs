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
            Assert.Equal(3, tris.Length);

            int[] expectedIndices = [t3.index, m.Triangles.Count, m.Triangles.Count + 1];
            for (int i = 0; i < 3; i++)
            {
                Triangle curr = tris[i];
                Assert.Equal(expectedIndices[i], curr.index);

                Triangle next = tris[(i + 1) % 3];
                Triangle prev = tris[(i + 2) % 3];

                Assert.Equal(t3.adjacent[i], curr.adjacent[0]);
                Assert.Equal(next.index, curr.adjacent[1]);
                Assert.Equal(prev.index, curr.adjacent[2]);

                Assert.Equal(t3.constraints[i], curr.constraints[0]);
                if (i != 0)
                {
                    Assert.Equal(-1, curr.constraints[i]);
                }
            }
        }
    }
}
