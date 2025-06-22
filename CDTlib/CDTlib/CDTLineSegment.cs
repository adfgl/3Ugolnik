namespace CDTlib
{
    public class CDTLineSegment : CDTSegment
    {
        public CDTNode A { get; }
        public CDTNode B { get; }

        public override CDTNode Start => A;
        public override CDTNode End => B;

        public override double Length
        {
            get
            {
                return Distance(A, B);
            }
        }


        public CDTLineSegment(CDTNode a, CDTNode b)
        {
            A = a;
            B = b;
        }

        public override CDTNode PointAt(double t)
        {
            return new CDTNode
            {
                X = A.X + t * (B.X - A.X),
                Y = A.Y + t * (B.Y - A.Y),
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
                list.Add(new CDTLineSegment(p0, p1));
            }
            return list;
        }
    }
}
