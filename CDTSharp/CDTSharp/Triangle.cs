namespace CDTSharp
{
  
    public readonly struct Triangle
    {
        public readonly int index;
        public readonly int[] indices, adjacent;
        public readonly bool[] constrained;
        public readonly Circle circle;
        public readonly double area;
        public readonly List<int> parents;

        public Triangle(int index, Node a, Node b, Node c, double? area = null, IEnumerable<int>? parents = null)
        {
            this.index = index;
            this.indices = [a.Index, b.Index, c.Index];
            this.adjacent = [-1, -1, -1];
            this.constrained = [false, false, false];
            this.circle = new Circle(a.X, a.Y, b.X, b.Y, c.X, c.Y);
            this.area = area is null ? GeometryHelper.Cross(a, b, c) * 0.5 : area.Value;
            this.parents = parents != null ? new List<int>(parents) : new List<int>();

            if (this.area <= 0)
            {
                if (this.area == 0)
                {
                    throw new Exception($"Degenerate triangle: {this}");
                }
                throw new Exception($"Wrong winding order. Expected CCW: {this}");
            }
        }

        public Triangle(int index, Node a, Node b, Node c)
             : this(index, a, b, c, GeometryHelper.Cross(a, b, c) * 0.5, null)
        {

        }

        public void Edge(int edge, out int start, out int end)
        {
            start = indices[edge];
            end = indices[Mesh.NEXT[edge]];
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

        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < 3; i++)
            {
                s += adjacent[i] + " " + constrained[i] + (i != 2 ? ", " : "");
            }
            return $"{String.Join(' ', indices.Select(i => i))} ({s})";
        }
    }
}
