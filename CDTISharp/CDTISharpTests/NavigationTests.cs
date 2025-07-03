using CDTISharp.Geometry;
using CDTISharp.Meshing;

namespace CDTISharpTests
{
    public class NavigationTests
    {
        [Fact]
        public void FindContaining_PointInsideStart()
        {
            Mesh m = TestCases.Case2();
            Triangle t = m.Triangles[0];
            Node pt = m.Center(t);

            List<int> path = new List<int>();
            SearchResult? result = Navigation.FindContaining(m.Triangles, m.Nodes, pt, path, 0, t.index);

            Assert.NotNull(result);
            Assert.Equal(t.index, result.Triangle);
            Assert.Equal(-1, result.Edge);
            Assert.Equal(-1, result.Node);

            Assert.Single(path);
        }

        [Fact]
        public void FindContaining_WhenNode()
        {
            Mesh m = TestCases.Case2();
            foreach (Triangle item in m.Triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    Node pt = m.Nodes[item.indices[i]];

                    List<int> path = new List<int>();
                    SearchResult? result = Navigation.FindContaining(m.Triangles, m.Nodes, new Node() { X = pt.X, Y = pt.Y }, path, 0);

                    Assert.NotNull(result);
                    Assert.NotEqual(-1, result.Edge);
                    Assert.NotEqual(-1, result.Triangle);
                    Assert.NotEqual(-1, result.Node);

                    Triangle t = m.Triangles[result.Triangle];
                    Assert.NotEqual(-1, t.IndexOf(result.Node));
                    Assert.Equal(pt, m.Nodes[result.Node]);
                }
            }
        }

        [Fact]
        public void FindContaining_WhenEdge()
        {
            Mesh m = TestCases.Case2();
            foreach (Triangle item in m.Triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    item.Edge(i, out int startIndex, out int endIndex);
                    Node start = m.Nodes[startIndex];
                    Node end = m.Nodes[endIndex];

                    Node pt = Node.Between(start, end);

                    List<int> path = new List<int>();
                    SearchResult? result = Navigation.FindContaining(m.Triangles, m.Nodes, pt, path, 0);

            

                    Assert.NotNull(result);

                    if (result.Edge == -1)
                    {

                    }

                    Assert.NotEqual(-1, result.Edge);
                    Assert.NotEqual(-1, result.Triangle);
                    Assert.Equal(-1, result.Node);


                }
            }
        }

    }
}
