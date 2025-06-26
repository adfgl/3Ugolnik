using CDTGeometryLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public class HeTriangle
    {
        public HeTriangle(int index, HeNode a, HeNode b, HeNode c)
        {
            Index = index;

            HeEdge ab = new HeEdge(a);
            HeEdge bc = new HeEdge(b);
            HeEdge ca = new HeEdge(c);

            a.Edge = ab;
            b.Edge = bc;
            c.Edge = ca;

            Edge = ab;
            ab.Triangle = bc.Triangle = ca.Triangle = this;

            ab.Next = bc;
            bc.Next = ca;
            ca.Next = ab;

            Circle = new Circle(a.X, a.Y, b.X, b.Y, c.X, c.Y);
        }

        public void Nodes(out HeNode a, out HeNode b, out HeNode c)
        {
            a = Edge.Origin;
            b = Edge.Next.Origin;
            c = Edge.Next.Next.Origin;
        }

        public void Edges(out HeEdge ab, out HeEdge bc, out HeEdge ca)
        {
            ab = Edge;
            bc = ab.Next;
            ca = bc.Next;
        }

        public int Index { get; }
        public HeEdge Edge { get; set; }
        public Circle Circle { get; set; }
        public bool Dead { get; set; } = false;

        public double Area()
        {
            Nodes(out HeNode a, out HeNode b, out HeNode c);
            double area = GeometryHelper.Cross(a, b, c.X, c.Y) * 0.5;
            return area;
        }

        public IEnumerable<HeEdge> Forward()
        {
            HeEdge he = Edge;
            HeEdge current = he;
            do
            {
                yield return current;
                current = current.Next;
            } while (current != he);
        }

        public IEnumerable<HeEdge> Backward()
        {
            HeEdge he = Edge;
            HeEdge current = he;
            do
            {
                yield return current;
                current = current.Prev;
            } while (current != he);
        }
    }
}
