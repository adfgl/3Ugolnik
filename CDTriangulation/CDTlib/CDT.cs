using System.Drawing;

namespace CDTlib
{
    public static class CDT
    {
        public static List<Face> Triangulate<T>(IEnumerable<T> points, Func<T, double> getX, Func<T, double> getY)
        {
            List<Node> dirtyNodes = new List<Node>();
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            foreach (T point in points)
            {
                double x = getX(point);
                double y = getY(point);

                if (x < minX) minX = x;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;

                dirtyNodes.Add(new Node(-1, x, y));
            }


            List<Face> faces = new List<Face>();
            QuadTree nodes = new QuadTree(new Rectangle(minX, minY, maxX, maxY));
            AddSuperStructure(faces, nodes.Bounds);



            return faces;
        }

        public static Node Insert(List<Face> faces, QuadTree nodes, double x, double y)
        {
            Node? node = nodes.TryGet(x, y);
            if (node != null)
            {
                return node;
            }

            node = new Node(nodes.Count, x, y);


            return node;
        }

        static void AddSuperStructure(List<Face> faces, Rectangle bounds)
        {
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double size = 2 * dmax;

            Node a = new Node(-3, midx - size, midy - size);
            Node b = new Node(-2, midx + size, midy - size);
            Node c = new Node(-1, midx, midy + size);

            Face superFace = new Face(0, a, b, c).ComputeArea();
            faces.Add(superFace);
        }


      
    }
}
