using CDTISharp.Geometry;

namespace CDTISharp.Meshing
{
    public class Mesh
    {
        public readonly static int[] NEXT = [1, 2, 0];
        public readonly static int[] PREV = [2, 0, 1];

        readonly List<Triangle> _triangles;
        readonly QuadTree _qt;
        readonly List<Node> _nodes;
        readonly Rectangle _bounds;

        public List<Triangle> Triangles => _triangles;
        public List<Node> Nodes => _nodes;
        public Rectangle Bounds => _bounds;

        public Mesh(Rectangle rectangle)
        {
            _triangles = new List<Triangle>();
            _bounds = rectangle;
            _qt = new QuadTree(rectangle);
            _nodes = new List<Node>();
        }

        public Mesh AddSuperStructure(Rectangle bounds, double scale)
        {
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double size = Math.Max(scale, 2) * dmax;

            Node a = new Node(0, midx - size, midy - size);
            Node b = new Node(1, midx + size, midy - size);
            Node c = new Node(2, midx, midy + size);

            _nodes.Add(a);
            _nodes.Add(b);
            _nodes.Add(c);

            Triangle triangle = new Triangle(0, a, b, c);
            _triangles.Add(triangle);

            a.Triangle = b.Triangle = c.Triangle = triangle.index;
            return this;
        }

        public void Refine(Quality quality, double eps)
        {
            Stack<int> affected = new Stack<int>();

            List<Node> nodes = _nodes;
            HashSet<Constraint> seen = new HashSet<Constraint>();
            Queue<int> triangleQueue = new Queue<int>();
            Queue<Constraint> segmentQueue = new Queue<Constraint>();

            foreach (Triangle t in _triangles)
            {
                if (Bad(t, quality))
                {
                    triangleQueue.Enqueue(t.index);
                }

                for (int i = 0; i < 3; i++)
                {
                    int type = t.constraints[i];
                    if (type == -1) continue;

                    Node a = nodes[t.indices[i]];
                    Node b = nodes[t.indices[NEXT[i]]];
                    Constraint constraint = new Constraint(a, b, type);
                    if (seen.Add(constraint) && constraint.Enchrouched(_qt))
                    {
                        segmentQueue.Enqueue(constraint);
                    }
                }
            }

            while (segmentQueue.Count > 0 || triangleQueue.Count > 0)
            {
                if (segmentQueue.Count > 0)
                {
                    Constraint constraint = segmentQueue.Dequeue();
                    SearchResult? result = Navigation.FindEdge(_triangles, constraint.start, constraint.end);
                    if (result is null || result.Edge == -1)
                    {
                        continue;
                    }

                    Node node = new Node() { Index = nodes.Count,  X = constraint.circle.x, Y = constraint.circle.y };
                    Triangle[] tris = Splitting.Split(_triangles, _nodes, result.Triangle, result.Edge, node);
                    Add(tris);
                    Legalize(affected, tris);

                    while (affected.Count > 0)
                    {
                        triangleQueue.Enqueue(affected.Pop());
                    }

                    seen.Remove(constraint);
                    foreach (Constraint e in constraint.Split(node))
                    {
                        if (seen.Add(e) && e.Enchrouched(_qt) && e.VisibleFromInterior(seen, node))
                        {
                            segmentQueue.Enqueue(e);
                        }
                    }
                }

                if (triangleQueue.Count > 0)
                {
                    Triangle t = _triangles[triangleQueue.Dequeue()];
                    if (!Bad(t, quality))
                    {
                        continue;
                    }

                    double x = t.circle.x;
                    double y = t.circle.y;
                    if (!_qt.Bounds.Contains(x, y))
                    {
                        continue;
                    }

                    Node node = new Node() { X = x, Y = y };
                    bool encroaches = false;
                    foreach (Constraint seg in seen)
                    {
                        if (seg.circle.Contains(x, y) && seg.VisibleFromInterior(seen, node))
                        {
                            segmentQueue.Enqueue(seg);
                            encroaches = true;
                        }
                    }

                    if (encroaches)
                    {
                        continue;
                    }

                    Node? inserted = Add(affected, node, eps);
                    if (inserted == node)
                    {
                        while (affected.Count > 0)
                        {
                            triangleQueue.Enqueue(affected.Pop());
                        }
                    }
                }
            }
        }

