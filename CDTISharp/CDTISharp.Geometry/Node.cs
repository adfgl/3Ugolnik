namespace CDTISharp.Geometry
{
    public class Node
    {
        public Node()
        {
            
        }

        public Node(int index, double x, double y, double z = 0)
        {
            Index = index;  
            X = x;
            Y = y;
            Z = z;
        }

        public void Deconstruct(out double x, out double y)
        {
            x = X; y = Y;
        }

        public int Index { get; set; } = -1;
        public int Triangle { get; set; } = -1;

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public static Node Between(Node a, Node b)
        {
            return new Node(-1, (a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5, (a.Z + b.Z) * 0.5);
        }

        public static Node Average(params Node[] nodes)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            int n = nodes.Length;
            for (int i = 0; i < n; i++)
            {
                Node node = nodes[i];
                x += node.X;
                y += node.Y;
                z += node.Z;
            }
            x /= n;
            y /= n;
            z /= n;

            return new Node()
            {
                X = x,
                Y = y,
                Z = z
            };
        }

        public override string ToString()
        {
            return $"[{Index}] {X} {Y}";
        }
    }
}
