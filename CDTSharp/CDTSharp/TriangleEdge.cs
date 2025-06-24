namespace CDTSharp
{
    public readonly struct TriangleEdge
    {
        public readonly int triangle, edge;

        public TriangleEdge(int triangle = -1, int edge = -1)
        {
            this.triangle = triangle;
            this.edge = edge;
        }

        public void Deconstruct(out int triangle, out int edge)
        {
            triangle = this.triangle;
            edge = this.edge;
        }

        public override string ToString()
        {
            return $"{triangle} {edge}";
        }
    }
}
