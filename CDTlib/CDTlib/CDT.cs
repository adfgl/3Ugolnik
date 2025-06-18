using System;
using System.Collections.Generic;
using System.Linq;
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

        }

  

    }
}
