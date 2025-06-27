using CDTSharp.Geometry;
using System.Text;

namespace CDTSharp.Meshing
{
    public static class MeshEx
    {
        public static string ToSvg(this Mesh mesh, int size = 1000, float padding = 10)
        {
            List<Triangle> triangles = mesh.Triangles;
            if (triangles.Count == 0)
            {
                return "<svg xmlns='http://www.w3.org/2000/svg'/>";
            }

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            foreach (Triangle triangle in triangles)
            {
                triangle.Nodes(out Node a, out Node b, out Node c);
                process(a);
                process(b);
                process(c);
            }

            double scale = (size - 2 * padding) / Math.Max(maxX - minX, maxY - minY);

            StringBuilder sb = new StringBuilder();
            sb.Append("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 ");
            sb.Append(size); sb.Append(' '); sb.Append(size); sb.Append("'>");

            HashSet<(int, int)> drawn = new HashSet<(int, int)>();
            foreach (Triangle triangle in triangles)
            {
                triangle.Nodes(out Node a, out Node b, out Node c);

                var (x1, y1) = project(a.X, a.Y);
                var (x2, y2) = project(b.X, b.Y);
                var (x3, y3) = project(c.X, c.Y);

                sb.Append($"<polygon points='{x1:F1},{y1:F1} {x2:F1},{y2:F1} {x3:F1},{y3:F1}' fill='lightgray' fill-opacity='0.5'/>");

                foreach (Edge edge in triangle.Forward())
                {
                    var (start, end) = edge;
                    int startIndex = start.Index;
                    int endIndex = end.Index;
                    if (startIndex > endIndex)
                    {
                        int t = startIndex;
                        startIndex = endIndex;
                        endIndex = t;
                    }

                    if (!drawn.Add((startIndex, endIndex)))
                    {
                        continue;
                    }

                    string edgeColor;
                    double thickness = 1;
                    switch (edge.Constrained)
                    {
                        case EConstraint.None:
                            edgeColor = "black";       
                            break;
                        case EConstraint.User:
                            edgeColor = "red";
                            thickness = 1.5;
                            break;
                        case EConstraint.Contour:
                            edgeColor = "orange";
                            thickness = 2;
                            break;
                        case EConstraint.Hole:
                            edgeColor = "green";
                            thickness = 1.5;
                            break;
                        default:
                            edgeColor = "gray";        
                            break;
                    }

                    var (ex1, ey1) = project(start.X, start.Y);
                    var (ex2, ey2) = project(end.X, end.Y);

                    sb.Append($"<line x1='{ex1:F1}' y1='{ey1:F1}' x2='{ex2:F1}' y2='{ey2:F1}' stroke='{edgeColor}' stroke-width='{thickness}'/>");
                }
            }

            sb.Append("</svg>");
            return sb.ToString();

            void process(Node v)
            {
                double x = v.X;
                double y = v.Y;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            (double x, double y) project(double x, double y)
            {
                double sx = (x - minX) * scale + padding;
                double sy = (y - minY) * scale + padding;
                return (sx, size - sy); // Y-flip
            }
        }
    }
}
