namespace CDTISharp.Meshing
{
    public readonly struct Triangle
    {

        public readonly int index;
        public readonly int[] indices, adjacent, constraints;
        public readonly Circle circle;

        public Triangle(int index, Node a, Node b, Node c)
        {
            this.index = index;
            this.indices = [a.Index, b.Index, c.Index];
            this.adjacent = [-1, -1, -1];
            this.constraints = [-1, -1, -1];

            this.circle = new Circle(a.X, a.Y, b.X, b.Y, c.X, c.Y);
        }

        public int IndexOf(int vertex)
        {
            for (int i = 0; i < 3; i++)
            {
                if (indices[i] == vertex)
                {
                    return i;
                }
            }
            return -1;
        }

        public int IndexOf(int from, int to)
        {
            for (int i = 0; i < 3; i++)
            {
                if (indices[i] == from && indices[Mesh.NEXT[i]] == to)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
