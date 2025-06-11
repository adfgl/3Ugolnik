namespace CDTlib.DataStructures
{
    public class Edge
    {
        public Edge(Node origin)
        {
            Origin = origin;    
        }

        public bool Constrained { get; set; } = false;
        public Node Origin { get; set; }
        public Edge Next { get; set; } = null!;
        public Edge Prev { get; set; } = null!;
        public Edge? Twin { get; set; } = null;
        public Triangle Triangle { get; set; } = null!;

        public EOrientation Orient(double x, double y)
        {
            return GeometryHelper.ClassifyPoint(Origin.X, Origin.Y, Next.Origin.X, Next.Origin.Y, x, y);
        }

        public bool Contains(Node node)
        {
            return Origin == node || Next.Origin == node;
        }

        public override string ToString()
        {
            return $"{Origin.Index} {Next.Origin.Index}";
        }
    }
}
