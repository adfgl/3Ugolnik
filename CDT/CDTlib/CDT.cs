using CDTlib.DataStructures;
using CDTlib.Utils;
using System.Runtime.CompilerServices;

namespace CDTlib
{
    public static class CDT
    {
        public const double EPS = 1e-6;

        public static List<Triangle> Triangulate(IList<Vec2> points)
        {
            (Rect bounds, List<Vec2> uniquePoints) = Preporcess(points);

            (List<Triangle> triangles, List<Node> nodes) = AddSuperStructure(bounds, uniquePoints, ESuperStructure.Circle);
            int superIndex = nodes.Count;

            foreach (Vec2 point in uniquePoints)
            {
                var (x, y) = point;
                Insert(triangles, nodes, x, y); 
            }
            return triangles;
        }

        public static void Insert(List<Triangle> triangles, List<Node> nodes, double x, double y)
        {
            (Triangle? triangle, Edge? edge, Node? node) = FindContaining(triangles, x, y);
            if (node != null)
            {
                return;
            }

            if (triangle == null)
            {
                throw new Exception($"Point [{x} {y}] is outside of topology.");
            }

            Node newNode = new Node(nodes.Count, x, y);
            nodes.Add(newNode);

            Triangle[] tris;
            Edge[] affected;
            int baseIndex = triangles.Count;
            if (edge == null)
            {
                tris = SplitTriangle(baseIndex, triangle, newNode, out affected);
            }
            else
            {
                tris = SplitEdge(baseIndex, edge, newNode, out affected);
            }

            AddNewTriangles(triangles, tris);
            Legalize(triangles, affected);
        }

        public static void Legalize(List<Triangle> triangles, Edge[] affected)
        {
            Stack<Edge> toLegalize = new Stack<Edge>(affected);
            while (toLegalize.Count > 0)
            {
                Edge edge = toLegalize.Pop();
                if (ShouldFlip(edge))
                {
                    Triangle[] tris = FlipEdge(edge, out Edge[] newAffected);
                    AddNewTriangles(triangles, tris);

                    foreach (Edge item in newAffected)
                    {
                        toLegalize.Push(item);
                    }
                }
            }
        }

        public static bool ShouldFlip(Edge edge)
        {
            Edge? twin = edge.Twin;
            if (twin is null) return false;

            Node v0 = edge.Origin;
            Node v1 = edge.Next.Origin;
            Node v2 = edge.Prev.Origin;
            Node v3 = twin.Next.Origin;

            if (DelaunayCriteria.SumOfOppositeAngles(v3.X, v3.Y, v0.X, v0.Y, v1.X, v1.Y, v2.X, v2.Y))
            {
                return true;
            }
            return false;
        }

        public static Triangle[] FlipEdge(Edge edge, out Edge[] affected)
        {
            /*
              v2 - is inserted point, we want to propagate flip away from it, otherwise we 
              are risking ending up in flipping degeneracy
                           v2                        v2
                           /\                        /|\
                          /  \                      / | \
                         /    \                    /  |  \
                        /      \                  /   |   \ 
                       /   t0   \                /    |    \
                      /          \              /     |     \ 
                     /            \            /      |      \
                 v0 +--------------+ v1    v0 +   t1  |  t0   + v1
                     \            /            \      |      /
                      \          /              \     |     /
                       \   t1   /                \    |    /
                        \      /                  \   |   / 
                         \    /                    \  |  /
                          \  /                      \ | /
                           \/                        \|/
                           v3                        v3
             */

            Edge? twin = edge.Twin;
            if (twin is null)
            {
                throw new Exception();
            }

            Node v0 = edge.Origin;
            Node v1 = edge.Next.Origin;
            Node v2 = edge.Prev.Origin;
            Node v3 = twin.Next.Origin;

            Triangle t0 = edge.Triangle;
            Triangle t1 = twin.Triangle;

            Triangle new0 = BuildTriangle(v2, v3, v1, t0.Index);
            new0.Area = Area(new0);

            Triangle new1 = BuildTriangle(v3, v2, v0, t1.Index);
            new1.Area = t0.Area + t1.Area - new0.Area;

            SetTwins(new0.Edge, new1.Edge);

            SetTwins(new0.Edge.Next, t1.Edge.Next.Twin);
            SetTwins(new0.Edge.Prev, t0.Edge.Next.Twin);

            SetTwins(new1.Edge.Next, t1.Edge.Prev.Twin);
            SetTwins(new1.Edge.Prev, t0.Edge.Prev.Twin);

            affected = [new0.Edge.Next, new1.Edge.Prev];
            return [new0, new1];
        }


