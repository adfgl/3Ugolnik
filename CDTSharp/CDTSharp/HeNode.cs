using CDTGeometryLib;

namespace CDTSharp
{
    public class HeNode : Node
    {
        public HeNode(int index, double x, double y) : base(index, x, y)
        {

        }

        public void Deconstruct(out double x, out double y)
        {
            x = X; y = Y;
        }

        public HeEdge Edge { get; set; } = null!;

        public bool Close(double x, double y, double eps = 1e-6)
        {
            double dx = x - X;
            double dy = y - Y;
            return dx * dx + dy * dy < eps;
        }

        public IEnumerable<HeEdge> Around()
        {
            HeEdge start = Edge;
            HeEdge current = Edge;
            do
            {
                yield return current;
                current = current.Prev.Twin!;
            } while (current != null && current != start);
        }
    }
}
