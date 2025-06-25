namespace CDTSharp
{
    public class HeNode
    {
        public HeNode(double x, double y)
        {
            X = x; 
            Y = y;
            Edge = null!;
        }

        public void Deconstruct(out double x, out double y)
        {
            x = X; y = Y;
        }

        public HeEdge Edge { get; set; }    
        public double X { get; set; }
        public double Y { get; set; }
    }
}
