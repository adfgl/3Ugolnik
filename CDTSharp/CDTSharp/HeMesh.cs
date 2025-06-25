using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CDTSharp
{
    public class HeMesh
    {
        readonly List<HeTriangle> _triangles;
        readonly List<HeNode> _nodes;

        public HeTriangle[] Flip(HeEdge edge)
        {
            HeEdge? twin = edge.Twin;
            if (twin is null || edge.Constrained)
            {
                return [];
            }

            /*
              b - is inserted point, we want to propagate flip away from it, otherwise we 
              are risking ending up in flipping degeneracy
                           d                          d
                           /\                        /|\
                          /  \                      / | \
                         /    \                    /  |  \
                        /      \                  /   |   \ 
                       /   t0   \                /    |    \
                      /          \              /     |     \ 
                     /            \            /      |      \
                  a +--------------+ c      a +   t0  |  t1   + c
                     \            /            \      |      /
                      \          /              \     |     /
                       \   t1   /                \    |    /
                        \      /                  \   |   / 
                         \    /                    \  |  /
                          \  /                      \ | /
                           \/                        \|/
                            b                         b
             */

            HeTriangle old0 = edge.Triangle;
            HeTriangle old1 = twin.Triangle;

            HeEdge cd = edge.Next;
            HeEdge da = cd.Next;
            HeEdge ab = twin.Next;
            HeEdge bc = ab.Next;

            var (a, c) = edge;
            HeNode d = da.Origin;
            HeNode b = bc.Origin;

            HeTriangle new0 = new HeTriangle(old0.Index, a, b, d);
            HeTriangle new1 = new HeTriangle(old1.Index, b, c, d, old0.Area + old1.Area - new0.Area);

            new0.Edge.Propose(ab);
            new0.Edge.Prev.Propose(da);
            new1.Edge.Propose(bc);
            new1.Edge.Next.Propose(cd);

            new0.Edge.Next.SetTwin(new1.Edge.Prev);

            return [new0, new1];
        }

        public HeTriangle[] Split(HeEdge edge, HeNode node)
        {
            HeEdge? twin = edge.Twin;
            if (twin is null)
            {
                return SplitNoTwin(edge, node);
            }

            /*
                    d                            d            
                    /\                          /|\             
                   /  \                        / | \           
                  /    \                      /  |  \          
                 /      \                    /   |   \       
                /   f0   \                  /    |    \        
               /          \                / f0  |  f1 \       
              /            \              /      |      \      
           a +--------------+ c        a +-------e-------+ c  
              \            /              \      |      /      
               \          /                \ f3  |  f2 /       
                \   f1   /                  \    |    /        
                 \      /                    \   |   /      
                  \    /                      \  |  /          
                   \  /                        \ | /           
                    \/                          \|/            
                    b                            b            
          */

            HeTriangle old0 = edge.Triangle;
            HeTriangle old1 = twin.Triangle;

            HeEdge cd = edge.Next;
            HeEdge da = cd.Next;
            HeEdge ab = twin.Next;
            HeEdge bc = ab.Next;

            var (a, c) = edge;
            HeNode d = da.Origin;
            HeNode b = bc.Origin;
            HeNode e = node;

            int baseIndex = _triangles.Count;
            HeTriangle new0 = new HeTriangle(old0.Index, d, a, e);
            HeTriangle new1 = new HeTriangle(old1.Index, c, d, e, old0.Area - new0.Area);
            HeTriangle new2 = new HeTriangle(baseIndex, b, c, e);
            HeTriangle new3 = new HeTriangle(baseIndex + 1, a, b, e, old1.Area - new2.Area);

            new0.Edge.Propose(da);
            new1.Edge.Propose(cd);
            new2.Edge.Propose(bc);
            new3.Edge.Propose(ab);

            new0.Edge.Next.SetTwin(new1.Edge.Next);
            new1.Edge.Next.SetTwin(new2.Edge.Next);
            new2.Edge.Next.SetTwin(new3.Edge.Next);
            new3.Edge.Next.SetTwin(new0.Edge.Next);

            if (edge.Constrained)
            {
                new0.Edge.Next.SetConstraint(true);
                new1.Edge.Prev.SetConstraint(true);
            }
            return [new0, new1, new2, new3];
        }

        public HeTriangle[] SplitNoTwin(HeEdge edge, HeNode node)
        {
            /*
                       c                            c        
                       /\                          /|\        
                      /  \                        / | \       
                     /    \                      /  |  \      
                    /      \                    /   |   \      
                   /  t0    \                  /    |    \    
                  /          \                /     |     \   
                 /            \              /  t0  |  t1  \  
              a +--------------+ b        a +-------+-------+ b
                                                    d
           */

            HeTriangle old0 = edge.Triangle;

            HeEdge ab = old0.Edge;
            HeEdge bc = ab.Next;
            HeEdge ca = bc.Next;

            HeNode a = ab.Origin;
            HeNode b = bc.Origin;
            HeNode c = ca.Origin;
            HeNode d = node;

            HeTriangle new0 = new HeTriangle(old0.Index, c, a, d);
            HeTriangle new1 = new HeTriangle(_triangles.Count, b, c, d);

            new0.Edge.Prev.SetTwin(new1.Edge.Next);
            
            new0.Edge.Propose(ca.Twin);
            new1.Edge.Propose(bc.Twin);

            if (edge.Constrained)
            {
                new0.Edge.Next.Constrained = new1.Edge.Prev.Constrained = true;
            }

            return [new0, new1];
        }

        public HeTriangle[] Split(HeTriangle triangle, HeNode node)
        {
            /*
                        *C



                   /           ^
                  ^      *D     \



              A*        ->         *B

            */

            HeEdge ab = triangle.Edge;
            HeEdge bc = ab.Next;
            HeEdge ca = bc.Next;

            HeNode a = ab.Origin;
            HeNode b = bc.Origin;
            HeNode c = ca.Origin;
            HeNode d = node;

            a.Edge = ab;
            b.Edge = bc;
            c.Edge = ca;

            HeTriangle new0 = new HeTriangle(triangle.Index, a, b, d);
            HeTriangle new1 = new HeTriangle(_triangles.Count, b, c, d);
            HeTriangle new2 = new HeTriangle(_triangles.Count + 1, c, a, d, triangle.Area - new0.Area - new1.Area);

            new0.Edge.Propose(ab);
            new1.Edge.Propose(bc);
            new2.Edge.Propose(ca);

            new0.Edge.Next.SetTwin(new1.Edge.Prev);
            new1.Edge.Next.SetTwin(new2.Edge.Prev);
            new2.Edge.Next.SetTwin(new0.Edge.Prev);

            return [new0, new1, new2];
        }
    }
}
