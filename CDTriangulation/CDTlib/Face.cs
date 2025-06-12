namespace CDTlib
{
    using System.Collections;
    using System.Collections.Generic;

    public class Face : IEnumerable<Edge>
    {
        public Face(int index, Edge edge)
        {
            Index = index;
            Edge = edge;
        }

        public void Deconstruct(out Node a, out Node b, out Node c)
        {
            a = Edge.Origin;
            b = Edge.Next.Origin;
            c = Edge.Prev.Origin;
        }

        public int Index { get; set; }
        public Edge Edge { get; set; }

        public IEnumerator<Edge> GetEnumerator()
        {
            yield return Edge;
            yield return Edge.Next;
            yield return Edge.Prev;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            var (a, b, c) = this;
            return $"({Index}) {a.Index} {b.Index} {c.Index}";
        }
    }
}
