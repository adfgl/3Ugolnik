using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTlib
{
    public class MeshSummary
    {
        public int TriangleCount { get; }
        public int VertexCount { get; }
        public double MinArea { get; }
        public double MaxArea { get; }
        public double AvgArea { get; }
        public double MinAngle { get; }
        public double MaxAngle { get; }
        public double AvgAngle { get; }

        public MeshSummary(int triangleCount, int vertexCount, double minArea, double maxArea, double avgArea,
            double minAngle, double maxAngle, double avgAngle)
        {
            TriangleCount = triangleCount;
            VertexCount = vertexCount;
            MinArea = minArea;
            MaxArea = maxArea;
            AvgArea = avgArea;
            MinAngle = minAngle;
            MaxAngle = maxAngle;
            AvgAngle = avgAngle;
        }

        public static MeshSummary ComputeMeshSummary(Mesh mesh)
        {
            double minArea = double.MaxValue;
            double maxArea = double.MinValue;
            double totalArea = 0;

            double minAngle = double.MaxValue;
            double maxAngle = double.MinValue;
            double totalAngle = 0;

            foreach (var tri in mesh.Triangles)
            {
                if (tri.super || tri.parents.Count == 0)
                {
                    continue;
                }

                double area = tri.area;
                totalArea += area;
                if (area < minArea) minArea = area;
                if (area > maxArea) maxArea = area;

                for (int i = 0; i < 3; i++)
                {
                    var a = mesh.Nodes[tri.indices[(i + 2) % 3]];
                    var b = mesh.Nodes[tri.indices[i]];
                    var c = mesh.Nodes[tri.indices[(i + 1) % 3]];
                    double ang = Node.Angle(a, b, c) * 180.0 / Math.PI;

                    if (ang < minAngle) minAngle = ang;
                    if (ang > maxAngle) maxAngle = ang;
                    totalAngle += ang;
                }
            }

            int triCount = mesh.Triangles.Count;
            int nodeCount = mesh.Nodes.Count;
            double avgArea = triCount > 0 ? totalArea / triCount : 0;
            double avgAngle = triCount > 0 ? totalAngle / (3 * triCount) : 0;

            return new MeshSummary(
                triCount,
                nodeCount,
                minArea,
                maxArea,
                avgArea,
                minAngle,
                maxAngle,
                avgAngle
            );
        }


        public override string ToString()
        {
            return $@"
Mesh Summary:
Triangles: {TriangleCount}
Vertices:  {VertexCount}
Area   Min: {MinArea:F4} | Max: {MaxArea:F4} | Avg: {AvgArea:F4}
Angle  Min: {MinAngle:F2}° | Max: {MaxAngle:F2}° | Avg: {AvgAngle:F2}°";
        }
    }

}
