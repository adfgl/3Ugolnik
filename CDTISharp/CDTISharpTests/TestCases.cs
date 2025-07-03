using CDTISharp.Geometry;
using CDTISharp.Meshing;

namespace CDTISharpTests
{
    public class TestCases
    {
        public static Mesh Case1()
        {
            /*  v0    v3    v1
                +-----+-----+
                 \ 0 / \ 1 /
                  \ / 3 \ /
                v5 +-----+ v4
                    \ 2 /
                     \ /
                      +
                      v2
            */

            Node v0 = new Node(0, -100, +100);
            Node v1 = new Node(1, +100, +100);
            Node v2 = new Node(2, 0, -100);

            Node v3 = Node.Between(v0, v1); v3.Index = 3;
            Node v4 = Node.Between(v1, v2); v4.Index = 4;
            Node v5 = Node.Between(v2, v0); v5.Index = 5;

            Triangle t0 = new Triangle(0, v0, v5, v3);
            Triangle t1 = new Triangle(1, v1, v3, v4);
            Triangle t2 = new Triangle(2, v2, v4, v5);
            Triangle t3 = new Triangle(3, v3, v5, v4);

            Mesh mesh = new Mesh([t0, t1, t2, t3], [v0, v1, v2, v3, v4, v5]).BruteForceTwins();
            return mesh;
        }

        

    }
}
