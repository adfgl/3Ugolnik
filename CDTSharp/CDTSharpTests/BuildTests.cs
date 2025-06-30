using CDTSharp.Geometry;

namespace CDTSharpTests
{
    public class BuildTests
    {
        [Fact]
        public void TriangleIsBuiltCorrectly()
        {
            Node a = new Node(0, -100, 0);
            Node b = new Node(1, +100, 0);
            Node c = new Node(2, 0, +100);

            Triangle t = new Triangle(0, a, b, c);

            t.Edges(out Edge ab, out Edge bc, out Edge ca);

            Edge[] edges = t.Forward().ToArray();
            Node[] nodes = [a, b, c];
            for (int i = 0; i < 3; i++)
            {
                Node node = nodes[i];
                Edge curr = edges[i];
                Edge next = edges[(i + 1) % 3];
                Edge prev = edges[(i + 2) % 3];

                Assert.Equal(next, curr.Next);
                Assert.Equal(prev, curr.Prev);
                Assert.Null(curr.Twin);
                
                Assert.Equal(t, curr.Triangle);

                Assert.Equal(node, curr.Origin);
                Assert.Equal(curr.Origin, node);
            }
        }
    }
}
