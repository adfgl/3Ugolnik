using System.Globalization;

namespace CDTlib
{
    public readonly struct Vec2
    {
        public readonly double x, y;

        public Vec2(double x, double y)
        {
            this.x = x; this.y = y;
        }

        public override string ToString()
        {
            return $"{x.ToString(CultureInfo.InvariantCulture)}, {y.ToString(CultureInfo.InvariantCulture)}";
        }
    }
}
