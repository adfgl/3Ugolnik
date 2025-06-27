using CDTSharp.Geometry;
using CDTSharp.IO;
using CDTSharp.Meshing;
using System.Diagnostics;

namespace CDTSharp
{
    public static class CDT
    {
        const bool VALIDATE_HOLES = true;
        const double TO_DEG = 180.0 / Math.PI;

        public static CDTMesh Triangulate(CDTInput input)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            ClosedPolygon contour = ToPolygon(input.Contour);
            List<ClosedPolygon> holes = new List<ClosedPolygon>();
            foreach (List<CDTSegment> cdtHole in input.Holes)
            {
                ClosedPolygon candidate = ToPolygon(cdtHole);
             
                bool addHole = false;
                if (VALIDATE_HOLES && (contour.Contains(candidate) || contour.Intersects(candidate)))
                {
                    addHole = true;
                    for (int i = holes.Count - 1; i >= 0; i--)
                    {
                        ClosedPolygon existing = holes[i];
                        if (existing.Contains(candidate))
                        {
                            addHole = false;
                            break;
                        }

                        if (candidate.Contains(existing))
                        {
                            holes.RemoveAt(i);
                        }
                    }
                }

                if (addHole)
                {
                    holes.Add(candidate);
                }
            }
            contour.Holes = holes;

            List<(Node a, Node b)> userEdgeConstraints = new List<(Node a, Node b)>();
            foreach (CDTSegment item in input.ConstraintEdges)
            {
                Segment segment = item.ToSegment();
                Segment[] split = segment.Split();

                foreach (LineSegment line in split)
                {
                    userEdgeConstraints.Add((line.Start, line.End));
                }
            }

            List<Node> userNodeConstraints = new List<Node>();
            foreach (CDTNode item in input.ConstraintNodes)
            {
                userNodeConstraints.Add(item.ToNode());
            }

            Mesh mesh = new Mesh(contour, userEdgeConstraints, userNodeConstraints);
            CDTQuality? quality = input.Quality;
            if (quality is not null)
            {
                mesh = mesh.Refine(new Quality()
                {
                    MaxArea = quality.MaxArea,
                    MaxEdgeLength = quality.MaxEdgeLength,
                    MinAngle = quality.MinAngle,
                });
            }
            mesh = mesh.RemoveSuperStructure();

#if DEBUG
            Console.WriteLine(mesh.ToSvg());
#endif


            CDTNode[] nodes = new CDTNode[mesh.Nodes.Count];
            for (int i = 0; i < nodes.Length; i++)
            {
                Node n = mesh.Nodes[i];
                nodes[i] = new CDTNode()
                {
                    X = n.X,
                    Y = n.Y,
                };
            }

            double avgEdge = 0;
            double avgArea = 0;
            double avgAngle = 0;

            double minArea, minAngle, minEdge, maxArea, maxAngle, maxEdge;
            minArea = minAngle = minEdge = double.MaxValue;
            maxArea = maxAngle = maxEdge = double.MinValue;

            int numTris = mesh.Triangles.Count;
            CDTTriangle[] triangles = new CDTTriangle[numTris];
            for (int i = 0; i < numTris; i++)
            {
                Triangle t = mesh.Triangles[i];
                double area = t.Area();

                CDTEdge[] edges = new CDTEdge[3];
                int edgeCount = 0;

                double curAvgAngle = 0;
                double curAvgEdge = 0;
                foreach (Edge e in t.Forward())
                {
                    double len = 0;
                    double ang = 0;

                    Node a = e.Prev.Origin;
                    var (b, c) = e;
                    len = GeometryHelper.Distance(b, c);
                    ang = GeometryHelper.Angle(a, b, c) * TO_DEG;

                    curAvgEdge += len;
                    curAvgAngle += ang;
                    if (minEdge > len) minEdge = len;
                    if (maxEdge < len) maxEdge = len;
                    if (minAngle > ang) minAngle = ang;
                    if (maxAngle > ang) maxAngle = ang;

                    edges[edgeCount++] = new CDTEdge()
                    {
                        Triangle = t.Index,
                        Adjacent = e.Twin is null ? -1 : e.Twin.Triangle.Index,
                        Origin = e.Origin.Index,
                        Length = len,
                        Angle = ang,
                        Constraint = (CDTConstraintType)e.Constrained
                    };
                }
                curAvgAngle /= 3.0;
                curAvgEdge /= 3.0;

                if (minArea > area) minArea = area;
                if (maxArea < area) maxArea = area;

                avgAngle += curAvgAngle;
                avgEdge += curAvgEdge;
                avgArea += area;

                triangles[i] = new CDTTriangle()
                {
                    Edges = edges,
                    Area = area
                };
            }

            avgArea /= numTris;
            avgEdge /= numTris;
            avgAngle /= numTris;

            sw.Stop();
            long execution = sw.ElapsedMilliseconds;

            CDTSummary summary = new CDTSummary()
            {
                AvgAngle = avgAngle,
                AvgEdge = avgEdge,
                AvgArea = avgArea,
                MaxAngle = maxAngle,
                MaxArea = maxArea,
                MinAngle = minAngle,
                MinArea = minArea,
                Execution = execution,
                MaxEdge = maxEdge,
                MinEdge = minEdge,
                TriangleCount = triangles.Length,
                NodeCount = triangles.Length
            };

#if DEBUG
            Console.WriteLine();
            Console.WriteLine(summary);
#endif

            return new CDTMesh()
            {
                Nodes = nodes,
                Triangles = triangles,
                Summary = summary
            };
        }

        static ClosedPolygon ToPolygon(IEnumerable<CDTSegment> segments)
        {
            List<Node> nodes = new List<Node>();
            foreach (CDTSegment item in segments)
            {
                Segment segment = item.ToSegment();
                Segment[] split = segment.Split();
                foreach (Segment s in split)
                {
                    nodes.Add(s.Start);
                    nodes.Add(s.End);
                }
            }
            return new ClosedPolygon(nodes);
        }

        static Node ToNode(this CDTNode node)
        {
            return new Node() { X = node.X, Y = node.Y };
        }

        static Segment ToSegment(this CDTSegment item)
        {
            Segment segment;
            switch (item.Type)
            {
                case CDTSegmentType.Line:
                    CDTLineSegment line = (CDTLineSegment)item;
                    segment = new LineSegment(line.Start.ToNode(), line.End.ToNode());
                    break;

                case CDTSegmentType.Arc:
                    CDTArcSegment arc = (CDTArcSegment)item;
                    segment = new ArcSegment(arc.Start.ToNode(), arc.End.ToNode(), arc.Center.ToNode(), arc.Clockwise);
                    break;

                case CDTSegmentType.Bezier:
                    CDTBezierSegment bzr = (CDTBezierSegment)item;
                    segment = new BezierSegment(bzr.ControlPoints.Select(o => o.ToNode()));
                    break;

                default:
                    throw new NotImplementedException();
            }

            segment.Segments = item.Segments;
            return segment;
        }

    }
}
