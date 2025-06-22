using CDTlib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTTests
{
    public class TopologyTests
    {
        Mesh TestCase()
        {
            /*
              5-------6------7
              |\  4  /\   5 /|
              | \   /  \   / |
              |  \ /  0 \ /  |
              | 6 3------4  7|
              |  / \  1 / \  |
              | /   \  /   \ |
              |/  2  \/   3 \|
              0-------1------2
            */

            Node n0 = new Node(0, -50, -50, 0);
            Node n1 = new Node(1, 0, -50, 0);
            Node n2 = new Node(2, 50, -50, 0);

            Node n3 = new Node(3, -25, 0, 0);
            Node n4 = new Node(4, 25, 0, 0);

            Node n5 = new Node(5, -50, 50, 0);
            Node n6 = new Node(6, 0, 50, 0);
            Node n7 = new Node(7, 50, 50, 0);

            Triangle t0 = new Triangle(0, n3, n4, n6);
            Triangle t1 = new Triangle(1, n1, n4, n3);
            Triangle t2 = new Triangle(2, n0, n1, n3);
            Triangle t3 = new Triangle(3, n1, n2, n4);
            Triangle t4 = new Triangle(4, n6, n5, n3);
            Triangle t5 = new Triangle(5, n6, n4, n7);
            Triangle t6 = new Triangle(6, n3, n5, n0);
            Triangle t7 = new Triangle(7, n4, n2, n7);

            Mesh mesh = new Mesh();

            foreach (var item in new List<Node>() { n0, n1, n2, n3, n4, n5, n6, n7})
            {
                mesh.Nodes.Add(item);
            }

            foreach (var item in new List<Triangle>() { t0, t1, t2, t3, t4, t5, t6, t7})
            {
                mesh.Triangles.Add(item);
            }

            foreach (var item in mesh.Triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    item.Edge(i, out int a, out int b);

                    mesh.FindEdgeBrute(b, a, out int tri, out int edge);

                    if (edge == -1)
                    {
                        continue;
                    }

                    mesh.Triangles[tri].adjacent[edge] = item.index;
                    item.adjacent[edge] = tri;
                }
            }

            return mesh;

        }

        [Fact]
        public void FindContaining_CorrectlyFindsTriangleWhenPointIsInside()
        {
            var mesh = TestCase();

            foreach (Triangle item in mesh.Triangles)
            {
                double x = 0;
                double y = 0;
                for (int i = 0; i < 3; i++)
                {
                    Node n = mesh.Nodes[item.indices[i]];
                    x += n.X;
                    y += n.Y;
                }
                x /= 3.0; 
                y /= 3.0;

                mesh.FindContaining(x, y, out int tri, out int edge, out int node);

                Assert.Equal(-1, node);
                Assert.Equal(-1, edge);
                Assert.Equal(item.index, tri);
            }
        }

        [Fact]
        public void FindContaining_CorrectlyFindsTriangleWhenPointIsOnEdge()
        {
            var mesh = TestCase();

            foreach (Triangle item in mesh.Triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    item.Edge(i, out int start, out int end);

                    Node a = mesh.Nodes[start];
                    Node b = mesh.Nodes[end];

                    double x = (a.X + b.X) / 2.0;
                    double y = (a.Y + b.Y) / 2.0;

                    mesh.FindContaining(x, y, out int tri, out int edge, out int node);

                    mesh.FindEdgeBrute(start, end, out int expectedTri0, out int expectedEdge0);
                    mesh.FindEdgeBrute(end, start, out int expectedTri1, out int expectedEdge1);

                    Assert.Equal(-1, node);

                    if (expectedTri0 == tri)
                    {
                        Assert.Equal(expectedTri0, tri);
                        Assert.Equal(expectedEdge0, edge);
                    }
                    else if (expectedTri1 == tri)
                    {
                        Assert.Equal(expectedTri1, tri);
                        Assert.Equal(expectedEdge1, edge);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
        }
    }
}
