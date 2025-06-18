using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