        public static Triangle[] SplitEdge(int baseIndex, Edge edge, Node node, out Edge[] affected)
        {
            /*
                        v2                          v2            
                        /\                          /|\             
                       /  \                        / | \           
                      /    \                      /  |  \          
                     /      \                    /   |   \      
                    /   t0   \                  /    |    \        
                   /          \                / t0  |  t1 \       
                  /            \              /      |      \      
              v0 +--------------+ v1      v0 +-------v4------+ v1  
                  \            /              \      |      /      
                   \          /                \ t3  |  t2 /       
                    \   t1   /                  \    |    /        
                     \      /                    \   |   /      
                      \    /                      \  |  /          
                       \  /                        \ | /           
                        \/                          \|/            
                        v3                          v3            
            */

            Edge? twin = edge.Twin;
            if (twin == null)
            {
                throw new Exception();
            }

            Triangle t0 = edge.Triangle;
            Triangle t1 = twin.Triangle;

            Triangle new0 = BuildTriangle(edge.Prev, node, t0.Index);
            Triangle new1 = BuildTriangle(edge.Next, node, baseIndex);
            Triangle new2 = BuildTriangle(twin.Prev, node, baseIndex + 1);
            Triangle new3 = BuildTriangle(twin.Next, node, t1.Index);

            new0.Area = Area(new0);
            new1.Area = t0.Area - new0.Area;
            new2.Area = Area(new0);
            new3.Area = t0.Area - new2.Area;

            bool constrained = edge.Constrained || twin.Constrained;
            new0.Edge.Next.Constrained = constrained;
            new1.Edge.Prev.Constrained = constrained;
            new2.Edge.Next.Constrained = constrained;
            new3.Edge.Prev.Constrained = constrained;

            Triangle[] tris = [new0, new1, new2, new3];
            SetTwins(tris);

            affected = [new0.Edge, new1.Edge, new2.Edge, new3.Edge];
            return tris;
        }

