namespace CDTSharp
{
    public class CDTNode
    {
        public CDTNode()
        {
            
        }

        public CDTNode(double x, double y, double z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public override string ToString()
        {
            return $"{X} {Y} {Z}";
        }
    }
}
