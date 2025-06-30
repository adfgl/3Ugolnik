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
        public void TriangleSplit()
        {
            Mesh m = TestCase();
            Node toInsert = Node.Average(m.Nodes[0], m.Nodes[1], m.Nodes[2]);
            toInsert.Index = m.Nodes.Count;
            Triangle t3 = m.Triangles[3];

            Triangle[] tris = m.Split(t3, toInsert);
            Assert.Equal(3, tris.Length);

            Edge[] oldEdges = t3.Forward().ToArray();
            for (int i = 0; i < 3; i++)
            {
                Triangle t = tris[i];
                Assert.Equal(oldEdges[i].Twin, t.Edge.Twin);
                Assert.Equal(oldEdges[i].Constrained, t.Edge.Constrained);
            }


        }
    }
}
