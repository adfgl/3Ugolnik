namespace CDTlib
{
    public class Face
    {
        public Face(int index, Edge edge)
        {
            Index = index;
            Edge = edge;
        }

        public int Index { get; set; }
        public Edge Edge { get; set; } = null!;
    }
}
