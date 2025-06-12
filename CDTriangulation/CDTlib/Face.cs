namespace CDTlib
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class Face : ISplittable, IEnumerable<Edge>
    {
        public Face(int index, Edge edge)
        {
            Index = index;
            Edge = edge;
        }

        public Face(int index, Node a, Node b, Node c)
        {
            Debug.Assert(Node.Cross(a, b, c) > 0, "Incorrect winding order.");

            Index = index;

            Edge ab = new Edge(a); 
            Edge bc = new Edge(b); 
            Edge ca = new Edge(c); 

            Edge = ab;

            ab.Next = bc;
            bc.Next = ca;
            ca.Next = ab;

            ab.Prev = ca;
            bc.Prev = ab;
            ca.Prev = bc;

            ab.Face = bc.Face = ca.Face = this;
        }

        public Face(int index, Edge e, Node n)
            : this(index, e.Origin, e.Next.Origin, n)
        {
            
        }

        public void Deconstruct(out Node a, out Node b, out Node c)
        {
            a = Edge.Origin;
            b = Edge.Next.Origin;
            c = Edge.Prev.Origin;
        }

        public int Index { get; set; }
        public Edge Edge { get; set; }
        public bool Dead { get; set; } = false;
        public double Area { get; set; }

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

        public SplitResult Split(Node node)
        {
            var (a, b, c) = this;
            Edge ab = Edge;
            Edge bc = ab.Next;
            Edge ca = bc.Next;

            a.Edge = ab;
            b.Edge = bc;
            c.Edge = ca;

            Face new0 = new Face(Index, node, a, b);
            Face new1 = new Face(-1, node, b, c);
            Face new2 = new Face(-1, node, c, a);

            new0.Edge.Twin = new2.Edge.Prev;
            new0.Edge.Next.Twin = ab;
            new0.Edge.Prev.Twin = new1.Edge;
            new0.Edge.Next.Constrained = ab.Constrained;

            new1.Edge.Twin = new0.Edge.Prev;
            new1.Edge.Next.Twin = bc;
            new1.Edge.Prev.Twin = new2.Edge;
            new1.Edge.Next.Constrained = bc.Constrained;

            new2.Edge.Twin = new1.Edge.Prev;
            new2.Edge.Next.Twin = ca;
            new2.Edge.Prev.Twin = new0.Edge;
            new2.Edge.Next.Constrained = ca.Constrained;

            return new SplitResult()
            {
                AffectedEdges =[new0.Edge, new1.Edge, new2.Edge],
                NewFaces = [new0, new1, new2],
                OldFaces = [this]
            };
        }

        public override string ToString()
        {
            var (a, b, c) = this;
            return $"({Index}) {a.Index} {b.Index} {c.Index}";
        }
    }
}
