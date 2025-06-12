namespace CDTlib
{
    using System.Collections;
    using System.Collections.Generic;

    public class Face : ISplittable, IEnumerable<Edge>
    {
        public Face(int index, Edge edge)
        {
            Index = index;
            Edge = edge;
        }

        public Face(int index, Node a, Node b, Node c)
        {
            Index = index;

            Edge ab = new Edge(a); 
            Edge bc = new Edge(b); 
            Edge ca = new Edge(c); 

            Edge = ab;

            a.Edge = ab;
            b.Edge = bc;
            c.Edge = ca;

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
            Edge[] edges = [Edge, Edge.Next, Edge.Prev];

            Edge[] affected = new Edge[3];
            Face[] faces = new Face[3];
            Face prev = null!;
            for (int i = 0; i < 3; i++)
            {
                bool first = i == 0;
                Edge edge = edges[i];
                Face face = new Face(first ? Index : -1, edge, node);

                face.Edge.Constrained = edge.Constrained;
                face.Edge.Twin = edge.Twin;
                if (!first)
                {
                    face.Edge.Prev.SetTwin(prev.Edge.Next);
                }
                prev = face;

                faces[i] = face;
                affected[i] = face.Edge;
            }

            faces[0].Edge.Prev.SetTwin(prev.Edge.Next);

            return new SplitResult()
            {
                Affected = affected,
                NewFaces = faces,
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
