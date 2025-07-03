namespace CDTISharp.Geometry
{
    public class ClosedPolygon
    {
        public ClosedPolygon(List<Node> points, List<ClosedPolygon>? holes = null)
        {
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            Points = new List<Node>(points.Count);
            foreach (Node node in points)
            {
                double x = node.X;
                double y = node.Y;

                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;

                Points.Add(node);
            }

            var first = points.First();
            var last = points.Last();
            if (!first.Equals(last))
            {
                Points.Add(first);
            }
            Bounds = new Rectangle(minX, minY, maxX, maxY);
            Holes = holes is null ? new List<ClosedPolygon>() : holes;
        }

        public List<Node> Points { get; set; }
        public Rectangle Bounds { get; set; }
        public List<ClosedPolygon> Holes { get; set; }

        public bool Contains(double x, double y, double tolernace = 0)
        {
            if (!Bounds.Contains(x, y))
            {
                return false;
            }

            if (GeometryHelper.Contains(Points, x, y, tolernace))
            {
                foreach (ClosedPolygon hole in Holes)
                {
                    if (hole.Bounds.Contains(x, y) && GeometryHelper.Contains(hole.Points, x, y, tolernace))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool Contains(ClosedPolygon other, double tolernace = 0)
        {
            if (!Bounds.Contains(other.Bounds))
            {
                return false;
            }

            foreach (var item in other.Points)
            {
                double x = item.X;
                double y = item.Y;
                if (!Bounds.Contains(x, y) || !GeometryHelper.Contains(Points, x, y, tolernace))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Intersects(ClosedPolygon other, double tolernace = 0)
        {
            if (!Bounds.Intersects(other.Bounds))
            {
                return false;
            }

            List<Node> av = Points, ab = other.Points;
            int ac = av.Count, bc = ab.Count;
            for (int i = 0; i < ac - 1; i++)
            {
                Node p1 = av[i];
                Node p2 = av[i + 1];
                for (int j = 0; j < bc - 1; j++)
                {
                    Node q1 = ab[j];
                    Node q2 = ab[j + 1];
                    if (GeometryHelper.Intersect(p1, p2, q1, q2) is not null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
