namespace CDTSharp.IO
{
    public class CDTEdge
    {
        public int Origin { get; set; } = -1;
        public int Adjacent { get; set; } = -1;
        public double Length { get; set; }
        public double Angle { get; set; }
    }
}
