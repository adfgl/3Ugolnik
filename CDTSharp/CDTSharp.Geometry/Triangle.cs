﻿using System.Diagnostics;

namespace CDTSharp.Geometry
{
    public class Triangle
    {
        public Triangle(int index, Node a, Node b, Node c)
        {
            Index = index;

            Edge ab = new Edge(a);
            Edge bc = new Edge(b);
            Edge ca = new Edge(c);

            if (GeometryHelper.Cross(a, b, c.X, c.Y) < 0)
            {
                throw new Exception();
            }

            if (a.Edge == null) a.Edge = ab;
            if (b.Edge == null) b.Edge = bc;
            if (c.Edge == null) c.Edge = ca;

            Edge = ab;
            ab.Triangle = bc.Triangle = ca.Triangle = this;

            ab.Next = bc;
            bc.Next = ca;
            ca.Next = ab;

            ab.Prev = ca;
            bc.Prev = ab;
            ca.Prev = bc;

            Circle = new Circle(a.X, a.Y, b.X, b.Y, c.X, c.Y);
        }

        public void Nodes(out Node a, out Node b, out Node c)
        {
            a = Edge.Origin;
            b = Edge.Next.Origin;
            c = Edge.Prev.Origin;
        }

        public void Edges(out Edge ab, out Edge bc, out Edge ca)
        {
            ab = Edge;
            bc = ab.Next;
            ca = bc.Next;
        }

        public int Index { get; set; }
        public Edge Edge { get; set; }
        public Circle Circle { get; set; }

        public bool Removed { get; set; } = false;
        public bool Hole { get; set; } = false;

        public bool ContainsSuper()
        {
            return Edge.Origin.Index < 0 || Edge.Next.Origin.Index < 0 || Edge.Next.Next.Origin.Index < 0;
        }

        public double Area()
        {
            Nodes(out Node a, out Node b, out Node c);
            double area = GeometryHelper.Cross(a, b, c.X, c.Y) * 0.5;
            return area;
        }

        public void Center(out double x, out double y)
        {
            Nodes(out Node a, out Node b, out Node c);
            x = (a.X + b.X + c.X) / 3.0;
            y = (a.Y + b.Y + c.Y) / 3.0;
        }

        public IEnumerable<Edge> Forward()
        {
            Edge he = Edge;
            Edge current = he;
            do
            {
                yield return current;
                current = current.Next;
            } while (current != he);
        }

        public IEnumerable<Edge> Backward()
        {
            Edge he = Edge;
            Edge current = he;
            do
            {
                yield return current;
                current = current.Prev;
            } while (current != he);
        }

        public override string ToString()
        {
            Nodes(out var a, out var b, out var c);
            return $"[{Index}] ({a}), ({b}), ({c})";
        }
    }
}
