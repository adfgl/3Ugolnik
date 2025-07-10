using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TriSharp
{
    public struct TriangleWalker
    {
        readonly List<Triangle> _triangles;
        readonly int _start, _vertex;
        Triangle _current;

        public TriangleWalker(List<Triangle> triangles, int triangleIndex, int globalVertexIndex)
        {
            _triangles = triangles;
            _vertex = globalVertexIndex;
            _start = triangleIndex;

            Triangle t = triangles[triangleIndex];
            _current = OrientTriangle(ref t, globalVertexIndex);
        }

        public Triangle Current => _current;
        public int Vertex => _vertex;
        public int Start => _start;

        public bool Next()
        {
            int next = _current.adjAB;
            if (next == _start || next == -1)
            {
                return false;
            }
            Triangle t = _triangles[next];
            _current = OrientTriangle(ref t, _vertex);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Triangle OrientTriangle(ref Triangle t, int vertex)
        {
            int edge = t.IndexOf(vertex);
            if (edge == 0)
            {
                return t;
            }
               
            if (edge == 1)
            {
                return new Triangle(t.index, t.indxB, t.indxC, t.indxA, t.adjBC, t.adjCA, t.adjAB, t.conBC, t.conCA, t.conAB);
            }
               
            if (edge == 2)
            {
                return new Triangle(t.index, t.indxC, t.indxA, t.indxB, t.adjCA, t.adjAB, t.adjBC, t.conCA, t.conAB, t.conBC);
            }
            throw new ArgumentException("Vertex not found in triangle.");
        }

        public static List<Triangle> GetTriangles(List<Triangle> triangles, Vertex point, List<Triangle> output)
        {
            TriangleWalker walker = new TriangleWalker(triangles, point.Triangle, point.Index);
            do
            {
                output.Add(walker.Current);
            }
            while (walker.Next());
            return output;
        }


    }
}
