using CDTISharp.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTISharp.Meshing
{
    public static class Navigation
    {
        public static void FindContaining(List<Triangle> triangles, List<Node> nodes, Node pt, out int triangle, out int edge, out int node, double eps, int searchStart = -1)
        {
            triangle = edge = node = -1;
            if (triangles.Count == 0)
            {
                return;
            }

            double x = pt.X;
            double y = pt.Y;

            int maxSteps = _triangles.Count * 3;
            int trianglesChecked = 0;

            int skipEdge = -1;
            int current = searchStart == -1 ? triangles.Count - 1 : searchStart;
            while (true)
            {
                if (trianglesChecked++ > maxSteps)
                {
                    throw new Exception("FindContaining exceeded max steps. Likely invalid topology.");
                }

                Triangle t = triangles[current];

                int bestExit = -1;
                double worstCross = 0;
                bool inside = true;
                for (int i = 0; i < 3; i++)
                {
                    if (i == skipEdge)
                    {
                        continue;
                    }

                    Node start = nodes[t.indices[i]];
                    if (GeometryHelper.CloseOrEqual(start, pt, eps))
                    {
                        triangle = current;
                        edge = i;
                        node = start.Index;
                        return;
                    }

                    Node end = nodes[t.indices[Mesh.NEXT[i]]];
                    if (GeometryHelper.CloseOrEqual(start, pt, eps))
                    {
                        triangle = current;
                        edge = Mesh.NEXT[i];
                        node = end.Index;
                        return;
                    }

                    double cross = GeometryHelper.Cross(start, end, x, y);
                    if (Math.Abs(cross) < eps)
                    {
                        double dx = end.X - start.X;
                        double dy = end.Y - start.Y;
                        double dot = (x - start.X) * dx + (y - start.Y) * dy;
                        double lenSq = dx * dx + dy * dy;

                        if (dot >= -eps && dot <= lenSq + eps)
                        {
                            triangle = current;
                            edge = i;
                            node = -1;
                            return;
                        }
                    }

                    if (cross < 0)
                    {
                        inside = false;
                        if (bestExit == -1 || cross < worstCross)
                        {
                            worstCross = cross;
                            bestExit = i;
                        }
                    }
                }

                if (inside)
                {
                    triangle = current;
                    edge = node = -1;
                    return;
                }

                int next = t.adjacent[bestExit];
                if (next == -1)
                {
                    triangle = edge = node = -1;
                    return;
                }

                int bestStart = t.indices[bestExit];
                int bestEnd = t.indices[Mesh.NEXT[bestExit]];

                skipEdge = triangles[next].IndexOf(bestEnd, bestStart);
                current = next;
            }
        }

        public static void FindEdgeBrute(List<Triangle> triangles, Node a, Node b, out int triangle, out int edge)
        {
            foreach (Triangle t in triangles)
            {
                int e = t.IndexOf(a.Index, b.Index);
                if (e != -1)
                {
                    triangle = t.index;
                    edge = e;
                    return;
                }
            }

            triangle = -1;
            edge = -1;
        }

        public static void FindEdge(List<Triangle> triangles, Node a, Node b, out int triangle, out int edge)
        {
            TriangleWalker walker = new TriangleWalker(triangles, a.Triangle, a.Index);
            do
            {
                Triangle t = triangles[walker.Current];
                triangle = t.index;

                int e0 = walker.Edge0;
                if (t.indices[e0] == a.Index && t.indices[Mesh.NEXT[e0]] == b.Index)
                {
                    edge = e0;
                    return;
                }

                int e1 = walker.Edge1;
                if (t.indices[e1] == a.Index && t.indices[Mesh.NEXT[e1]] == b.Index)
                {
                    edge = e1;
                    return;
                }
            }
            while (walker.MoveNext());

            triangle = edge = -1;
        }
    }
}
