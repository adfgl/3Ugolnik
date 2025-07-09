namespace TriSharp
{
    public class Vertex
    {
        public Vertex()
        {

        }

        public Vertex(int index, double x, double y, double z = 0)
        {
            Index = index;
            X = x;
            Y = y;
            Z = z;
        }

        public int Index { get; set; } = -1;
        public int Triangle { get; set; } = -1;

        public double X { get; set; }   
        public double Y { get; set; }
        public double Z { get; set; }

        public override string ToString()
        {
            return $"[{Index}] {X} {Y}";
        }
    }
}
