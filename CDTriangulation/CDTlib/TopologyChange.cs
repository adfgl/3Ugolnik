using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTlib
{
    public class TopologyChange
    {
        public Face[] NewFaces { get; set; }
        public Face[] OldFaces { get; set; }
        public Edge[] AffectedEdges {  get; set; }
    }
}
