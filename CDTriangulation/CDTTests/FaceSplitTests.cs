using CDTlib;

namespace CDTTests
{
    public class FaceSplitTests
    {
        [Fact]
        public void SimpleSplitSetRelationsCorrectly()
        {
            Face face = new Face(0, 
                new Node(0, -30, -30), 
                new Node(1, +30, -30), 
                new Node(2, 0, 30));

            SplitResult result = face.Split(new Node(3, 0, 0));

            Assert.Equal(3, result.NewFaces.Length);
            Assert.Single(result.OldFaces);

            for (int i = 0; i < 3; i++)
            {
                Face curr = result.NewFaces[i];
                Face next = result.NewFaces[(i + 1) % 3];

                Assert.Null(curr.Edge.Twin); 
                Assert.False(curr.Edge.Constrained);

                Assert.Same(curr.Edge.Next.Twin, next.Edge.Prev);
                Assert.Same(next.Edge.Prev.Twin, curr.Edge.Next);
            }

        }
    }
}
