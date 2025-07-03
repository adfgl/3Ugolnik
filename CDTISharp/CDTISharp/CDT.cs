using CDTISharp.Geometry;
using CDTISharp.IO;
using CDTISharp.Meshing;
using System.Diagnostics;

namespace CDTISharp
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
                mesh.Refine(new Quality()
                {
                    MaxArea = quality.MaxArea,
                    MaxEdgeLength = quality.MaxEdgeLength,
                    MinAngle = quality.MinAngle,
                }, 1e-6);
            }

#if DEBUG
            Console.WriteLine(mesh.ToSvg());
#endif

            sw.Stop();
            long execution = sw.ElapsedMilliseconds;

            return new CDTMesh();
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
