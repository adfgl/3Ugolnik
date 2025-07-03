using CDTISharp.Geometry;
using CDTISharp.Meshing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTISharpTests
{
    public static class MeshEx
    {
        public static Node Center(this Mesh mesh, Triangle t)
        {
            Node[] nodes = new Node[3];
            for (int i = 0; i < 3; i++)
            {
                nodes[i] = mesh.Nodes[t.indices[i]];
            }
            return Node.Average(nodes);
        }
    }
}
