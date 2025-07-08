using CDTISharp.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTISharp.Meshing
{
    public static class SvgEx
    {
        public static string ToSvg(this Mesh mesh, int size = 1000, double padding = 10, bool drawCircle = false)
        {
            // https://www.svgviewer.dev/

            List<Triangle> triangles = mesh.Triangles;
            if (triangles.Count == 0)
            {
                return "<svg xmlns='http://www.w3.org/2000/svg'/>";
            }

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            foreach (Triangle triangle in triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    Node v = mesh.Nodes[triangle.indices[i]];
                    double x = v.X;
                    double y = v.Y;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            double scale = (size - 2 * padding) / Math.Max(maxX - minX, maxY - minY);

            StringBuilder sb = new StringBuilder();
            sb.Append("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 ");
            sb.Append(size); sb.Append(' '); sb.Append(size); sb.Append("'>");

            HashSet<(int, int)> drawn = new HashSet<(int, int)>();
            foreach (Triangle triangle in triangles)
            {
                Node a = mesh.Nodes[triangle.indices[0]];
                Node b = mesh.Nodes[triangle.indices[1]];
                Node c = mesh.Nodes[triangle.indices[2]];

                var (x1, y1) = project(a.X, a.Y);
                var (x2, y2) = project(b.X, b.Y);
                var (x3, y3) = project(c.X, c.Y);

                sb.Append($"<polygon points='{x1:F1},{y1:F1} {x2:F1},{y2:F1} {x3:F1},{y3:F1}' fill='lightblue' fill-opacity='0.5'/>");

                for (int i = 0; i < 3; i++)
                {
                    triangle.Edge(i, out int startIndex, out int endIndex);
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
                    switch (triangle.constraints[i])
                    {
                        case -1:
                            edgeColor = "gray";
                            break;

                        case 0:
                            edgeColor = "blue";
                            thickness = 2;
                            break;

                        case 1:
                            edgeColor = "brown";
                            thickness = 1.5;
                            break;

                        case 2:
                            edgeColor = "green";
                            thickness = 1.5;
                            break;
                        default:
                            edgeColor = "red";
                            thickness = 3;
                            break;
                    }

                    Node start = mesh.Nodes[startIndex];
                    Node end = mesh.Nodes[endIndex];

                    var (ex1, ey1) = project(start.X, start.Y);
                    var (ex2, ey2) = project(end.X, end.Y);

                    sb.Append($"<line x1='{ex1:F1}' y1='{ey1:F1}' x2='{ex2:F1}' y2='{ey2:F1}' stroke='{edgeColor}' stroke-width='{thickness}'/>");
                }

                if (drawCircle)
                {
                    var (cx, cy) = project(triangle.circle.x, triangle.circle.y);
                    double r = Math.Sqrt(triangle.circle.radiusSqr) * scale;
                    sb.Append($"<circle cx='{cx:F1}' cy='{cy:F1}' r='{r:F1}' fill='none' stroke='blue' stroke-opacity='0.6' stroke-width='1'/>");
                }
            }

            sb.Append("</svg>");
            return sb.ToString();

            (double x, double y) project(double x, double y)
            {
                double sx = (x - minX) * scale + padding;
                double sy = (y - minY) * scale + padding;
                return (sx, size - sy); // Y-flip
            }
        }
    }
}
