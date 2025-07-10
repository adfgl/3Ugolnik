using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TriSharp
{
    using static Triangle;

    public class Mesh
    {

        List<Vertex> _vertices = new List<Vertex>();
        readonly QuadTree _quadTree;
        List<Triangle> _triangles = new List<Triangle>();
        List<Circle> _circles = new List<Circle>();
        Rectangle _bounds;
        List<int> _affected;

        Vertex Insert(int triangle, int edge, Vertex vtx, double eps, int constraint)
        {
            List<int> visited = new List<int>();
            Span<Triangle> tris = stackalloc Triangle[4];
            int created;
            if (edge == NO_INDEX)
            {
                created = Split(_triangles, triangle, _vertices.Count, tris);
            }
            else
            {
                created = Split(_triangles, triangle, edge, _vertices.Count, tris, NO_INDEX, out _);
            }

            // do something before adding?

            vtx = AddVertex(vtx);
            AddTriangles(tris, created);
            Legalize(tris, created);
            return vtx;
        }

        public Vertex? Insert(Vertex vtx, double eps)
        {
            List<int> visited = new List<int>();
            (int t, int e, int v) = Find(vtx, visited, eps);

            if (t == NO_INDEX)
            {
                return null;
            }
            return Insert(t, e, vtx, eps, -1);
        }

        public Triangle EntranceTriangle(Vertex start, Vertex end)
        {
            Circler walker = new Circler(_triangles, start);
            do
            {
                Triangle t = walker.Current;
                Vertex a = _vertices[t.indxA];

                if (Vertex.Cross(a, _vertices[t.indxB], end) < 0)
                {
                    continue;
                }

                if (Vertex.Cross(_vertices[t.indxC], a, end) < 0)
                {
                    continue;
                }
                return t.Orient(t.IndexOf(start.Index));

            } while (walker.Next());

            throw new Exception("Could not find entrance triangle.");
        }

        public List<Constraint> Insert(int constraint, Vertex start, Vertex end, double eps, bool alwaysSplit)
        {
            List<Constraint> constraints = new List<Constraint>();

            Queue<(Vertex?, Vertex?)> toInsert = new Queue<(Vertex?, Vertex?)>();
            toInsert.Enqueue((Insert(start, eps), Insert(end, eps)));

            Span<Triangle> tris = stackalloc Triangle[4];
            int created;
            while (toInsert.Count > 0)
            {
                (Vertex? s, Vertex? e) = toInsert.Dequeue();
                if (s == null || e == null || Vertex.CloseOrEqual(s, e, eps))
                {
                    continue;
                }

                (int triangle, int edge) = FindEdge(s, e);
                if (edge != NO_INDEX)
                {
                    SetConstraint(triangle, edge, constraint);
                    constraints.Add(new Constraint(s, e, constraint));
                    continue;
                }

                while (true)
                {
                    Triangle t = EntranceTriangle(s, e);
                    Vertex a = _vertices[t.indxA];
                    Vertex b = _vertices[t.indxB];
                    Vertex c = _vertices[t.indxC];

                    if (Vertex.AreParallel(a, b, s, e, eps))
                    {
                        toInsert.Enqueue((a, b));
                        toInsert.Enqueue((b, e));
                        break;
                    }

                    if (Vertex.AreParallel(a, c, s, e, eps))
                    {
                        toInsert.Enqueue((a, c));
                        toInsert.Enqueue((c, e));
                        break;
                    }

                    Vertex? inter = Vertex.Intersect(b, c, s, e);
                    if (inter == null)
                    {
                        throw new Exception();
                    }

                    if (CanFlip(t, 1) && !alwaysSplit)
                    {
                        created = Flip(_triangles, t.index, 1, tris, constraint, out int opposite);

                        Vertex op = _vertices[opposite];
                        toInsert.Enqueue((s, op));
                        toInsert.Enqueue((op, e));
                    }
                    else
                    {
                        created = Split(_triangles, triangle, 1, _vertices.Count, tris, constraint, out int opposite);
                        inter = AddVertex(inter);

                        Vertex op = _vertices[opposite];
                        toInsert.Enqueue((s, inter));
                        toInsert.Enqueue((inter, op));
                        toInsert.Enqueue((op, e));
                    }

                    AddTriangles(tris, created);
                    Legalize(tris, created);
                    break;
                }
            }
            return constraints;
        }

        public void SetConstraint(int triangle, int edge, int type)
        {
            Triangle t = _triangles[triangle].Orient(edge);
            t.conAB = type;
            _triangles[triangle] = t;

            int adjIndex = t.adjAB;
            if (adjIndex == NO_INDEX) return;

            Triangle adj = _triangles[adjIndex];
            adj = adj.Orient(adj.IndexOf(t.indxB));
            adj.conAB = type;
            _triangles[adjIndex] = adj;
        }

        Vertex AddVertex(Vertex vtx)
        {
            if (!_bounds.Contains(vtx.X, vtx.Y))
            {
                throw new Exception("Out of bounds");
            }

            vtx.Index = _vertices.Count;
            _quadTree.Add(vtx);
            _vertices.Add(vtx);
            return vtx;
        }

        void AddTriangles(Span<Triangle> triangles, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Triangle t = triangles[i];
                Vertex a = _vertices[t.indxA];
                Vertex b = _vertices[t.indxB];
                Vertex c = _vertices[t.indxC];
                Circle circle = new Circle(a.X, a.Y, b.X, b.Y, c.X, c.Y);

                int index = t.index;
                if (index < _triangles.Count)
                {
                    Disconnect(_triangles[index]);
                    _triangles[index] = t;
                    _circles[index] = circle;
                }
                else
                {
                    _triangles.Add(t);
                    _circles.Add(circle);
                }
                Connect(_triangles[index]);
            }
        }

        void Connect(Triangle t)
        {
            int index = t.index;

            Vertex a = _vertices[t.indxA];
            Vertex b = _vertices[t.indxB];
            Vertex c = _vertices[t.indxC];

            if (a.Triangle == NO_INDEX) a.Triangle = index;
            if (b.Triangle == NO_INDEX) b.Triangle = index;
            if (c.Triangle == NO_INDEX) c.Triangle = index;

            if (t.adjAB != NO_INDEX)
            {
                Triangle adj = _triangles[t.adjAB];
                switch (adj.IndexOf(b.Index))
                {
                    case 0: adj.adjAB = index; break;
                    case 1: adj.adjBC = index; break;
                    case 2: adj.adjCA = index; break;
                    default:
                        throw new IndexOutOfRangeException();
                }
                _triangles[adj.index] = adj;
            }

            if (t.adjBC != NO_INDEX)
            {
                Triangle adj = _triangles[t.adjBC];
                switch (adj.IndexOf(c.Index))
                {
                    case 0: adj.adjAB = index; break;
                    case 1: adj.adjBC = index; break;
                    case 2: adj.adjCA = index; break;
                    default:
                        throw new IndexOutOfRangeException();
                }

                _triangles[adj.index] = adj;
            }

            if (t.adjCA != NO_INDEX)
            {
                Triangle adj = _triangles[t.adjCA];
                switch (adj.IndexOf(a.Index))
                {
                    case 0: adj.adjAB = index; break;
                    case 1: adj.adjBC = index; break;
                    case 2: adj.adjCA = index; break;
                    default:
                        throw new IndexOutOfRangeException();
                }

                _triangles[adj.index] = adj;
            }
        }

        Triangle Disconnect(Triangle t)
        {
            int index = t.index;

            Vertex a = _vertices[t.indxA];
            Vertex b = _vertices[t.indxB];
            Vertex c = _vertices[t.indxC];

            if (a.Triangle == index) a.Triangle = NO_INDEX;
            if (b.Triangle == index) b.Triangle = NO_INDEX;
            if (c.Triangle == index) c.Triangle = NO_INDEX;

            if (t.adjAB != NO_INDEX)
            {
                Triangle adj = _triangles[t.adjAB];
                switch (adj.IndexOf(b.Index))
                {
                    case 0: adj.adjAB = NO_INDEX; break;
                    case 1: adj.adjBC = NO_INDEX; break;
                    case 2: adj.adjCA = NO_INDEX; break;
                    default:
                        throw new IndexOutOfRangeException();
                }

                _triangles[adj.index] = adj;
                t.adjAB = NO_INDEX;
            }

            if (t.adjBC != NO_INDEX)
            {
                Triangle adj = _triangles[t.adjBC];
                switch (adj.IndexOf(c.Index))
                {
                    case 0: adj.adjAB = NO_INDEX; break;
                    case 1: adj.adjBC = NO_INDEX; break;
                    case 2: adj.adjCA = NO_INDEX; break;
                    default:
                        throw new IndexOutOfRangeException();
                }

                _triangles[adj.index] = adj;
                t.adjBC = NO_INDEX;
            }

            if (t.adjCA != NO_INDEX)
            {
                Triangle adj = _triangles[t.adjCA];
                switch (adj.IndexOf(a.Index))
                {
                    case 0: adj.adjAB = NO_INDEX; break;
                    case 1: adj.adjBC = NO_INDEX; break;
                    case 2: adj.adjCA = NO_INDEX; break;
                    default:
                        throw new IndexOutOfRangeException();
                }

                _triangles[adj.index] = adj;
                t.adjCA = NO_INDEX;
            }
            return t;
        }

        public (int t, int e, int v) Find(Vertex vtx, List<int> path, double eps, int searchStart = -1)
        {
            if (_triangles.Count == 0)
                return (NO_INDEX, NO_INDEX, NO_INDEX);

            double x = vtx.X, y = vtx.Y;
            int maxSteps = _triangles.Count * 3;
            int trianglesChecked = 0;

            int current = searchStart == NO_INDEX ? _triangles.Count - 1 : searchStart;

            int bestExitStart = -1, bestExitEnd = -1;
            while (true)
            {
                if (trianglesChecked++ > maxSteps)
                {
#if DEBUG
                    throw new Exception("FindContaining exceeded max steps. Likely invalid topology.");
#endif
                    return (NO_INDEX, NO_INDEX, NO_INDEX);
                  
                }

                path.Add(current);

                Triangle t = _triangles[current];
                Span<int> inds = [t.indxA, t.indxB, t.indxC];
                Span<int> adjs = [t.adjAB, t.adjBC, t.adjCA];

                int bestExit = NO_INDEX;
                double worstCross = double.MaxValue;
                bool inside = true;
                for (int edge = 0; edge < 3; edge++)
                {
                    int i0 = inds[edge];
                    int i1 = inds[(edge + 1) % 3];
                    if (i0 == bestExitEnd && i1 == bestExitStart)
                    {
                        continue;
                    }

                    Vertex v0 = _vertices[i0];
                    Vertex v1 = _vertices[i1];
                    if (Vertex.CloseOrEqual(v0, vtx, eps))
                        return (current, NO_INDEX, i0);

                    double cross = Vertex.Cross(v0, v1, vtx);
                    if (Math.Abs(cross) <= eps &&
                        x >= Math.Min(v0.X, v1.X) - eps && x <= Math.Max(v0.X, v1.X) + eps &&
                        y >= Math.Min(v0.Y, v1.Y) - eps && y <= Math.Max(v0.Y, v1.Y) + eps)
                        return (current, edge, NO_INDEX);

                    if (cross <= 0)
                    {
                        inside = false;
                        if (cross < worstCross)
                        {
                            worstCross = cross;
                            bestExit = adjs[edge];
                            bestExitStart = i0;
                            bestExitEnd = i1;
                        }
                    }
                }

                if (inside)
                    return (current, NO_INDEX, NO_INDEX);

                current = bestExit;
            }
        }


        public (int triangle, int edge) FindEdgeBrute(Vertex a, Vertex b)
        {
            int ai = a.Index;
            int bi = b.Index;
            foreach (Triangle t in _triangles)
            {
                int edge = t.EdgeIndex(ai, bi);
                if (edge != NO_INDEX)
                {
                    return (t.index, edge);
                }
            }
            return (NO_INDEX, NO_INDEX);
        }

        public (int triangle, int edge) FindEdge(Vertex a, Vertex b)
        {
            Circler walker = new Circler(_triangles, a);

            int ai = a.Index;
            int bi = b.Index;
            do
            {
                Triangle t = walker.Current;
                if ((t.indxA == ai && t.indxB == bi) || (t.indxA == bi && t.indxB == ai))
                {
                    return (t.index, 0);
                }

                if ((t.indxB == ai && t.indxC == bi) || (t.indxB == bi && t.indxC == ai))
                {
                    return (t.index, 1);
                }

                if ((t.indxC == ai && t.indxA == bi) || (t.indxC == bi && t.indxA == ai))
                {
                    return (t.index, 2);
                }
            }
            while (walker.Next());

            return (NO_INDEX, NO_INDEX);
        }

        public bool CanFlip(Triangle t, int edge)
        {
            /*
                          c           
                          /\          
                         /  \         
                        /    \        
                       /      \       
                      /   t0   \      
                     /          \     
                    /            \     
                 a +--------------+ b 
                    \            /    
                     \          /     
                      \   t1   /      
                       \      /       
                        \    /        
                         \  /         
                          \/          
                           d          
            */

            t = t.Orient(edge);

            int adjIndex = t.adjAB;
            if (adjIndex == NO_INDEX || t.conAB != NO_INDEX)
            {
                return false;
            }

            Triangle adj = _triangles[adjIndex];
            int twin = adj.EdgeIndex(t.indxB, t.indxA);
            return Vertex.QuadConvex(
                _vertices[t.indxA], 
                _vertices[adj.Orient(twin).indxC], 
                _vertices[t.indxB],
                _vertices[t.indxC]);
        }

        public bool ShouldFlipCirclePrecalculated(Triangle t, int edge)
        {
            t = t.Orient(edge);

            Triangle adj = _triangles[t.adjAB];
            int twin = adj.EdgeIndex(t.indxB, t.indxA);
            adj = adj.Orient(twin);

            (double x0, double y0) = _vertices[adj.indxC];

            return _circles[t.index].Contains(x0, y0);
        }

        public bool ShouldFlipCircle(Triangle t, int edge)
        {
            t = t.Orient(edge);

            Triangle adj = _triangles[t.adjAB];
            int twin = adj.EdgeIndex(t.indxB, t.indxA);
            Vertex a = _vertices[t.indxA];
            Vertex b = _vertices[t.indxB];
            Vertex c = _vertices[t.indxC];
            Vertex d = _vertices[adj.Orient(twin).indxC];

            double adx = a.X - d.X, ady = a.Y - d.Y;
            double bdx = b.X - d.X, bdy = b.Y - d.Y;
            double cdx = c.X - d.X, cdy = c.Y - d.Y;

            double abdet = adx * bdy - bdx * ady;
            double bcdet = bdx * cdy - cdx * bdy;
            double cadet = cdx * ady - adx * cdy;

            double alift = adx * adx + ady * ady;
            double blift = bdx * bdx + bdy * bdy;
            double clift = cdx * cdx + cdy * cdy;

            return (alift * bcdet + blift * cadet + clift * abdet) > 0;
        }

        public bool ShouldFlipAngles(Triangle t, int edge)
        {
            t = t.Orient(edge);

            Triangle adj = _triangles[t.adjAB];
            int twin = adj.EdgeIndex(t.indxB, t.indxA);
            (double x1, double y1) = _vertices[t.indxA];
            (double x2, double y2) = _vertices[t.indxC];
            (double x3, double y3) = _vertices[t.indxB];
            (double x0, double y0) = _vertices[adj.Orient(twin).indxC];

            double sAlpha = (x0 - x1) * (x0 - x3) + (y0 - y1) * (y0 - y3);
            if (sAlpha < 0 && (x2 - x1) * (x2 - x3) + (y2 - y1) * (y2 - y3) < 0)
            {
                return true;
            }

            return
                ((x0 - x1) * (y0 - y3) - (x0 - x3) * (y0 - y1)) *
                ((x2 - x3) * (x2 - x1) + (y2 - y3) * (y2 - y1)) +
                (sAlpha *
                ((x2 - x3) * (y2 - y1) - (x2 - x1) * (y2 - y3))) < 0;
        }

        void Legalize(Span<Triangle> triangles, int count)
        {
            _affected.Clear();

            Stack<Triangle> toLegalize = new Stack<Triangle>();
            for (int i = 0; i < count; i++)
            {
                toLegalize.Push(triangles[i]);
            }

            while (toLegalize.Count > 0)
            {
                Triangle t = toLegalize.Pop();
                _affected.Add(t.index);

                int edge = 0;
                if (!CanFlip(t, edge) ||
                    !ShouldFlipCirclePrecalculated(t, edge))
                {
                    continue;
                }

                count = Triangle.Flip(_triangles, t.index, edge, triangles, NO_INDEX, out _);
                for (int i = 0; i < count; i++)
                {
                    toLegalize.Push(triangles[i]);
                }
            }
        }
    }
}
