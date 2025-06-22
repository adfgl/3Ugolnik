namespace CDTlib
{
    public abstract class CDTSegment
    {
        public abstract CDTNode Start { get; }
        public abstract CDTNode End { get; }

        /// <summary>
        /// Splits this segment into smaller subsegments.
        /// </summary>
        public abstract IReadOnlyList<CDTSegment> Split(int parts);

        /// <summary>
        /// Gets a point on the segment at t in [0,1]
        /// </summary>
        public abstract CDTNode PointAt(double t);

        public abstract double Length { get; }

        public int NumSegments { get; set; } = 1;

        public IReadOnlyList<CDTSegment> Split() => Split(Math.Max(1, NumSegments));

        protected static double Distance(CDTNode a, CDTNode b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double dz = b.Z - a.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
