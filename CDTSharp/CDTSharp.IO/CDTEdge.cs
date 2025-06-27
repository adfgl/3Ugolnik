namespace CDTSharp.IO
{
    public class CDTEdge
    {
        public int Triangle { get; set; } = -1;
        public int Origin { get; set; } = -1;
        public int Adjacent { get; set; } = -1;
        public double Length { get; set; }
        public double Angle { get; set; }
        public CDTConstraintType Constraint { get; set; } = CDTConstraintType.None;

        public override string ToString()
        {
            return $"[Tri {Triangle,3}] {(Constraint == CDTConstraintType.None ? "" : $"({Constraint}) " )}Origin: {Origin,3} | Adjacent: {Adjacent,3} | Length: {Length,7:F4} | Angle: {Angle,6:F2}°";
        }
    }
}
