namespace CDTlib
{
    public class Polygon
    {
        public Polygon(List<CDTPoint> points)
        {
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;

            Nodes = new List<Node>(points.Count);

            int count = 0;
            foreach (CDTPoint point in points)
            {
                double x = point.X;
                double y = point.Y;

                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;

                Nodes.Add(new Node(count++, x, y, 0));
            }

            var first = Nodes.First();
            var last = Nodes.Last();
            if (first.X == last.X && first.Y == last.Y)
            {
                Nodes.RemoveAt(Nodes.Count - 1);
            }

            Rect = new Rectangle(minX, minY, maxX, maxY);
        }

        public Rectangle Rect { get; set; }
        public List<Node> Nodes { get; set; }

        public bool Contains(double x, double y, double tolerance = 0)
        {
            if (!Rect.Contains(x, y))
            {
                return false;
            }

            int count = Nodes.Count;
            bool inside = false;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                Node a = Nodes[i];
                Node b = Nodes[j];

                double xi = a.X;
                double yi = a.Y;

                double xj = b.X;
                double yj = b.Y;

                bool crosses = (yi > y + tolerance) != (yj > y + tolerance);
                if (!crosses) continue;

                double t = (y - yi) / (yj - yi);
                double xCross = xi + t * (xj - xi);
                if (x < xCross - tolerance)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        public bool Contains(Polygon other)
        {
            if (!Rect.Contains(other.Rect))
            {
                return false;
            }

            foreach (var item in other.Nodes)
            {
                if (!Contains(item.X, item.Y))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Intersects(Polygon other)
        {
            if (!Rect.Intersects(other.Rect))
            {
                return false;
            }
        }
    }
}