        public bool Bad(Triangle t, Quality q)
        {
            if (t.super || t.partOfHole)
            {
                return false;
            }

            List<Node> nodes = _nodes;

            double edgeLenLimSqr = q.MaxEdgeLength;
            edgeLenLimSqr *= edgeLenLimSqr;

            double minEdgeSqr = double.MaxValue;
            double area = -1;
            for (int i = 0; i < 3; i++)
            {
                Node a = nodes[t.indices[i]];
                Node b = nodes[t.indices[NEXT[i]]];
                double lenSqr = GeometryHelper.SquareLength(a, b);
                if (q.MaxEdgeLength > 0 && lenSqr > edgeLenLimSqr)
                {
                    return true;
                }

                if (minEdgeSqr > lenSqr)
                {
                    minEdgeSqr = lenSqr;
                }

                Node c = nodes[t.indices[PREV[i]]];
                if (area < 0)
                {
                    area = GeometryHelper.Cross(a, b, c.X, c.Y) * 0.5;
                    if (area < 0)
                    {
                        throw new Exception("Wrong winding order.");
                    }

                    if (area > q.MaxArea)
                    {
                        return true;
                    }
                }

                if (q.MinAngle > 0)
                {
                    double rad = GeometryHelper.Angle(c, a, b);
                    if (rad < q.MinAngle)
                    {
                        return true;
                    }
                }
            }
            return t.circle.radiusSqr / minEdgeSqr > 2;
        }

        public Node? Add(Stack<int> affected, Node point, double eps)
        {
            List<int> visited = new List<int>();
            SearchResult? result = Navigation.FindContaining(_triangles, _nodes, point, visited, eps);
            if (result is null)
            {
                throw new Exception();
            }

            if (result.Node != -1)
            {
                return Nodes[result.Node];
            }
            return Add(affected, result.Triangle, result.Edge, point);
        }

        public Node? Add(Stack<int> affected, int triangle, int edge, Node point)
        {
            if (triangle == -1)
            {
                return null;
            }

            point.Index = _qt.Items.Count;

            Triangle[] triangles;
            if (edge != -1)
            {
                triangles = Splitting.Split(_triangles, _nodes, triangle, edge, point);
            }
            else
            {
                triangles = Splitting.Split(_triangles, _nodes, triangle, point);
            }

            // do something before adding?

            _qt.Add(point);
            _nodes.Add(point);
            Add(triangles);
            Legalize(affected, triangles);
            return point;
        }

        public void Add(Stack<int> affected, Node start, Node end, int type, bool alwaysSplit, double eps)
        {
            Node? a = Add(affected, start, eps);
            Node? b = Add(affected, end, eps);
            if (a is null || b is null)
            {
                return;
            }

            Queue<Constraint> toInsert = new Queue<Constraint>();
            toInsert.Enqueue(new Constraint(a, b, type));
            while (toInsert.Count > 0)
            {
                Constraint constraint = toInsert.Dequeue();
                if (constraint.Degenerate())
                {
                    continue;
                }

                SearchResult? result = Navigation.FindEdge(_triangles, constraint.start, constraint.end);
                if (result is null)
                {
                    throw new Exception();
                }

                if (result.Edge != -1)
                {
                    SetConstraint(result.Triangle, result.Edge, type);
                }
                else
                {
                    Triangle entrance = EntranceTriangle(constraint.start.Index, constraint.end.Index);
                    if (WalkAndInsert(affected, entrance, constraint, toInsert, alwaysSplit, eps))
                    {
                        continue;
                    }
                }
            }
        }

        bool WalkAndInsert(Stack<int> affected, Triangle current, Constraint constraint, Queue<Constraint> toInsert, bool alwaysSplit, double eps)
        {
            while (true)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (TrySplitOrFlip(affected, current, i, constraint, toInsert, alwaysSplit, eps))
                    {
                        return true;
                    }
                }

