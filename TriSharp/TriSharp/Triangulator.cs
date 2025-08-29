using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriSharp
{
    public enum ESuperStructure
    {
        Triangle, Square, Circle, Hull
    }

    public class Triangulator
    {
        Rect _bounds;

        public Triangulator(Rect bounds)
        {
            _bounds = bounds;
        }

        void AddSuperStructure()
        {

        }

        public void InsertVertex(Vertex vertex)
        {

        }

        public void RemoveVertex(Vertex vertex)
        {

        }

        public void InsertConstraint(Vertex a, Vertex b, int type)
        {

        }

        public void RemoveConstraint(Vertex a, Vertex b)
        {

        }

    }
}
