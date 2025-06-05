namespace CDTlib.DataStructures
{
    public class Triangle
    {
        public Edge Edge { get; set; }

        public override string ToString()
        {
            int a = Edge.Origin.Index;
            int b = Edge.Next.Origin.Index;
            int c = Edge.Next.Next.Origin.Index;
            return $"{a} {b} {c}";
        }
    }
}
