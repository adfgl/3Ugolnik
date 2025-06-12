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

        public Face ComputeArea()
        {
            var (a, b, c) = this;
            Area = Node.Cross(a, b, c) * 0.5;

            if (Area < 0)
            {
                throw new Exception("Incorrect winding order!");
            }
            return this;
        }

        public TopologyChange Split(Node node)
        {
            /*
                        c            
                        /\           
                       /  \          
                      /    \         
                     /      \        
                    /  f0    \       
                   /          \      
                  /            \     
               a +--------------+ b  
                                     
            */

            var (a, b, c) = this;
            Edge ab = Edge;
            Edge bc = ab.Next;
            Edge ca = bc.Next;

            Face new0 = new Face(Index, node, a, b).ComputeArea();
            Face new1 = new Face(-1, node, b, c).ComputeArea();
            Face new2 = new Face(-1, node, c, a)
            {
                Area = Area - new0.Area - new1.Area
            };

            // twins
            new0.Edge.Twin = new2.Edge.Prev;
            new0.Edge.Next.Twin = ab.Twin;
            new0.Edge.Prev.Twin = new1.Edge;

            new1.Edge.Twin = new0.Edge.Prev;
            new1.Edge.Next.Twin = bc.Twin;
            new1.Edge.Prev.Twin = new2.Edge;

            new2.Edge.Twin = new1.Edge.Prev;
            new2.Edge.Next.Twin = ca.Twin;
            new2.Edge.Prev.Twin = new0.Edge;

            // constraints
            new0.Edge.Next.Constrained = ab.Constrained;
            new1.Edge.Next.Constrained = bc.Constrained;
            new2.Edge.Next.Constrained = ca.Constrained;

            return new TopologyChange()
            {
                AffectedEdges =[new0.Edge, new1.Edge, new2.Edge],
                NewFaces = [new0, new1, new2],
                OldFaces = [this]
            };
        }

        public void Center(out double x, out double y)
        {
            var (a, b, c) = this;
            x = (a.X + b.X + c.X) / 3.0;
            y = (a.Y + b.Y + c.Y) / 3.0;
        }

        public override string ToString()
        {
            var (a, b, c) = this;
            return $"({Index}) {a.Index} {b.Index} {c.Index}";
        }
    }
}
