namespace CDTISharp.IO
{
    public class CDTEdge
    {
        public int Triangle { get; set; } = -1;
        public int Origin { get; set; } = -1;
        public int Adjacent { get; set; } = -1;
        public int Constraint { get; set; } = -1;
        public double Length { get; set; }
        public double Angle { get; set; }

        public override string ToString()
        {
            return $"[Tri {Triangle,3}] {(Constraint == -1 ? "" : $"({Constraint}) ")}Origin: {Origin,3} | Adjacent: {Adjacent,3} | Length: {Length,7:F4} | Angle: {Angle,6:F2}°";
        }
    }
}
