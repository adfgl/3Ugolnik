namespace CDTISharp.Meshing
{
    public class Node
    {
        public Node()
        {
            
        }

        public Node(int index, double x, double y)
        {
            Index = index;  
            X = x;
            Y = y;
        }

        public int Index { get; set; } = -1;
        public int Triangle { get; set; } = -1;

        public double X { get; set; }
        public double Y { get; set; }
    }
}
