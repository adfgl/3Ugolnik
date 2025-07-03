using CDTISharp.Geometry;
using CDTISharp.Meshing;
using System.Diagnostics;

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

        public static Mesh Case2()
        {
            /*    
                   +-----+-----+  v0   v1  v2
                   |\ 1 / \ 3 /|
                   |0\ / 2 \ /4|
                   +--+-----+--+  v3 v4 v5 v6
                   |9/ \ 7 / \5|
                   |/ 8 \ / 6 \|
                   +-----+-----+  v7   v8  v9
            */

            Node v0 = new Node(0, -100, +100);
            Node v1 = new Node(1, +0, +100);
            Node v2 = new Node(2, +100, +100);

            Node v3 = new Node(3, -100, 0);
            Node v4 = new Node(4, -50, 0);
            Node v5 = new Node(5, +50, 0);
            Node v6 = new Node(6, +100, 0);

            Node v7 = new Node(7, -100, -100);
            Node v8 = new Node(8, +0, -100);
            Node v9 = new Node(9, +100, -100);

            Triangle t0 = new Triangle(0, v3, v4, v0);
            Triangle t1 = new Triangle(1, v1, v0, v4);
            Triangle t2 = new Triangle(2, v4, v5, v1);
            Triangle t3 = new Triangle(3, v2, v1, v5);
            Triangle t4 = new Triangle(4, v5, v6, v2);

            Triangle t5 = new Triangle(5, v9, v6, v5);
            Triangle t6 = new Triangle(6, v8, v9, v5);
            Triangle t7 = new Triangle(7, v5, v4, v8);
            Triangle t8 = new Triangle(8, v7, v8, v4);
            Triangle t9 = new Triangle(9, v4, v3, v7);

            Mesh mesh = new Mesh([t0, t1, t2, t3, t4, t5, t6, t7, t8, t9], [v0, v1, v2, v3, v4, v5, v6, v7, v8, v9]).BruteForceTwins();
            return mesh;
        }

    }
}
