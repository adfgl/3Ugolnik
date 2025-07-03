using CDTISharp.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTISharp.Meshing
{
    public class SearchResult
    {
        public int Triangle { get; set; } = -1;
        public int Edge { get; set; } = -1;
        public int Node { get; set; } = -1;
    }

    public static class Navigation
    {
        public static SearchResult? FindContaining(List<Triangle> triangles, List<Node> nodes, Node pt, List<int> path, double eps, int searchStart = -1)
        {
            if (triangles.Count == 0)
            {
                return null;
            }

            double x = pt.X;
            double y = pt.Y;

            int maxSteps = triangles.Count * 3;
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
                path.Add(current);

                int bestExit = -1;
                double worstCross = double.MaxValue;
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
                        return new SearchResult()
                        {
                            Edge = i,
                            Triangle = current,
                            Node = start.Index,
                        };
                    }

                    Node end = nodes[t.indices[Mesh.NEXT[i]]];
                    if (GeometryHelper.CloseOrEqual(end, pt, eps))
                    {
                        return new SearchResult()
                        {
                            Edge = Mesh.NEXT[i],
                            Triangle = current,
                            Node = end.Index,
                        };
                    }

                    double cross = GeometryHelper.Cross(start, end, x, y);
                    if (Math.Abs(cross) <= eps)
                    {
                        if (x >= Math.Min(start.X, end.X) - eps &&
                            x <= Math.Max(start.X, end.X) + eps &&
                            y >= Math.Min(start.Y, end.Y) - eps &&
                            y <= Math.Max(start.Y, end.Y) + eps)
                        {
                            return new SearchResult()
                            {
                                Edge = i,
                                Triangle = current,
                            };
                        }
                    }

                    Debug.WriteLine(cross);

                    if (cross <= 0)
                    {
                        inside = false;
                        if (bestExit == -1 || cross < worstCross)
                        {
                            worstCross = cross;
                            bestExit = i;
                        }
                    }
                }
                Debug.WriteLine("");
                if (inside)
                {
                    return new SearchResult()
                    {
                        Triangle = current
                    };
                }

                int next = t.adjacent[bestExit];
                if (next == -1)
                {
                    return null;
                }

                int bestStart = t.indices[bestExit];
                int bestEnd = t.indices[Mesh.NEXT[bestExit]];

                skipEdge = triangles[next].IndexOf(bestEnd, bestStart);
                current = next;
            }
        }

        public static SearchResult? FindEdgeBrute(List<Triangle> triangles, Node a, Node b)
        {
            foreach (Triangle t in triangles)
            {
                int e = t.IndexOf(a.Index, b.Index);
                if (e != -1)
                {
                    return new SearchResult()
                    {
                        Triangle = t.index,
                        Edge = e,
                    };
                }
            }
            return null;
        }

        public static SearchResult? FindEdge(List<Triangle> triangles, Node a, Node b)
        {
            TriangleWalker walker = new TriangleWalker(triangles, a.Triangle, a.Index);
            do
            {
                Triangle t = triangles[walker.Current];

                int e0 = walker.Edge0;
                if (t.indices[e0] == a.Index && t.indices[Mesh.NEXT[e0]] == b.Index)
                {
                    return new SearchResult()
                    {
                        Triangle = t.index,
                        Edge = e0,
                    };
                }

                int e1 = walker.Edge1;
                if (t.indices[e1] == a.Index && t.indices[Mesh.NEXT[e1]] == b.Index)
                {
                    return new SearchResult()
                    {
                        Triangle = t.index,
                        Edge = e1,
                    };
                }
            }
            while (walker.MoveNext());

            return null;
        }
    }
}
