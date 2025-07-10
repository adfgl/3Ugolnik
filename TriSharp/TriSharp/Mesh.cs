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
