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
    }
}
