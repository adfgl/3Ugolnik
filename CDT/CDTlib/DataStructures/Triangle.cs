namespace CDTlib.DataStructures
{
    using System.Collections;
    using System.Collections.Generic;

    public class Triangle : IEnumerable<Edge>
    {
        public Triangle(int index, Edge edge)
        {
            Index = index;
            Edge = edge;
        }

        public int Index { get; set; }
        public Edge Edge { get; set; }
        public double Area { get; set; }
        public bool Deleted { get; set; } = false;

        public void Center(out double x, out double y)
        {
            Node a = Edge.Origin;
            Node b = Edge.Next.Origin;
            Node c = Edge.Prev.Origin;

            x = (a.X + b.X + c.X) / 3.0;
            y = (a.Y + b.Y + c.Y) / 3.0;
        }

        public override string ToString()
        {
            int a = Edge.Origin.Index;
            int b = Edge.Next.Origin.Index;
            int c = Edge.Next.Next.Origin.Index;
            return $"({Index}) {a} {b} {c} [{Area}]";
        }

        public IEnumerator<Edge> GetEnumerator()
        {
            yield return Edge;
            yield return Edge.Next;
            yield return Edge.Next.Next;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
