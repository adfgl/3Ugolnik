namespace CDTSharp.IO
{
    public class CDTNode
    {
        public CDTNode()
        {
            
        }

        public CDTNode(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; set; }
        public double Y { get; set; }

        public override string ToString()
        {
            return $"{X} {Y}";
        }
    }
}
