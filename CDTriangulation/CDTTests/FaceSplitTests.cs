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

        }
    }
}
