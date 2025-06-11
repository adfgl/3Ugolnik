using CDTlib.DataStructures;
using CDTlib.Utils;
using System.Numerics;

namespace CDTTests
{
    public class QuadTreeTests
    {
        static QuadTree TestCase()
        {
            Vec2[] vertices = 
            [
                new Vec2(-100, -100), 
                new Vec2(-100, +100),
                new Vec2(+100, +100), 
                new Vec2(+100, -100), 
            ];

            Rect rect = Rect.FromPoints(vertices);
            QuadTree quadTree = new QuadTree(rect);
            for (int i = 0; i < vertices.Length; i++)
            {
                var (x, y) = vertices[i];
                quadTree.Add(new Node(i, x, y));
            }
            return quadTree;
        }

        [Fact]
        public void QuerryAllWorksCorrectly()
        {
            QuadTree actual = TestCase();
            List<Node> result = actual.Query(actual.Bounds);
            Assert.Equal(actual.Items.Count, result.Count);
        }
    }
}
