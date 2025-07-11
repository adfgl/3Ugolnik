﻿namespace CDTSharp.Geometry
{
    public class Node
    {
        public Node()
        {
            
        }

        public Node(int index, double x, double y)
        {
            Index = index;
            X = x; Y = y;
        }

        public int Index { get; set; } = -1;
        public double X { get; set; }
        public double Y { get; set; }
        public Edge Edge { get; set; } = null!;

        public void Deconstruct(out double x, out double y)
        {
            x = X; y = Y;
        }

        public static Node Average(params Node[] nodes)
        {
            double x = 0;
            double y = 0;
            foreach (Node node in nodes)
            {
                x += node.X;
                y += node.Y;
            }
            x /= nodes.Length;
            y /= nodes.Length;

            return new Node()
            {
                X = x,
                Y = y,
            };
        }

        public bool Close(double x, double y, double eps = 1e-6)
        {
            double dx = x - X;
            double dy = y - Y;
            return dx * dx + dy * dy < eps;
        }

        public IEnumerable<Edge> Around()
        {
            Edge start = Edge;
            Edge current = Edge;
            do
            {
                yield return current;
                current = current.Prev.Twin!;
            } while (current != null && current != start);
        }

        public override string ToString()
        {
            return $"[{Index}] {X} {Y}";
        }
    }
}
