using CDTISharp.Geometry;

namespace CDTISharp.Meshing
{
    public class Normalizer
    {
        readonly double _minX, _minY, _scale;

        public Normalizer(Rect bounds)
        {
            _minX = bounds.minX;
            _minY = bounds.minY;

            double width = bounds.Width();
            double height = bounds.Height();
            _scale = 1.0 / Math.Max(width, height);
        }

        public double Scale => _scale;

        public void Normalize(Node node)
        {
            node.X = (node.X - _minX) * _scale;
            node.Y = (node.Y - _minY) * _scale;
        }

        public void Denormalize(Node node)
        {
            node.X = node.X / _scale + _minX;
            node.Y = node.Y / _scale + _minY;
        }
    }
}
