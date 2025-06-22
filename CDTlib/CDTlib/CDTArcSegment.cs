
namespace CDTlib
{
    public class CDTArcSegment : CDTSegment
    {
        public CDTNode A { get; }
        public CDTNode B { get; }
        public CDTNode Center { get; }
        public bool Clockwise { get; }

        public override CDTNode Start => A;
        public override CDTNode End => B;

        public override double Length
        {
            get
            {
                double radius = Math.Sqrt(Math.Pow(A.X - Center.X, 2) + Math.Pow(A.Y - Center.Y, 2));
                double angleA = Math.Atan2(A.Y - Center.Y, A.X - Center.X);
                double angleB = Math.Atan2(B.Y - Center.Y, B.X - Center.X);
                double delta = Clockwise ? NormalizeAngle(angleA - angleB) : NormalizeAngle(angleB - angleA);
                return radius * delta;
            }
        }

        public CDTArcSegment(CDTNode a, CDTNode b, CDTNode center, bool clockwise)
        {
            A = a;
            B = b;
            Center = center;
            Clockwise = clockwise;
        }

        public override CDTNode PointAt(double t)
        {
            double angleA = Math.Atan2(A.Y - Center.Y, A.X - Center.X);
            double angleB = Math.Atan2(B.Y - Center.Y, B.X - Center.X);

            double angleDelta = Clockwise
                ? NormalizeAngle(angleA - angleB)
                : NormalizeAngle(angleB - angleA);

            double angle = angleA + (Clockwise ? -1 : 1) * angleDelta * t;
            double radius = Math.Sqrt(Math.Pow(A.X - Center.X, 2) + Math.Pow(A.Y - Center.Y, 2));

            return new CDTNode
            {
                X = Center.X + radius * Math.Cos(angle),
                Y = Center.Y + radius * Math.Sin(angle),
                Z = A.Z + t * (B.Z - A.Z)
            };
        }

        public override IReadOnlyList<CDTSegment> Split(int parts)
        {
            var list = new List<CDTSegment>(parts);
            for (int i = 0; i < parts; i++)
            {
                double t0 = (double)i / parts;
                double t1 = (double)(i + 1) / parts;
                CDTNode p0 = PointAt(t0);
                CDTNode p1 = PointAt(t1);
                list.Add(new CDTArcSegment(p0, p1, Center, Clockwise));
            }
            return list;
        }

        private static double NormalizeAngle(double angle)
        {
            while (angle < 0) angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
            return angle;
        }
    }
}
