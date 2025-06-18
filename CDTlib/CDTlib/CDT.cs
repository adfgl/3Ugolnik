namespace CDTlib
{
    public class CDT
    {
        readonly QuadTree _quadTree;
        readonly Mesh _mesh = new Mesh();

        public CDT(CDTPolygon contour, CDTQuality quality, List<CDTPolygon>? holes = null, List<CDTSegment>? constraintSegments = null, List<CDTPoint>? constraintPoints = null)
        {
            List<CDTPoint> allContourPoints = new List<CDTPoint>();
            List<CDTPoint> allConstrainedPoints = new List<CDTPoint>();
            List<(CDTPoint, CDTPoint)> constraints = new List<(CDTPoint, CDTPoint)>();

            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            void updateBounds(CDTPoint pt)
            {
                double x = pt.X;
                double y = pt.Y;
                allContourPoints.Add(pt);

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            foreach (CDTSegment segment in contour.Segments)
            {
                IReadOnlyList<CDTSegment> segments = segment.Split(segment.NumSegments);
                foreach (CDTSegment s in segments)
                {
                    updateBounds(s.Start);
                    constraints.Add((s.Start, s.End));
                }
                updateBounds(segments.Last().End);
            }

            if (holes != null)
            {
                foreach (CDTPolygon polygon in holes)
                {
                    foreach (CDTSegment segment in polygon.Segments)
                    {
                        IReadOnlyList<CDTSegment> segments = segment.Split(segment.NumSegments);
                        foreach (CDTSegment s in segments)
                        {
                            updateBounds(s.Start);
                            constraints.Add((s.Start, s.End));
                        }
                        updateBounds(segments.Last().End);
                    }
                }
            }

            if (constraintPoints != null)
            {
                foreach (CDTPoint item in constraintPoints)
                {
                    updateBounds(item);
                }
            }

            _quadTree = new QuadTree(new Rectangle(minX, minY, maxX, maxY));

       
        }

        public Node AddPoint(double x, double y, double? z, out List<int> affectedTriangles)
        {
            affectedTriangles = new List<int>();

            _mesh.FindContaining(x, y, out int triIndex, out int edgeIndex, out int nodeIndex);
            if (nodeIndex != -1)
            {
                return _mesh.Nodes[nodeIndex];
            }

            Node newNode = new Node(_mesh.Nodes.Count, x, y, double.NaN);

            Triangle tri = _mesh.Triangles[triIndex];
            Affected[] affected;

            double zActual;
            if (edgeIndex == -1)
            {
                affected = _mesh.Split(tri, newNode);

                zActual = 0;
                for (int i = 0; i < 3; i++)
                {
                    zActual += _mesh.Nodes[tri.indices[i]].Z;
                }
                zActual /= 3.0;
            }
            else
            {
                affected = _mesh.Split(tri, edgeIndex, newNode);

                tri.Edge(edgeIndex, out int a, out int b);
                zActual = (_mesh.Nodes[a].Z + _mesh.Nodes[b].Z) / 2.0;
            }

            if (z is null)
            {
                newNode.Z = zActual;
            }
            else
            {
                newNode.Z = z.Value;
            }

            // do something before adding?

            _mesh.Nodes.Add(newNode);   
            _quadTree.Add(newNode);

            _mesh.Add(affected);
            _mesh.Legalize(affectedTriangles, affected);
            return newNode;
        }

        public void AddConstraint(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            Node start = AddPoint(x1, y1, z1, out _);
            Node end = AddPoint(x2, y2, z2, out _);

            Queue<(Node, Node)> toInsert = new();
            toInsert.Enqueue((start, end));

            while (toInsert.Count > 0)
            {
                var (a, b) = toInsert.Dequeue();
                if (a.Index == b.Index)
                    continue;

                InsertConstraintSegment(a, b, toInsert);
            }
        }

        private void InsertConstraintSegment(Node start, Node end, Queue<(Node, Node)> toInsert)
        {
            while (true)
            {
                _mesh.FindEdge(start.Index, end.Index, out int triangleIndex, out int edgeIndex);
                if (edgeIndex != -1)
                {
                    _mesh.SetConstraint(triangleIndex, edgeIndex, true);
                    return;
                }

                Triangle current = _mesh.EntranceTriangle(start.Index, end.Index);
                if (WalkAndInsert(current, start, end, toInsert))
                    return;
            }
        }

        private bool WalkAndInsert(Triangle current, Node start, Node end, Queue<(Node, Node)> toInsert)
        {
            List<int> affected = new();

            while (true)
            {
                for (int edge = 0; edge < 3; edge++)
                {
                    if (TrySplitOrFlip(current, edge, start, end, toInsert))
                        return true;
                }

                int exitEdge = FindExitEdge(current, end);
                int adjIndex = current.adjacent[exitEdge];
                if (adjIndex == -1)
                    throw new Exception("Constraint insertion failed: no adjacent triangle during walk.");

                current = _mesh.Triangles[adjIndex];
            }
        }

        private bool TrySplitOrFlip(Triangle triangle, int edge, Node start, Node end, Queue<(Node, Node)> toInsert)
        {
            triangle.Edge(edge, out int aIndex, out int bIndex);
            if (aIndex == start.Index || bIndex == start.Index || aIndex == end.Index || bIndex == end.Index)
                return false;

            Node a = _mesh.Nodes[aIndex];
            Node b = _mesh.Nodes[bIndex];

            Node? intersection = Node.Intersect(a, b, start, end);
            if (intersection is null)
                return false;

            Affected[] tris;
            if (_mesh.CanFlip(triangle, edge))
            {
                tris = _mesh.Flip(triangle, edge);
            }
            else
            {
                Node newNode = new Node(_mesh.Nodes.Count, intersection.X, intersection.Y, intersection.Z);
                tris = _mesh.Split(triangle, edge, newNode);

                toInsert.Enqueue((start, newNode));
                toInsert.Enqueue((newNode, end));

                _mesh.Nodes.Add(newNode);
                _quadTree.Add(newNode);
            }

            _mesh.Add(tris);
            _mesh.Legalize(new List<int>(), tris);

            return true;
        }

        private int FindExitEdge(Triangle triangle, Node target)
        {
            int bestEdge = -1;
            double bestCross = 0;

            for (int edge = 0; edge < 3; edge++)
            {
                triangle.Edge(edge, out int aIndex, out int bIndex);
                Node a = _mesh.Nodes[aIndex];
                Node b = _mesh.Nodes[bIndex];

                double cross = Node.Cross(a, b, target);
                if (bestEdge == -1 || cross < bestCross)
                {
                    bestCross = cross;
                    bestEdge = edge;
                }
            }

            return bestEdge;
        }


        public void Refine(double maxArea)
        {
            HashSet<Segment> seen = new HashSet<Segment>();
            Queue<Triangle> triangleQueue = new Queue<Triangle>();
            Queue<Segment> segmentQueue = new Queue<Segment>();

            foreach (Triangle triangle in _mesh.Triangles)
            {
                if (IsBad(triangle, maxArea))
                {
                    triangleQueue.Enqueue(triangle);
                }

                for (int i = 0; i < 3; i++)
                {
                    if (!triangle.constrained[i])
                    {
                        continue;
                    }

                    triangle.Edge(i, out int a, out int b);

                    Segment segment = new Segment(_mesh.Nodes[a], _mesh.Nodes[b]);
                    if (seen.Add(segment) && Enchrouched(_quadTree, segment))
                    {
                        segmentQueue.Enqueue(segment);
                    }
                }
            }

            while (segmentQueue.Count > 0 || triangleQueue.Count > 0)
            {
                if (segmentQueue.Count > 0)
                {
                    Segment seg = segmentQueue.Dequeue();

                    _mesh.FindEdge(seg.a.Index, seg.b.Index, out int triangle, out int edge);
                    if (triangle == -1 || edge == -1)
                    {
                        throw new Exception($"Midpoint of segment ({seg.a},{seg.b}) not found on any edge.");
                    }

                    double x = seg.circle.x;
                    double y = seg.circle.y;
                    double z = (seg.a.Z + seg.b.Z) * 0.5;

                    Node newNode = new Node(_mesh.Nodes.Count, x, y, z);
                    _mesh.Nodes.Add(newNode);
                    _quadTree.Add(newNode);

                    seg.Split(newNode, out Segment a, out Segment b);
                    seen.Remove(seg);
                    seen.Add(a);
                    seen.Add(b);

                    if (IsVisibleFromInterior(seen, a, x, y) && Enchrouched(_quadTree, a))
                    {
                        segmentQueue.Enqueue(a);
                    }
                    if (IsVisibleFromInterior(seen, b, x, y) && Enchrouched(_quadTree, b))
                    {
                        segmentQueue.Enqueue(b);
                    }

                    List<int> affected = _mesh.SplitAndAdd(_mesh.Triangles[triangle], edge, newNode);
                    foreach (int item in affected)
                    {
                        triangleQueue.Enqueue(_mesh.Triangles[item]);
                    }
                }

                if (triangleQueue.Count > 0)
                {
                    Triangle tri = triangleQueue.Dequeue();
                    if (!IsBad(tri, maxArea))
                    {
                        continue;
                    }

                    double x = tri.circle.x;
                    double y = tri.circle.y;

                    bool encroaches = false;
                    foreach (Segment seg in seen)
                    {
                        if (seg.circle.Contains(x, y) && IsVisibleFromInterior(seen, seg, x, y))
                        {
                            segmentQueue.Enqueue(seg);
                            encroaches = true;
                        }
                    }

                    if (encroaches)
                    {
                        continue;
                    }

                    Node inserted = AddPoint(x, y, null, out List<int> affected);
                    foreach (int item in affected)
                    {
                        triangleQueue.Enqueue(_mesh.Triangles[item]);
                    }
                }
            }
        }

        public bool IsBad(Triangle triangle, double maxAllowedArea)
        {
            if (triangle.super)
            {
                return false;
            }

            if (triangle.area > maxAllowedArea)
            {
                return true;
            }

            double minEdgeSq = double.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                if (triangle.indices[i] < 3)
                {
                    return false;
                }

                triangle.Edge(i, out int start, out int end);

                Node a = _mesh.Nodes[start];
                Node b = _mesh.Nodes[end];
                double lenSqr = Node.SquareDistance(a, b);
                if (minEdgeSq > lenSqr)
                {
                    minEdgeSq = lenSqr;
                }
            }
            return triangle.circle.radiusSqr / minEdgeSq > 2;
        }


        public bool Enchrouched(QuadTree nodes, Segment segment)
        {
            Node a = segment.a;
            Node b = segment.b;

            Rectangle bound = new Rectangle(segment.circle);
            List<Node> points = nodes.Query(bound);

            for (int i = 0; i < 3; i++)
            {
                points.Add(_mesh.Nodes[i]);
            }

            foreach (Node n in points)
            {
                if (n == a || n == b) continue;
                if (segment.circle.Contains(n.X, n.Y))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsVisibleFromInterior(IEnumerable<Segment> segments, Segment segment, double x, double y)
        {
            Node node = new Node(-1, x, y, 0);
            Node mid = new Node(-1, segment.circle.x, segment.circle.y, 0);
            foreach (Segment seg in segments)
            {
                if (seg.Equals(segment))
                    continue;

                if (Node.Intersect(mid, node, seg.a, seg.b) is not null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
