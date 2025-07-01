using CDTSharp.Geometry;
using CDTSharp.Meshing;

namespace CDTSharpTests
{
    public class SplitTests
    {
        Mesh TestCase()
        {
            /*  A           B
             *  +-----+-----+
             *   \ 0 / \ 1 /
             *    \ / 3 \ /
             *     +-----+
             *      \ 2 /
             *       \ /
             *        +
             *        C
             */


            Node a = new Node(0, -100, +100);
            Node b = new Node(1, +100, +100);
            Node c = new Node(2, 0, -100);

            Node ab = Node.Average(a, b); ab.Index = 3;
            Node bc = Node.Average(b, c); bc.Index = 4;
            Node ca = Node.Average(c, a); ca.Index = 5;

            Triangle t0 = new Triangle(0, a, ca, ab);
            Triangle t1 = new Triangle(1, b, ab, bc);
            Triangle t2 = new Triangle(2, c, bc, ca);
            Triangle t3 = new Triangle(3, ab, ca, bc);

            Mesh mesh = new Mesh([t0, t1, t2, t3], [a, b, c, ab, bc, ca]);
            mesh = mesh.BruteForceTwins();

            return mesh;
        }

        [Fact]
        public void EdgeSplit()
        {
            Mesh m = TestCase();

            Triangle t2 = m.Triangles[2];
            Edge e = t2.Edge.Next;

            Node toInsert = Node.Average(e.Origin, e.Next.Origin);
            toInsert.Index = m.Nodes.Count;

            Triangle[] tris = m.Split(e, toInsert);
            Assert.Equal(4, tris.Length);

        }

        [Fact]
        public void TriangleSplit()
        {
            Mesh m = TestCase();
            Triangle t3 = m.Triangles[3];
            Node toInsert = Node.Average(m.Nodes[0], m.Nodes[1], m.Nodes[2]);
            toInsert.Index = m.Nodes.Count;

            Triangle[] tris = m.Split(t3, toInsert);
            Assert.Equal(3, tris.Length);

            Edge[] oldEdges = t3.Forward().ToArray();
            for (int i = 0; i < 3; i++)
            {
                Edge expectedTwin = oldEdges[i];

                Triangle curr = tris[i];
                Triangle next = tris[(i + 1) % 3];
                Triangle prev = tris[(i + 2) % 3];

                Assert.NotEqual(expectedTwin.Twin, curr.Edge);

                Assert.Equal(expectedTwin.Twin, curr.Edge.Twin);
                Assert.Equal(next.Edge.Prev, curr.Edge.Next.Twin);
                Assert.Equal(prev.Edge.Next, curr.Edge.Prev.Twin);

                Assert.Equal(expectedTwin.Constrained, curr.Edge.Constrained);
                Assert.Equal(next.Edge.Prev.Constrained, curr.Edge.Next.Constrained);
                Assert.Equal(prev.Edge.Next.Constrained, curr.Edge.Prev.Constrained);
            }
        }
    }
}