                int exitEdge = FindExitEdge(current, constraint.end);
                int adjIndex = current.adjacent[exitEdge];
                if (adjIndex == -1)
                    throw new Exception("Constraint insertion failed: no adjacent triangle during walk.");
            }
        }

        bool TrySplitOrFlip(Stack<int> affected, Triangle triangle, int edge, Constraint constraint, Queue<Constraint> toInsert, bool alwaysSplit, double eps)
        {
            Node a = _nodes[triangle.indices[edge]];
            Node b = _nodes[triangle.indices[NEXT[edge]]];
            if (constraint.Contains(a, eps) || constraint.Contains(b, eps))
            {
                return false;
            }

            Node? inter = GeometryHelper.Intersect(a, b, constraint.start, constraint.end);
            if (inter is null)
            {
                return false;
            }

            if (alwaysSplit || !Flipping.CanFlip(_triangles, _nodes, triangle.index, edge))
            {
                Node? inserted = Add(affected, triangle.index, edge, inter);
                if (inter != inserted)
                {
                    return false;
                }

                foreach (Constraint c in constraint.Split(inserted, eps))
                {
                    toInsert.Enqueue(c);
                }
            }
            else
            {
                Triangle[] flipped = Flipping.Flip(_triangles, _nodes, triangle.index, edge);
                Add(flipped);
                Legalize(affected, flipped);
            }
            return true;
        }

        public int FindExitEdge(Triangle triangle, Node node)
        {
            List<Node> nodes = _nodes;
            int bestEdge = -1;
            double bestCross = 0;
            for (int edge = 0; edge < 3; edge++)
            {
                Node a = nodes[triangle.indices[edge]];
                Node b = nodes[triangle.indices[NEXT[edge]]];

                double cross = GeometryHelper.Cross(a, b, node.X, node.Y);
                if (bestEdge == -1 || cross < bestCross)
                {
                    bestCross = cross;
                    bestEdge = edge;
                }
            }

            return bestEdge;
        }

        public void SetConstraint(int triangle, int edge, int value)
        {
            Triangle tri = _triangles[triangle];
            tri.constraints[edge] = value;

            int adjIndex = tri.adjacent[edge];
            if (adjIndex == -1)
            {
                return;
            }

            int a = tri.indices[edge];
            int b = tri.indices[NEXT[edge]];

            Triangle adj = _triangles[adjIndex];
            adj.constraints[adj.IndexOf(b, a)] = value;
        }

        public Triangle EntranceTriangle(int start, int end)
        {
            Node nodeA = _nodes[start];
            Node nodeB = _nodes[end];

            double x = nodeB.X;
            double y = nodeB.Y;

            TriangleWalker walker = new TriangleWalker(_triangles, nodeA.Triangle, nodeA.Index);
            do
            {
                Triangle current = _triangles[walker.Current];
                int e0 = walker.Edge0;
                Node a0 = _nodes[current.indices[e0]];
                Node b0 = _nodes[current.indices[NEXT[e0]]];
                if (GeometryHelper.Cross(a0, b0, x, y) < 0)
                {
                    continue;
                }

                int e1 = walker.Edge1;
                Node a1 = _nodes[current.indices[e1]];
                Node b1 = _nodes[current.indices[NEXT[e1]]];
                if (GeometryHelper.Cross(a1, b1, x, y) < 0)
                {
                    continue;
                }
                return current;
            }
            while (walker.MoveNext());

            throw new Exception("Could not find entrance triangle.");
        }

        public void Legalize(Stack<int> affected, Triangle[] triangles)
        {
            Stack<Triangle> toLegalize = new Stack<Triangle>(triangles);
            while (toLegalize.Count > 0)
            {
                Triangle t = toLegalize.Pop();
                if (!Flipping.CanFlip(_triangles, _nodes, t.index, 0) || !Flipping.ShouldFlip(_triangles, _nodes, t.index, 0))
                {
                    continue;
                }

                Triangle[] flipped = Flipping.Flip(_triangles, _nodes, t.index, 0);
                Add(flipped);

                foreach (Triangle f in flipped)
                {
                    affected.Push(f.index);
                    toLegalize.Push(f);
                }
            }
        }



        public void Add(Triangle[] tris)
        {
            int n = tris.Length;
            for (int i = 0; i < n; i++)
            {
                Triangle t = tris[i];

                int index = t.index;
                if (index < _triangles.Count)
                {
                    SetConnectivity(_triangles[index], false);
                    _triangles[index] = t;
                }
                else
                {
                    _triangles.Add(t);
                }
            }

            for (int i = 0; i < n; i++)
            {
                SetConnectivity(tris[i], true);
            }
        }

        public void SetConnectivity(Triangle t, bool connect)
        {
            int ti = t.index;
            for (int i = 0; i < 3; i++)
            {
                int start = t.indices[i];
                Node origin = _nodes[start];

                if (connect)
                {
                    if (origin.Triangle == -1)
                    {
                        origin.Triangle = ti;
                    }
                }
                else
                {
                    if (origin.Triangle == ti)
                    {
                        origin.Triangle = -1;
                    }
                }

                int adjIndex = t.adjacent[i];
                if (adjIndex == -1) continue;

                Triangle adj = _triangles[adjIndex];
                int end = t.indices[NEXT[i]];
                int twin = adj.IndexOf(end, start);

                if (twin == -1) continue;

                if (connect)
                {
                    if (adj.adjacent[twin] == -1)
                    {
                        adj.adjacent[twin] = ti;
                    }
                }
                else
                {
                    if (adj.adjacent[twin] == ti)
                    {
                        adj.adjacent[twin] = -1;
                    }
                }
            }
        }


    

    }

  
}
