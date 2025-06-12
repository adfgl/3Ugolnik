using CDTlib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTTests
{
    public class FaceBuildTests
    {
        static (Node a, Node b, Node c) CreateNodes()
        {
            return (new Node(0, 0, 0), new Node(1, 1, 0), new Node(2, 0, 1));
        }

        [Fact]
        public void Constructor_WithThreeNodes_CreatesLinkedTriangle()
        {
            var (a, b, c) = CreateNodes();

            var face = new Face(1, a, b, c);

            Edge ab = face.Edge;
            Edge bc = ab.Next;
            Edge ca = bc.Next;

            Assert.Equal(ab.Prev, ca);
            Assert.Equal(bc.Prev, ab);
            Assert.Equal(ca.Prev, bc);

            Assert.Equal(ab.Next, bc);
            Assert.Equal(bc.Next, ca);
            Assert.Equal(ca.Next, ab);

            Assert.Equal(ab.Face, face);
            Assert.Equal(bc.Face, face);
            Assert.Equal(ca.Face, face);

            Assert.Equal(a.Edge, ab);
            Assert.Equal(b.Edge, bc);
            Assert.Equal(c.Edge, ca);
        }
    }
}
