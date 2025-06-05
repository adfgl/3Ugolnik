namespace CDTlib.DataStructures
{
    using System.Collections;
    using System.Collections.Generic;

    public class Triangle : IEnumerable<Edge>
    {
        public Triangle(Edge edge)
        {
            Edge = edge;
        }

        public Edge Edge { get; set; }
        public double Area { get; set; }    

        public override string ToString()
        {
            int a = Edge.Origin.Index;
            int b = Edge.Next.Origin.Index;
            int c = Edge.Next.Next.Origin.Index;
            return $"{a} {b} {c} ({Area})";
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
