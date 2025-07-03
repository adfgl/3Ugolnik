using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTISharp.IO
{
    public class CDTTriangle
    {
        public double Area { get; set; }
        public CDTEdge[] Edges { get; set; }

        public override string ToString()
        {
            return $"Triangle | Area: {Area,8:F4} | Nodes: [{string.Join(", ", Edges.Select(e => e.Origin))}]";
        }
    }
}
