using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public class Quality
    {
        public double MaxArea { get; set; } = -1;
        public double MinAngle { get; set; } = 0;
        public double MaxEdgeLength { get; set; } = -1;
    }
}
