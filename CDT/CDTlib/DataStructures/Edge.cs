namespace CDTlib.DataStructures
{
    public class Edge
    {
        public Edge(Node origin)
        {
            Origin = origin;    
        }

        public Node Origin { get; set; }
        public Edge Next { get; set; } = null!;
        public Edge? Twin { get; set; } = null;
        public Triangle Triangle { get; set; } = null!;
    }
}
