using CDTlib;
using CDTlib.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTTests
{
    public class TriangleSplitTests
    {
        static void SetAllTwins(List<Triangle> triangles)
        {
            Dictionary<(Node, Node), Edge> edgeMap = new();

            foreach (Triangle tri in triangles)
            {
                foreach (Edge edge in tri)
                {
                    (Node, Node) key = (edge.Origin, edge.Next.Origin);     
                    (Node, Node) twinKey = (edge.Next.Origin, edge.Origin); 
                    if (edgeMap.TryGetValue(twinKey, out Edge? twin))
                    {
                        CDT.SetTwins(edge, twin);
                    }
                    else
                    {
                        edgeMap[key] = edge;
                    }
                }
            }
        }

        static List<Triangle> TestCase()
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

            Node v0 = new Node(0, -50, -50);
            Node v1 = new Node(1, 0, -50);
            Node v2 = new Node(2, +50, -50);

            Node v3 = new Node(3, -25, 0);
            Node v4 = new Node(4, +25, 0);

            Node v5 = new Node(5, -50, +50);
            Node v6 = new Node(6, 0, +50);
            Node v7 = new Node(7, +50, +50);

            List<Triangle> triangles = [
                CDT.BuildTriangle(v3, v4, v6, 0),
                CDT.BuildTriangle(v1, v4, v3, 1),
                CDT.BuildTriangle(v0, v1, v3, 2),
                CDT.BuildTriangle(v1, v2, v4, 3),
                CDT.BuildTriangle(v3, v6, v5, 4),
                CDT.BuildTriangle(v4, v7, v6, 5),
                CDT.BuildTriangle(v0, v3, v5, 6),
                CDT.BuildTriangle(v4, v2, v7, 7),
            ];

            foreach (Triangle item in triangles)
            {
                item.Area = CDT.Area(item);
                if (item.Area < 0)
                {
                    throw new Exception();
                }
            }

            SetAllTwins(triangles);
            return triangles;
        }

        //[Fact]
        //public void Orientation_WhenPointToTheLeft()
        //{
        //    EOrientation actual = GeometryHelper.Orientation(-100, 0, 0, -100, 0, 100);
        //    Assert.Equal(EOrientation.Left, actual);
        //}

        //[Fact]
        //public void Orientation_WhenPointToTheRight()
        //{
        //    EOrientation actual = GeometryHelper.Orientation(+100, 0, 0, -100, 0, 100);
        //    Assert.Equal(EOrientation.Right, actual);
        //}

        [Fact]
        public void TriangleIsSplitCorrectly()
        {
            Node v0 = new Node(0, -100, -100);
            Node v1 = new Node(1, +100, -100);
            Node v2 = new Node(2, 0, +100);
            Node v3 = new Node(3, 0, 0);

            Triangle tri = CDT.BuildTriangle(v0, v1, v2, 0);
            tri.Area = CDT.Area(tri);

            List<Triangle> tris = [tri];

            Triangle[] newTris = CDT.SplitTriangle(tris.Count, tri, v3, out Edge[] affected);
            Assert.Equal(3, newTris.Length);    

            Triangle t013 = newTris[0];
            Edge e01 = t013.Edge;
            Edge e13 = e01.Next;
            Edge e30 = e13.Next;

            Triangle t123 = newTris[1];
            Edge e12 = t123.Edge;
            Edge e23 = e12.Next;
            Edge e31 = e23.Next;

            Triangle t203 = newTris[2];
            Edge e20 = t203.Edge;
            Edge e03 = e20.Next;
            Edge e32 = e03.Next;

            Assert.Null(e01.Twin);
            Assert.False(e01.Constrained);
            Assert.Equal(e13.Twin, e31);
            Assert.Equal(e30.Twin, e03);

            Assert.Null(e12.Twin);
            Assert.False(e12.Constrained);
            Assert.Equal(e23.Twin, e32);
            Assert.Equal(e31.Twin, e13);

            Assert.Null(e20.Twin);
            Assert.False(e20.Constrained);
            Assert.Equal(e03.Twin, e30);
            Assert.Equal(e32.Twin, e23);
        }

        [Fact]
        public void FindContaining_CorrectlyFindsTriangleWhenPointIsInside()
        {
            List<Triangle> tris = TestCase();
            foreach (Triangle item in tris)
            {
                item.Center(out double cx, out double cy);

                (Triangle? t, Edge? e, Node? n) = CDT.FindContaining(tris, cx, cy);

                Assert.Null(e);
                Assert.Null(n);
                Assert.Equal(item, t);
            }
        }
    }
}
