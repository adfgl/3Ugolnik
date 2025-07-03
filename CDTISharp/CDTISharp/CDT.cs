using CDTISharp.Geometry;
using CDTISharp.IO;
using CDTISharp.Meshing;
using System.Diagnostics;

namespace CDTISharp
{
    public static class CDT
    {
        const double TO_DEG = 180.0 / Math.PI;

        public static CDTMesh Triangulate()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();



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
