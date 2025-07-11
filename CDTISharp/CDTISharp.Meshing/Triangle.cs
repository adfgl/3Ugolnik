﻿using CDTISharp.Geometry;

namespace CDTISharp.Meshing
{
    public struct Triangle
    {
        public int index;
        public readonly bool super;
        public readonly int[] indices, adjacent, constraints;
        public readonly Circle circle;
        public bool discard = false;


        public Triangle(int index, Node a, Node b, Node c)
        {
            this.index = index;
            this.indices = [a.Index, b.Index, c.Index];
            this.adjacent = [-1, -1, -1];
            this.constraints = [-1, -1, -1];
            this.circle = new Circle(a.X, a.Y, b.X, b.Y, c.X, c.Y);
            this.super = a.Index < 3 || b.Index < 3 || c.Index < 3;
        }

        public void Edge(int index, out int start, out int end)
        {
            start = indices[index];
            end = indices[Mesh.NEXT[index]];
        }

        public int IndexOf(int vertex)
        {
            for (int i = 0; i < 3; i++)
            {
                if (indices[i] == vertex)
                {
                    return i;
                }
            }
            return -1;
        }

        public int IndexOf(int from, int to)
        {
            for (int i = 0; i < 3; i++)
            {
                if (indices[i] == from && indices[Mesh.NEXT[i]] == to)
                {
                    return i;
                }
            }
            return -1;
        }

        public override string ToString()
        {
            return $"[{index}] {String.Join(" ", indices)}";
        }
    }
}