        public static Triangle[] SplitTriangle(int baseIndex, Triangle triangle, Node node, out Edge[] affected)
        {
            Triangle new0 = BuildTriangle(triangle.Edge, node, triangle.Index);
            Triangle new1 = BuildTriangle(triangle.Edge.Next, node, baseIndex);
            Triangle new2 = BuildTriangle(triangle.Edge.Prev, node, baseIndex + 1);
            
            new0.Area = Area(new0);
            new1.Area = Area(new1);
            new2.Area = triangle.Area - new0.Area - new1.Area;

            Triangle[] tris = [new0, new1, new2];
            SetTwins(tris);

            affected = [new0.Edge, new1.Edge, new2.Edge];   
            return tris;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Area(Triangle t)
        {
            Node a = t.Edge.Origin;
            Node b = t.Edge.Next.Origin;
            Node c = t.Edge.Prev.Origin;
            return GeometryHelper.Area(a.X, a.Y, b.X, b.Y, c.X, c.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTwins(Edge a, Edge? b)
        {
            a.Twin = b;
            if (b != null)
            {
                b.Twin = a;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SetTwins(Triangle[] tris)
        {
            int n = tris.Length;
            for (int i = 0; i < n; i++)
            {
                Triangle curr = tris[i];
                Triangle next = tris[(i + 1) % n];
                SetTwins(curr.Edge.Next, next.Edge.Prev);
            }
        }

        static void AddNewTriangles(List<Triangle> triangles, Triangle[] tris)
        {
            for (int i = 0; i < tris.Length; i++)
            {
                Triangle t = tris[i];

                Edge? twin = t.Edge.Twin;
                if (twin is not null)
                {
                    twin.Twin = t.Edge;
                }

                int index = t.Index;
                if (index < triangles.Count)
                {
                    triangles[index] = t;
                }
                else
                {
                    triangles.Add(t);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Triangle BuildTriangle(Node a, Node b, Node c, int index)
        {
            Edge ab = new Edge(a);
            Edge bc = new Edge(b);
            Edge ca = new Edge(c);

            Triangle tri = new Triangle(index, ab);

            a.Edge = ab;
            b.Edge = bc;
            c.Edge = ca;

            ab.Next = bc;
            bc.Next = ca;
            ca.Next = ab;

            ab.Prev = ca;
            bc.Prev = ab;
            ca.Prev = bc;

            ab.Triangle = tri;
            bc.Triangle = tri;
            ca.Triangle = tri;

            return tri;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Triangle BuildTriangle(Edge edge, Node node, int index)
        {
            Triangle tri = BuildTriangle(edge.Origin, edge.Next.Origin, node, index);
            tri.Edge.Constrained = edge.Constrained;
            tri.Edge.Twin = edge.Twin;
            return tri;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CloseEnoughTo(Node node, double x, double y, double eps)
        {
            double dx = node.X - x;
            double dy = node.Y - y;
            return dx * dx + dy * dy <= eps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool OnEdge(Node a, Node b, double x, double y, double eps)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double dot = (x - a.X) * dx + (y - a.Y) * dy;
            return dot >= -eps && dot <= dx * dx + dy * dy + eps;
        }

        public static (Triangle? t, Edge? e, Node? n) FindContaining(List<Triangle> triangles, double x, double y, double eps = 1e-6)
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

                    if (OnEdge(a, b, x, y, eps))
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

        static (Rect bounds, List<Vec2> uniquePoints) Preporcess(IList<Vec2> points)
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

        static (List<Triangle> triangles, List<Node> nodes) AddSuperStructure(Rect bounds, List<Vec2> uniquePoints, ESuperStructure superStructure)
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

            List<Triangle> triangles = new List<Triangle>(points.Count);
            List<Node> nodes = new List<Node>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                var (x, y) = points[i];
                nodes.Add(new Node(i, x, y));
            }

            Edge? prevShared = null;
            for (int i = 1; i < points.Count - 1; i++)
            {
                Node nodeA = nodes[0];
                Node nodeB = nodes[i];
                Node nodeC = nodes[i + 1];

                Edge ab = new Edge(nodeA);
                Edge bc = new Edge(nodeB);
                Edge ca = new Edge(nodeC);

                ab.Next = bc;
                bc.Next = ca;
                ca.Next = ab;

                ab.Prev = ca;
                bc.Prev = ab;
                ca.Prev = bc;

                Triangle tri = new Triangle(triangles.Count, ab);
                tri.Area = Area(tri);

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

        static IList<Triangle> RemoveAndBuild(List<Triangle> triangles, Triangle triangle, Node node)
        {
            HashSet<Triangle> removed = new HashSet<Triangle>();
            Stack<Triangle> stack = new Stack<Triangle>();

            stack.Push(triangle);
            double x = node.X;
            double y = node.Y;

            while (stack.Count > 0)
            {
                Triangle current = stack.Pop();
                if (!removed.Add(current))
                    continue;

                foreach (Edge edge in current)
                {
                    Node a = edge.Origin;
                    Node b = edge.Next.Origin;
                    Node c = edge.Prev.Origin;

                    if (DelaunayCriteria.InCircle(x, y, a.X, a.Y, b.X, b.Y, c.X, c.Y))
                    {
                        Edge? twin = edge.Twin;
                        if (twin != null)
                        {
                            Triangle neighbor = twin.Triangle;
                            if (!removed.Contains(neighbor))
                            {
                                stack.Push(neighbor);
                            }
                        }
                    }
                }
            }

            List<Edge> border = new List<Edge>(removed.Count * 2);
            foreach (Triangle tri in removed)
            {
                foreach (Edge edge in tri)
                {
                    if (edge.Twin is not null && removed.Contains(edge.Twin.Triangle))
                    {
                        border.Add(edge);
                    }
                }
            }



            List<Triangle> newTriangles = new List<Triangle>();
            return newTriangles;
        }
    }
}
