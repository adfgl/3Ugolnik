using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TriSharp
{
    using static Triangle;

    public class Mesh
    {
        List<Vertex> _vertices = new List<Vertex>();
        List<Triangle> _triangles = new List<Triangle>();
        List<Circle> _circles = new List<Circle>();

        public (int triangle, int edge, int node) Find(Vertex vtx, List<int> path, double eps, int searchStart = -1)
        {
            if (_triangles.Count == 0)
            {
                return (NO_INDEX, NO_INDEX, NO_INDEX);
            }

            double x = vtx.X;
            double y = vtx.Y;

            int maxSteps = _triangles.Count * 3;
            int trianglesChecked = 0;

            int current = searchStart == NO_INDEX ? _triangles.Count - 1 : searchStart;

            Vertex start, end;
            double cross;
            while (true)
            {
                if (trianglesChecked++ > maxSteps)
                {
                    throw new Exception("FindContaining exceeded max steps. Likely invalid topology.");
                }

                path.Add(current);
                Triangle t = _triangles[current];

                int bestExit = NO_INDEX;
                double worstCross = double.MaxValue;
                bool inside = true;

                start = _vertices[t.indxA];
                end = _vertices[t.indxB];

                // a -> b
                if (Vertex.CloseOrEqual(start, vtx, eps))
                {
                    return (current, 0, start.Index);
                }

                cross = Vertex.Cross(start, end, vtx);
                if (Math.Abs(cross) <= eps &&
                    x >= Math.Min(start.X, end.X) - eps && x <= Math.Max(start.X, end.X) + eps &&
                    y >= Math.Min(start.Y, end.Y) - eps && y <= Math.Max(start.Y, end.Y) + eps)
                {
                    return (current, 0, NO_INDEX);
                }
                    
                if (cross <= 0) 
                {
                    inside = false;
                    if (bestExit == NO_INDEX || cross < worstCross)
                    {
                        worstCross = cross;
                        bestExit = t.adjAB;
                    }
                }

                // b -> c
                start = _vertices[t.indxB];
                end = _vertices[t.indxC];

                if (Vertex.CloseOrEqual(start, vtx, eps))
                {
                    return (current, 1, start.Index);
                }

                cross = Vertex.Cross(start, end, vtx);
                if (Math.Abs(cross) <= eps &&
                    x >= Math.Min(start.X, end.X) - eps && x <= Math.Max(start.X, end.X) + eps &&
                    y >= Math.Min(start.Y, end.Y) - eps && y <= Math.Max(start.Y, end.Y) + eps)
                {
                    return (current, 0, NO_INDEX);
                }

                if (cross <= 0)
                {
                    inside = false;
                    if (bestExit == NO_INDEX || cross < worstCross)
                    {
                        worstCross = cross;
                        bestExit = t.adjBC;
                    }
                }

                // c -> a
                start = _vertices[t.indxC];
                end = _vertices[t.indxA];

                if (Vertex.CloseOrEqual(start, vtx, eps))
                {
                    return (current, 2, start.Index);
                }

                cross = Vertex.Cross(start, end, vtx);
                if (Math.Abs(cross) <= eps &&
                    x >= Math.Min(start.X, end.X) - eps && x <= Math.Max(start.X, end.X) + eps &&
                    y >= Math.Min(start.Y, end.Y) - eps && y <= Math.Max(start.Y, end.Y) + eps)
                {
                    return (current, 0, NO_INDEX);
                }

                if (cross <= 0)
                {
                    inside = false;
                    if (bestExit == NO_INDEX || cross < worstCross)
                    {
                        worstCross = cross;
                        bestExit = t.adjCA;
                    }
                }

                if (inside)
                {
                    return (current, NO_INDEX, NO_INDEX);
                }
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
            TriangleWalker walker = new TriangleWalker(_triangles, a);

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
    }
}
