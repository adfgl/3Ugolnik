namespace CDTSharp
{
    public class HeTriangle
    {
        public HeTriangle(int index, HeNode a, HeNode b, HeNode c, double? area = null)
        {
            Index = index;

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

            Circle = new Circle(a.X, a.Y, b.X, b.Y, c.X, c.Y);
            Area = area.HasValue ? area.Value : GeometryHelper.Cross(a.X, a.Y, b.X, b.Y, c.X, c.Y) * 0.5;
        }

        public int Index { get; }
        public HeEdge Edge { get; set; }
        public Circle Circle { get; }
        public double Area { get; }
  
    }
}
