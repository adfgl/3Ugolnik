using CDTSharp.Geometry;
using CDTSharp.Meshing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharpTests
{
    public static class MeshTestEx
    {
        public static Mesh BruteForceTwins(this Mesh mesh)
        {
            List<Triangle> tris = mesh.Triangles;
            foreach (Triangle t0 in tris)
            {
                foreach (Edge e0 in t0.Forward())
                {
                    if (e0.Twin is not null) continue;

                    var (a0, b0) = e0;

                    foreach (Triangle t1 in tris)
                    {
                        if (t0 == t1) continue;

                        foreach (Edge e1 in t1.Forward())
                        {
                            var (a1, b1) = e1;

                            if (a1 == b0 && b1 == a0)
                            {
                                e0.Twin = e1;
                                e1.Twin = e0;
                            }
                        }
                    }
                }
            }
            return mesh;
        }
    }
}
