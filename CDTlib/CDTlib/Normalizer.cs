namespace CDTlib
{
    public class Normalizer
    {
        readonly double minX, minY;
        readonly double _scale;

        public Normalizer(Rectangle rect)
        {
            minX = rect.minX;
            minY = rect.minY;

            double dx = rect.maxX - rect.minX;
            double dy = rect.maxY - rect.minY;
            _scale = 1.0 / Math.Max(dx, dy);
        }

        public double Scale => _scale;

        public void Normalize(Node node)
        {
            node.X = (node.X - minX) * _scale;
            node.Y = (node.Y - minY) * _scale;
        }

        public Node Denormalize(Node node)
        {
            double x = node.X / _scale + minX;
            double y = node.Y / _scale + minY;
            return new Node(node.Index, x, y, node.Z);
        }

        public (double x, double y) Normalize(double x, double y)
        {
            return ((x - minX) * _scale, (y - minY) * _scale);
        }

        public (double x, double y) Denormalize(double x, double y)
        {
            return (x / _scale + minX, y / _scale + minY);
        }
    }

}
