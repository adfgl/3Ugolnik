namespace CDTSharp
{
    public class HeTriangle
    {
        public HeTriangle(HeNode a, HeNode b, HeNode c, double? area = null, IEnumerable<int>? parents = null)
        {
            HeEdge ab = new HeEdge(a);
            HeEdge bc = new HeEdge(b);
            HeEdge ca = new HeEdge(c);

            a.Edge = ab;
            b.Edge = bc;
            c.Edge = ca;

            Edge = ab;
            ab.Triangle = bc.Triangle = ca.Triangle = this;

            ab.Next = bc;
            bc.Next = ca;
            ca.Next = ab;

            Edges[0] = ab;
            Edges[1] = bc;
            Edges[2] = ca;

            Circle = new Circle(a.X, a.Y, b.X, b.Y, c.X, c.Y);
            Area = area.HasValue ? area.Value : GeometryHelper.Cross(a.X, a.Y, b.X, b.Y, c.X, c.Y) * 0.5;
            Parents = parents != null ? new List<int>(parents) : new List<int>();
        }

        public HeEdge Edge { get; set; }
        public HeEdge[] Edges { get; set; } = new HeEdge[3];
        public Circle Circle { get; }
        public double Area { get; }
        public List<int> Parents { get; }
    }
}
