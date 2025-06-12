using CDTlib;

namespace CDTTests
{
    public class FaceSplitTests
    {
        [Fact]
        public void Split_CreatesThreeNewFacesWithCorrectEdgeTwinsAndOrigins()
        {
            Face face = new Face(0,
                new Node(0, -30, -30),
                new Node(1, +30, -30),
                new Node(2, 0, 30));

            Node center = new Node(3, 0, 0);

            SplitResult result = face.Split(center);

            Assert.Equal(3, result.NewFaces.Length);
            Assert.Single(result.OldFaces);
            Assert.Equal(face, result.OldFaces[0]);

            Face f0 = result.NewFaces[0];
            Face f1 = result.NewFaces[1];
            Face f2 = result.NewFaces[2];

            Assert.Equal(f0.Edge.Prev, f1.Edge.Twin);         
            Assert.Equal(f1.Edge.Prev, f2.Edge.Twin);         
            Assert.Equal(f2.Edge.Prev, f0.Edge.Twin);         

            Assert.Equal(f0.Edge.Next.Twin, face.Edge);       
            Assert.Equal(f1.Edge.Next.Twin, face.Edge.Next);  
            Assert.Equal(f2.Edge.Next.Twin, face.Edge.Next.Next); 

            Assert.Equal(center, f0.Edge.Origin); 
            Assert.Equal(center, f1.Edge.Origin); 
            Assert.Equal(center, f2.Edge.Origin); 

            Assert.Equal(f0.Edge.Twin, f2.Edge.Prev);
            Assert.Equal(f1.Edge.Twin, f0.Edge.Prev);
            Assert.Equal(f2.Edge.Twin, f1.Edge.Prev);

            Assert.Equal(face.Edge.Constrained, f0.Edge.Next.Constrained);
            Assert.Equal(face.Edge.Next.Constrained, f1.Edge.Next.Constrained);
            Assert.Equal(face.Edge.Next.Next.Constrained, f2.Edge.Next.Constrained);

            foreach (Face f in result.NewFaces)
            {
                Assert.NotNull(f.Edge.Next);
                Assert.NotNull(f.Edge.Prev);
                Assert.Same(f, f.Edge.Face);
                Assert.Same(f, f.Edge.Next.Face);
                Assert.Same(f, f.Edge.Prev.Face);
            }
        }

    }
}
