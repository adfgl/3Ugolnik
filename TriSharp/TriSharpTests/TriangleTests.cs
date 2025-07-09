using TriSharp;

namespace TriSharpTests
{
    using static Int3;

    public class TriangleTests
    {
        [Fact]
        public void DefaultTriangleIsBuiltCorrectly()
        {
            int index = 0;
            int a = 1, b = 2, c = 3;

            Triangle t = new Triangle(0, 1, 2, 3);

            Assert.Equal(index, t.index);

            Assert.Equal(a, t.indices.a);
            Assert.Equal(b, t.indices.b);
            Assert.Equal(c, t.indices.c);

            Assert.Equal(NO_INDEX, t.adjacent.a);
            Assert.Equal(NO_INDEX, t.adjacent.b);
            Assert.Equal(NO_INDEX, t.adjacent.c);

            Assert.Equal(NO_INDEX, t.constraints.a);
            Assert.Equal(NO_INDEX, t.constraints.b);
            Assert.Equal(NO_INDEX, t.constraints.c);
        }
    }
}
