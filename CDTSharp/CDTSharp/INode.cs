using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public interface INode
    {
        int Index { get; }
        double X { get; }
        double Y { get; }
    }
}
