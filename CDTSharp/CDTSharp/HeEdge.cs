using CDTGeometryLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public class HeEdge
    {
        public HeEdge(HeNode origin)
        {
            Origin = origin;
        }

        public void Deconstruct(out HeNode start, out HeNode end)
        {
            start = Origin;
            end = Next.Origin;
        }

        public HeNode Origin { get; }
        public HeEdge Next { get; set; } = null!;
        public HeEdge Prev => Next.Next;
        public HeEdge? Twin { get; set; } = null;
        public HeTriangle Triangle { get; set; } = null!;
        public bool Constrained { get; set; } = false;

        public double SquareLength()
        {
            var (sx, sy) = Origin;
            var (ex, ey) = Next.Origin;
            double dx = ex - sx;
            double dy = ey - sy;
            return dx * dx + dy * dy;
        }

        public bool Contains(HeNode node)
        {
            return Origin == node || Next.Origin == node;
        }

        public double Orientation(double x, double y)
        {
            return GeometryHelper.Cross(Origin, Next.Origin, x, y);
        }

        public void CopyProperties(HeEdge? twin)
        {
            Twin = twin;
            if (twin is not null)
            {
                Constrained = twin.Constrained;
            }
        }

        public void SetTwin(HeEdge? twin)
        {
            Twin = twin;
            if (twin is not null)
            {
                twin.Twin = this;
            }
        }

        public void SetConstraint(bool value)
        {
            Constrained = value;
            if (Twin is not null)
            {
                Twin.Constrained = value;
            }
        }
    }
}
