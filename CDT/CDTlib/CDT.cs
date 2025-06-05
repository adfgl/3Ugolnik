using CDTlib.DataStructures;
using CDTlib.Utils;
using System.Runtime.CompilerServices;

namespace CDTlib
{
    public class CDT
    {
        public CDT Triangulate(IList<Vec2> points)
        {
            (Rect bounds, List<Vec2> uniquePoints) = Preporcess(points);

            (List<Triangle> triangles, List<Node> nodes) = AddSuperStructure(bounds, uniquePoints, ESuperStructure.Circle);
            foreach (Vec2 point in uniquePoints)
            {

            }
            return this;
        }

        Triangle? FindContaining(List<Triangle> triangles, double x, double y)
        {
            int current = triangles.Count - 1;
            while (true)
            {
                bool inside = true;
                foreach (Edge edge in triangles[current])
                {
                    Node a = edge.Origin;
                    Node b = edge.Next.Origin;


                }
            }

            return null;
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
            List<Node> nodes = new List<Node>(points.Count);

            int startIndex = -1;

            Vec2 a = points[0];
            Node nodeA = new Node(startIndex, a.x, a.y);
            nodes.Add(nodeA);

            Edge? prevShared = null;
            for (int i = 1; i < points.Count - 1; i++)
            {
                Vec2 b = points[i];
                Vec2 c = points[i + 1];
                double area = GeometryHelper.Area(a.x, a.y, b.x, b.y, c.x, c.y);    

                Node nodeB = new Node(startIndex--, b.x, b.y);
                Node nodeC = new Node(startIndex--, c.x, c.y);

                Edge ab = new Edge(nodeA);
                Edge bc = new Edge(nodeB);
                Edge ca = new Edge(nodeC);

                ab.Next = bc;
                bc.Next = ca;
                ca.Next = ab;

                Triangle tri = new Triangle(ab) { Area = area };
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
                nodes.Add(nodeB);
                nodes.Add(nodeC);
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
