using CDTlib.DataStructures;
using CDTlib.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CDTlib
{
    public class CDT
    {
        public CDT Triangulate(IList<Vec2> points)
        {
            (Rect bounds, List<Vec2> uniquePoints) = Preporcess(points);

            (List<Triangle> triangles, List<Node> nodes) = AddSuperStructure(bounds, uniquePoints, ESuperStructure.Circle);
            int superIndex = nodes.Count;

            foreach (Vec2 point in uniquePoints)
            {
                var (x, y) = point;

                (Triangle? triangle, Edge? edge, Node? node) = FindContaining(triangles, x, y);
                if (node != null)
                {
                    continue;
                }

                if (triangle == null)
                {
                    throw new Exception($"Point [{point}] is outside of topology.");
                }

                Node newNode = new Node(nodes.Count, x, y);
                nodes.Add(newNode);

                if (edge == null)
                {

                }
                else
                {
                    SplitTriangle(triangles, triangle, newNode);
                }
            }
            return this;
        }

        public static void SplitEdge(List<Triangle> triangles, Edge edge, Node node)
        {

        }

        public static void SplitTriangle(List<Triangle> triangles, Triangle triangle, Node node)
        {
            Edge? prevEdge = null;
            Edge firstEdge = null!;

            double remainingArea = triangle.Area;
            int baseIndex = triangles.Count;
            Edge[] edges = [triangle.Edge, triangle.Edge.Next, triangle.Edge.Next.Next];
            for (int i = 0; i < 3; i++)
            {
                int triIndex = i == 0 ? triangle.Index : baseIndex + i;

                Edge e = edges[i];

                Node a = e.Origin;
                Node b = e.Next.Origin;
                Node c = node;

                Edge ab = new Edge(a); a.Edge = ab;
                Edge bc = new Edge(b); b.Edge = bc;
                Edge ca = new Edge(c); c.Edge = ca;

                double area;
                if (i < 2)
                {
                    area = GeometryHelper.Area(a.X, a.Y, b.X, b.Y, c.X, c.Y);
                    remainingArea -= area;
                }
                else
                {
                    area = remainingArea;
                }

                Triangle tri = new Triangle(triIndex, ab)
                {
                    Area = area
                };

                ab.Next = bc;
                bc.Next = ca;
                ca.Next = ab;

                ab.Triangle = tri;
                bc.Triangle = tri;
                ca.Triangle = tri;

                Edge? twin = e.Twin;
                if (twin != null)
                {
                    ab.Twin = twin;
                    twin.Twin = ab;
                }

                if (prevEdge == null)
                {
                    firstEdge = bc;
                }
                else
                {
                    prevEdge.Twin = ca;
                    ca.Twin = prevEdge;
                }
                prevEdge = bc;

                if (triIndex < triangles.Count)
                {
                    triangles[triIndex] = tri;
                }
                else
                {
                    triangles.Add(tri);
                }
            }

            prevEdge!.Twin = firstEdge;
            firstEdge!.Twin = prevEdge;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CloseEnoughTo(Node node, double x, double y, double eps)
        {
            double dx = node.X - x;
            double dy = node.Y - y;
            return dx * dx + dy * dy <= eps;
        }

        (Triangle? t, Edge? e, Node? n) FindContaining(List<Triangle> triangles, double x, double y, double eps = 1e-6)
        {
            int max = triangles.Count * 3;
            int steps = 0;

            Triangle current = triangles[^1];
            while (true)
            {
                if (steps++ > max)
                {
                    throw new Exception("Could not find containing triangle. Most likely mesh topology is invalid.");
                }

                bool inside = true;
                foreach (Edge edge in current)
                {
                    Node a = edge.Origin;
                    if (CloseEnoughTo(a, x, y, eps))
                    {
                        return (current, edge, a);
                    }

                    Node b = edge.Next.Origin;
                    if (CloseEnoughTo(b, x, y, eps))
                    {
                        return (current, edge.Next, b);
                    }

                    double dx = b.X - a.X;
                    double dy = b.Y - a.Y;
                    double dot = (x - a.X) * dx + (y - a.Y) * dy;
                    if (dot >= -eps && dot <= dx * dx + dy * dy + eps)
                    {
                        return (current, edge, null);
                    }

                    EOrientation orientation = GeometryHelper.Orientation(x, y, a.X, a.Y, b.X, b.Y, eps);
                    if (orientation != EOrientation.Left)
                    {
                        Edge? twin = edge.Twin;
                        if (twin is null)
                        {
                            return (null, null, null);
                        }

                        inside = false;
                        current = twin.Triangle;
                        break;
                    }
                }

                if (inside)
                {
                    return (current, null, null);
                }
            }
        }

        (Rect bounds, List<Vec2> uniquePoints) Preporcess(IList<Vec2> points)
        {
            List<Vec2> uniquePoints = new List<Vec2>(points.Count);
            Rect rect = Rect.FromPoints(points);
            QuadTree qt = new QuadTree(rect);

            foreach (Vec2 point in points)
            {
                var (x, y) = point;
                if (qt.TryGet(x, y, 1e-6) == null)
                {
                    qt.Insert(new Node(-1, x, y));
                    uniquePoints.Add(point);    
                }
            }

            int count = uniquePoints.Count;
            if (count < 3)
            {
                throw new Exception($"Must have at least 3 points but got only {count} unique points.");
            }
            return (rect, uniquePoints);
        }

        (List<Triangle> triangles, List<Node> nodes) AddSuperStructure(Rect bounds, List<Vec2> uniquePoints, ESuperStructure superStructure)
        {
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double scale = 2;

            double size = scale * dmax;

            List<Vec2> points = new List<Vec2>();
            switch (superStructure)
            {
                case ESuperStructure.Triangle:
                    points.Add(new Vec2(midx - size, midy - size));
                    points.Add(new Vec2(midx, midy + size));
                    points.Add(new Vec2(midx + size, midy - size));
                    break;

                case ESuperStructure.Square:
                    points.Add(new Vec2(midx - size, midy - size)); 
                    points.Add(new Vec2(midx + size, midy - size)); 
                    points.Add(new Vec2(midx + size, midy + size)); 
                    points.Add(new Vec2(midx - size, midy + size)); 
                    break;

                case ESuperStructure.Circle:
                    int n = (int)Math.Max(4, Math.Sqrt(uniquePoints.Count));
                    for (int i = 0; i < n; i++)
                    {
                        double angle = 2 * Math.PI * i / n;
                        double x = Math.Cos(angle) * size;
                        double y = Math.Sin(angle) * size;
                        points.Add(new Vec2(x, y));
                    }
                    break;

                default:
                    throw new NotImplementedException($"Super-structure '{superStructure}' is not implemented.");
            }

            List<Triangle> triangles = new List<Triangle>();
            List<Node> nodes = new List<Node>();

            Vec2 a = points[0];
            Node nodeA = new Node(nodes.Count, a.x, a.y);
            nodes.Add(nodeA);

            Edge? prevShared = null;
            for (int i = 1; i < points.Count - 1; i++)
            {
                Vec2 b = points[i];
                Vec2 c = points[i + 1];
                double area = GeometryHelper.Area(a.x, a.y, b.x, b.y, c.x, c.y);    

                Node nodeB = new Node(nodes.Count, b.x, b.y);
                nodes.Add(nodeB);

                Node nodeC = new Node(nodes.Count, c.x, c.y);
                nodes.Add(nodeC);

                Edge ab = new Edge(nodeA);
                Edge bc = new Edge(nodeB);
                Edge ca = new Edge(nodeC);

                ab.Next = bc;
                bc.Next = ca;
                ca.Next = ab;

                Triangle tri = new Triangle(triangles.Count, ab) { Area = area };
                ab.Triangle = tri;
                bc.Triangle = tri;
                ca.Triangle = tri;

                nodeA.Edge = ab;
                nodeB.Edge = bc;
                nodeC.Edge = ca;

                if (prevShared != null)
                {
                    prevShared.Twin = ca;
                    ca.Twin = prevShared;
                }

                prevShared = bc;

                triangles.Add(tri);
            }
            return (triangles, nodes);
        }

     
        public enum ESuperStructure
        {
            Triangle,
            Square,
            Circle
        }
    }
}
