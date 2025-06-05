namespace CDTlib.DataStructures
{
    public class Edge
    {
        public Node Origin { get; set; } = null!;
        public Edge Next { get; set; } = null!;
        public Edge? Twin { get; set; } = null;
        public Triangle Triangle { get; set; } = null!;
    }
}
