﻿namespace CDTlib.Utils
{
    public readonly struct Rect
    {
        public readonly double minX, minY;
        public readonly double maxX, maxY;

        public static Rect Empty => new Rect(double.MaxValue, double.MaxValue, double.MinValue, double.MinValue);

        public Rect(double minX, double minY, double maxX, double maxY)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
        }

        public static Rect Build(double minX, double minY, double maxX, double maxY)
        {
            return new Rect(
                Math.Min(minX, maxX), Math.Min(minY, maxY),
                Math.Max(minX, maxX), Math.Max(minY, maxY)
            );
        }

        public static Rect FromPoints(IEnumerable<Vec2> points)
        {
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            foreach (Vec2 point in points)
            {
                var (x, y) = point;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
            return new Rect(minX, minY, maxX, maxY);
        }

        public void Deconstruct(out Vec2 min, out Vec2 max)
        {
            min = new Vec2(minX, minY);
            max = new Vec2(maxX, maxY);
        }

        public Rect Expand(double margin)
        {
            return new Rect(
                minX - margin,
                minY - margin,
                maxX + margin,
                maxY + margin
            );
        }

        public bool IntersectsCircle(double cx, double cy, double radius)
        {
            double closestX = Math.Max(minX, Math.Min(cx, maxX));
            double closestY = Math.Max(minY, Math.Min(cy, maxY));

            double dx = cx - closestX;
            double dy = cy - closestY;
            return dx * dx + dy * dy <= radius * radius;
        }

        public Rect Union(double x, double y)
        {
            return new Rect(
                Math.Min(minX, x), Math.Min(minY, y),
                Math.Max(maxX, x), Math.Max(maxY, y)
            );
        }

        public Rect Union(Rect other)
        {
            return new Rect(
                Math.Min(minX, other.minX), Math.Min(minY, other.minY),
                Math.Max(maxX, other.maxX), Math.Max(maxY, other.maxY)
            );
        }

        public bool Intersection(Rect other, out Rect intersection)
        {
            double minX = Math.Max(this.minX, other.minX);
            double minY = Math.Max(this.minY, other.minY);
            double maxX = Math.Min(this.maxX, other.maxX);
            double maxY = Math.Min(this.maxY, other.maxY);
            if (minX <= maxX && minY <= maxY)
            {
                intersection = new Rect(minX, minY, maxX, maxY);
                return true;
            }

            intersection = Empty;
            return false;
        }

        public Rect Move(double dx, double dy) => new Rect(minX + dx, minY + dy, maxX + dx, maxY + dy);
        public Vec2 Center() => new Vec2((minX + maxX) / 2, (minY + maxY) / 2);

        public bool Contains(double x, double y) => x >= minX && x <= maxX && y >= minY && y <= maxY;
        public bool ContainsStrict(double x, double y) => x > minX && x < maxX && y > minY && y < maxY;

        public bool Contains(Rect other) =>
            minX <= other.minX && minY <= other.minY &&
            maxX >= other.maxX && maxY >= other.maxY;

        public bool ContainsStrict(Rect other) =>
            minX < other.minX && minY < other.minY &&
            maxX > other.maxX && maxY > other.maxY;

        public bool Intersects(Rect other) =>
            minX <= other.maxX && minY <= other.maxY &&
            maxX >= other.minX && maxY >= other.minY;

        public bool IntersectsStrict(Rect other) =>
            minX < other.maxX && minY < other.maxY &&
            maxX > other.minX && maxY > other.minY;
    }
}
