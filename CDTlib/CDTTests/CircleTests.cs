using CDTlib;

namespace CDTTests
{
    public class CircleTests
    {
        [Fact]
        public void CircleBuildsCorrectly_FromThreeSparslyPositionedPoints()
        {
            Circle circle = new Circle(79.144, 143.238, 170.708, 197.264, 199.125, 103.244);

            const double epsilon = 1e-3;
            Assert.Equal(144.018, circle.x, epsilon);
            Assert.Equal(137.893, circle.y, epsilon);
            Assert.Equal(65.094, Math.Sqrt(circle.radiusSqr), epsilon);
        }

        [Fact]
        public void CircleBuildsCorrectly_FromTwoUniquePoints()
        {
            Circle circle = new Circle(79.144, 143.238, 199.125, 103.244);

            const double epsilon = 1e-3;
            Assert.Equal(139.135, circle.x, epsilon);
            Assert.Equal(123.241, circle.y, epsilon);
            Assert.Equal(63.236, Math.Sqrt(circle.radiusSqr), epsilon);
        }

        [Fact]
        public void CircleContains_PointStrictlyInside()
        {
            Circle circle = new Circle(-50, 0, +50, 0);

            Assert.True(circle.Contains(25, 25));
        }

        [Fact]
        public void CircleContains_PointStrictlyOutside()
        {
            Circle circle = new Circle(-50, 0, +50, 0);

            Assert.False(circle.Contains(125, 25));
        }

        [Fact]
        public void CircleContains_PointOnCircumference()
        {
            Circle circle = new Circle(-50, 0, +50, 0);

            Assert.True(circle.Contains(-50, 0));
        }
    }
}
