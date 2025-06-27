using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public class ArcSegment : Segment
    {
        public ArcSegment(Node start, Node end, Node center, bool clockwise) : base(start, end)
        {
            Center = center;
            Clockwise = clockwise;
        }

        public ArcSegment(double radius, double startAngle, double endAngle, Node center, bool clockwise)
       : base(
           new Node(-1, center.X + radius * Math.Cos(startAngle), center.Y + radius * Math.Sin(startAngle)),
           new Node(-1, center.X + radius * Math.Cos(endAngle), center.Y + radius * Math.Sin(endAngle))
         )
        {
            Center = center;
            Clockwise = clockwise;
        }

        public bool Clockwise { get; set; }
        public Node Center { get; set; }

        public override Node PointAt(double t)
        {
            double cx = Center.X;
            double cy = Center.Y;

            double dx1 = _start.X - cx;
            double dy1 = _start.Y - cy;
            double radius = Math.Sqrt(dx1 * dx1 + dy1 * dy1);

            double angleStart = Math.Atan2(dy1, dx1);
            double dx2 = _end.X - cx;
            double dy2 = _end.Y - cy;
            double angleEnd = Math.Atan2(dy2, dx2);

            angleStart = NormalizeAngle(angleStart);
            angleEnd = NormalizeAngle(angleEnd);

            double angleDelta;
            if (Clockwise)
            {
                if (angleStart < angleEnd)
                {
                    angleStart += 2 * Math.PI;
                }

                angleDelta = angleStart - angleEnd;
            }
            else
            {
                if (angleEnd < angleStart)
                {
                    angleEnd += 2 * Math.PI;
                }

                angleDelta = angleEnd - angleStart;
            }

            double angle = angleStart + (Clockwise ? -1 : 1) * angleDelta * t;
            double x = cx + radius * Math.Cos(angle);
            double y = cy + radius * Math.Sin(angle);
            return new Node(-1, x, y);
        }

        public override double Length()
        {
            double dx = _start.X - Center.X;
            double dy = _start.Y - Center.Y;
            double radius = Math.Sqrt(dx * dx + dy * dy);

            double angleStart = NormalizeAngle(Math.Atan2(dy, dx));
            double angleEnd = NormalizeAngle(Math.Atan2(_end.Y - Center.Y, _end.X - Center.X));

            double angleDelta;
            if (Clockwise)
            {
                if (angleStart < angleEnd)
                    angleStart += 2 * Math.PI;
                angleDelta = angleStart - angleEnd;
            }
            else
            {
                if (angleEnd < angleStart)
                    angleEnd += 2 * Math.PI;
                angleDelta = angleEnd - angleStart;
            }

            return radius * angleDelta;
        }

        public override Segment[] Split(int parts)
        {
            parts = Math.Max(parts, 1);
            Segment[] segments = new Segment[parts];
            for (int i = 0; i < parts; i++)
            {
                double t0 = (double)i / parts;
                double t1 = (double)(i + 1) / parts;
                Node start = PointAt(t0);
                Node end = PointAt(t1);
                segments[i] = new ArcSegment(start, end, Center, Clockwise);
            }
            return segments;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double NormalizeAngle(double angle)
        {
            while (angle < 0) angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
            return angle;
        }
    }
}
