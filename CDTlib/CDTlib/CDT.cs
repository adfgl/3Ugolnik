using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CDTlib
{
    public class CDT
    {
        QuadTree _quadTree = new QuadTree(new Rectangle());
        Mesh _mesh = new Mesh();

        public Node AddPoint(double x, double y, out List<int> affectedTriangles)
        {
            affectedTriangles = new List<int>();

            _mesh.FindContaining(x, y, out int triIndex, out int edgeIndex, out int nodeIndex);
            if (nodeIndex != -1)
            {
                return _mesh.Nodes[nodeIndex];
            }

            Node newNode = new Node(_mesh.Nodes.Count, x, y);

            Triangle tri = _mesh.Triangles[triIndex];
            Affected[] affected;
            if (edgeIndex == -1)
            {
                affected = _mesh.Split(tri, newNode);
            }
            else
            {
                affected = _mesh.Split(tri, edgeIndex, newNode);
            }

            // do something before adding?

            _mesh.Add(affected);
            _quadTree.Add(newNode);

            _mesh.Legalize(affectedTriangles, affected);
            return newNode;
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
                    Node newNode = new Node(_mesh.Nodes.Count, x, y);

                    List<int> affected = _mesh.SplitAndAdd(_mesh.Triangles[triangle], edge, newNode);

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

                    Node inserted = AddPoint(x, y, out List<int> affected);
                    foreach (int item in affected)
                    {
                        triangleQueue.Enqueue(_mesh.Triangles[item]);
                    }
                }
            }
        }

        public bool IsBad(Triangle triangle, double maxAllowedArea)
        {
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

            if (triangle.area > maxAllowedArea)
            {
                return true;
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
            Node node = new Node(-1, x, y);
            Node mid = new Node(-1, segment.circle.x, segment.circle.y);
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

        public void AddConstraint(double x1, double y1, double x2, double y2)
        {
            Node start = AddPoint(x1, y1, out _);
            Node end = AddPoint(x2, y2, out _);

            int startIndex = start.Index;
            int endIndex = end.Index;

            if (startIndex == endIndex)
            {
                return;
            }

            while (true)
            {
                _mesh.FindEdge(startIndex, endIndex, out int triangleIndex, out int edgeIndex);

                if (edgeIndex != -1)
                {
                    _mesh.SetConstraint(triangleIndex, edgeIndex, true);
                    return;
                }

                Triangle current = _mesh.EntranceTriangle(startIndex, endIndex);
                List<int> affected = new List<int>();

                bool changed = false;
                while (true)
                {
                    affected.Clear();
                    for (int edge = 0; edge < 3; edge++)
                    {
                        current.Edge(edge, out int aIndex, out int bIndex);
                        if (aIndex == startIndex || bIndex == startIndex || 
                            aIndex == endIndex || bIndex == endIndex)
                        {
                            continue;
                        }

                        Node a = _mesh.Nodes[aIndex];
                        Node b = _mesh.Nodes[bIndex];

                        Node? inter = Node.Intersect(a, b, start, end);
                        if (inter is null)
                        {
                            continue;
                        }

                        Affected[] tris;
                        if (!_mesh.CanFlip(current, edge))
                        {
                            tris = _mesh.Split(current, edge, inter);
                        }
                        else
                        {
                            tris = _mesh.Flip(current, edge);
                        }

                        _mesh.Add(tris);
                        _mesh.Legalize(affected, tris);

                        changed = true;
                        break;
                    }

                    if (changed)
                    {
                        break;
                    }

                    int next = -1;
                    double bestCross = 0;
                    for (int edge = 0; edge < 3; edge++)
                    {
                        current.Edge(edge, out int aIndex, out int bIndex);
                        Node a = _mesh.Nodes[aIndex];
                        Node b = _mesh.Nodes[bIndex];

                        double cross = Node.Cross(a, b, end);
                        if (next == -1 || cross < bestCross)
                        {
                            bestCross = cross;
                            next = edge;
                        }
                    }

                    int adjIndex = current.adjacent[next];
                    if (adjIndex == -1)
                    {
                        throw new Exception("Stuck during constraint insertion. Mesh may be invalid or degenerate.");
                    }
                    current = _mesh.Triangles[adjIndex];
                }
            }
        }
    }
}
