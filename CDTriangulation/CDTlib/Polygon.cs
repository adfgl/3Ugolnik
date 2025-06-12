namespace CDTlib
{
    public class Polygon
    {
        public Polygon(int index, List<CDTPoint> points)
        {
            Index = index;
            Rect = Rectangle.FromPoints(points, o => o.X, o => o.Y);
            Nodes = points.ToList();
        }

        public int Index { get; set; }
        public Rectangle Rect { get; set; }
        public List<CDTPoint> Nodes { get; set; }

        public bool Contains(double x, double y, double eps = 0)
        {
            if (!Rect.Contains(x, y)) return false;

            List<CDTPoint> verts = Nodes;
            int count = verts.Count;
            bool inside = false;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                var (xi, yi) = verts[i];
                var (xj, yj) = verts[j];

                bool crosses = (yi > y + eps) != (yj > y + eps);
                if (!crosses) continue;

                double t = (y - yi) / (yj - yi);
                double xCross = xi + t * (xj - xi);
                if (x < xCross - eps)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        //public static bool Intersects(Polygon a, Polygon b, double eps)
        //{
        //    if (!a.Rect.Intersects(b.Rect)) return false;

        //    List<CDTPoint> av = a.Nodes, ab = b.Nodes;
        //    int ac = av.Count, bc = ab.Count;
        //    for (int i = 0; i < ac; i++)
        //    {
        //        var p1 = av[i];
        //        var p2 = av[(i + 1) % ac];
        //        for (int j = 0; j < bc; j++)
        //        {
        //            var q1 = ab[j];
        //            var q2 = ab[(j + 1) % bc];
        //            if (GeometryHelper.Intersect(p1, p2, q1, q2, out _))
        //            {
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}
    }
}
