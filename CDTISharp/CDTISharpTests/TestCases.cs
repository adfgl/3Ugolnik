using CDTISharp.Geometry;
using CDTISharp.Meshing;

namespace CDTISharpTests
{
    public class TestCases
    {
        public static Mesh Case1()
        {
            /*  A           B
                +-----+-----+
                 \ 0 / \ 1 /
                  \ / 3 \ /
                   +-----+
                    \ 2 /
                     \ /
                      +
                      C
            */

            Node a = new Node(0, -100, +100);
            Node b = new Node(1, +100, +100);
            Node c = new Node(2, 0, -100);

            Node ab = Node.Between(a, b); ab.Index = 3;
            Node bc = Node.Between(b, c); bc.Index = 4;
            Node ca = Node.Between(c, a); ca.Index = 5;

            Triangle t0 = new Triangle(0, a, ca, ab);
            Triangle t1 = new Triangle(1, b, ab, bc);
            Triangle t2 = new Triangle(2, c, bc, ca);
            Triangle t3 = new Triangle(3, ab, ca, bc);

            Mesh mesh = new Mesh([t0, t1, t2, t3], [a, b, c, ab, bc, ca]).BruteForceTwins();
            return mesh;
        }

        

    }
}
