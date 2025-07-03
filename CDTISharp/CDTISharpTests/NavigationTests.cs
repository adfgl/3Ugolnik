using CDTISharp.Geometry;
using CDTISharp.Meshing;

namespace CDTISharpTests
{
    public class NavigationTests
    {
        [Fact]
        public void FindContaining_WhenNode()
        {
            Mesh m = TestCases.Case2();
            foreach (Triangle item in m.Triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    Node n = m.Nodes[item.indices[i]];

                    Node nCopy = new Node() { X = n.X, Y = n.Y };

                    List<int> path = new List<int>();

                    SearchResult? result = Navigation.FindContaining(m.Triangles, m.Nodes, nCopy, path, 0);

                    Assert.NotNull(result);

                    int index = item.IndexOf(result.Node);

                    Assert.NotEqual(-1, index);
                }
            }

            //Node n = m.Nodes[6];

            //Node nCopy = new Node() { X = n.X, Y = n.Y };

            //List<int> path = new List<int>();

            //SearchResult? result = Navigation.FindContaining(m.Triangles, m.Nodes, nCopy, path, 0);

            //Assert.NotNull(result);


        }

    }
}
