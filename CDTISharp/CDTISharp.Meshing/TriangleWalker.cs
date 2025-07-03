namespace CDTISharp.Meshing
{
    public struct TriangleWalker
    {
        readonly List<Triangle> _triangles;
        readonly int _start, _vertex;
        int _current, _edge0, _edge1;

        public TriangleWalker(List<Triangle> triangles, int triangleIndex, int globalVertexIndex)
        {
            _triangles = triangles;
            _vertex = globalVertexIndex;
            _current = _start = triangleIndex;

            _edge0 = _triangles[_current].IndexOf(_vertex);
            _edge1 = Mesh.PREV[_edge0];
        }

        public int Current => _current;
        public int Edge0 => _edge0;
        public int Edge1 => _edge1;

        public bool MoveNext()
        {
            Triangle tri = _triangles[_current];
            int next = tri.adjacent[_edge0];
            if (next == _start || next == -1)
            {
                return false;
            }

            _current = next;
            Triangle nextTri = _triangles[_current];
            _edge0 = nextTri.IndexOf(_vertex);
            _edge1 = Mesh.PREV[_edge0];
            return true;
        }
    }
}
