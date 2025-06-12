using System.Runtime.CompilerServices;

namespace CDTlib
{
    public class Edge : ISplittable
    {
        public Edge(Node origin)
        {
            Origin = origin;
        }

        public void Deconstruct(out Node start, out Node end)
        {
            start = Origin;
            end = Next.Origin;
        }

        public Node Origin { get; set; }
        public Edge Next { get; set; } = null!;
        public Edge Prev { get; set; } = null!;
        public Edge? Twin { get; set; } = null;
        public Face Face { get; set; } = null!;
        public bool Constrained { get; set; } = false;

        public void Quad(out Node a, out Node b, out Node c, out Node d)
        {
            /*
                        d          
                        /\         
                       /  \        
                      /    \       
                     /      \      
                    /   f0   \     
                   /          \    
                  /            \   
               a +--------------+ c
                  \            /   
                   \          /    
                    \   f1   /     
                     \      /      
                      \    /       
                       \  /        
                        \/         
                        b          
            */

            (a, c) = this;
            d = Prev.Origin;
            b = Twin!.Prev.Origin;

            Edge ab = Twin.Next;
            Edge bc = ab.Next;
            Edge cd = this.Next;
            Edge da = cd.Next;

            a.Edge = ab;
            b.Edge = bc;
            c.Edge = cd;
            d.Edge = da;
        }

        public TopologyChange Flip()
        {
            if (Twin is null)
            {
                throw new Exception("Can't flip edge with not twin");
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
                  a +--------------+ c      a +   t1  |  t0   + c
                     \            /            \      |      /
                      \          /              \     |     /
                       \   t1   /                \    |    /
                        \      /                  \   |   / 
                         \    /                    \  |  /
                          \  /                      \ | /
                           \/                        \|/
                            b                         b
             */


            Quad(out Node a, out Node b, out Node c, out Node d);

            Face old0 = Face;
            Face old1 = Twin.Face;

            Edge ac = this;
            Edge ca = ac.Twin;

            Edge cd = c.Edge;
            Edge da = d.Edge;

            Edge ab = a.Edge;
            Edge bc = b.Edge;

            Face f0 = new Face(old0.Index, d, b, c).ComputeArea();
            Face f1 = new Face(old1.Index, b, c, a)
            {
                Area = old0.Area + old1.Area - f0.Area
            };

            // twins
            f0.Edge.Twin = f1.Edge;
            f0.Edge.Next.Twin = bc.Twin;
            f0.Edge.Prev.Twin = cd.Twin;

            f1.Edge.Twin = f0.Edge;
            f1.Edge.Next.Twin = da.Twin;
            f1.Edge.Prev.Twin = ab.Twin;

            // constraints
            f0.Edge.Constrained = f1.Edge.Constrained = Constrained; // this is arguable scenario, but for brute flip will do

            f0.Edge.Next.Constrained = bc.Constrained;
            f0.Edge.Prev.Constrained = cd.Constrained;

            f1.Edge.Next.Constrained = da.Constrained;
            f1.Edge.Prev.Constrained = ab.Constrained;

            return new TopologyChange()
            {
                OldFaces = [old0, old1],
                NewFaces = [f0, f1],
                AffectedEdges = [f0.Edge.Next, f1.Edge.Prev]
            };
        }

        public TopologyChange Split(Node node)
        {
            if (Twin is null)
            {
                return SplitNoTwin(node);
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
               a +--------------+ c        a +------node-----+ c  
                  \            /              \      |      /      
                   \          /                \ f3  |  f2 /       
                    \   f1   /                  \    |    /        
                     \      /                    \   |   /      
                      \    /                      \  |  /          
                       \  /                        \ | /           
                        \/                          \|/            
                        b                            b            
            */

            Face old0 = Face;
            Face old1 = Twin.Face;

            Quad(out Node a, out Node b, out Node c, out Node d);
            Edge ac = this;
            Edge ca = ac.Twin;

            Edge cd = c.Edge;
            Edge da = d.Edge;

            Edge ab = a.Edge;
            Edge bc = b.Edge;

            Face f0 = new Face(old0.Index, node, d, a).ComputeArea();
            Face f1 = new Face(old1.Index, node, c, d) { Area = old0.Area - f0.Area };
            Face f2 = new Face(-1, node, b, c).ComputeArea();
            Face f3 = new Face(-1, node, a, b) { Area = old1.Area - f1.Area };

            // twins
            f0.Edge.Twin = f1.Edge.Prev;
            f0.Edge.Next.Twin = da.Twin;
            f0.Edge.Prev.Twin = f3.Edge;

            f1.Edge.Twin = f2.Edge.Prev;
            f1.Edge.Next.Twin = cd.Twin;
            f1.Edge.Prev.Twin = f0.Edge;

            f2.Edge.Twin = f3.Edge.Prev;
            f2.Edge.Next.Twin = bc.Twin;
            f2.Edge.Prev.Twin = f1.Edge;

            f3.Edge.Twin = f0.Edge.Prev;
            f3.Edge.Next.Twin = ab.Twin;
            f3.Edge.Prev.Twin = f2.Edge;

            // constraints
            f0.Edge.Next.Constrained = da.Constrained;
            f1.Edge.Next.Constrained = cd.Constrained;
            f2.Edge.Next.Constrained = bc.Constrained;
            f3.Edge.Next.Constrained = ab.Constrained;

            f0.Edge.Prev.Constrained 
                = f1.Edge.Constrained
                = f2.Edge.Prev.Constrained
                = f3.Edge.Constrained = Constrained;

            return new TopologyChange()
            {
                OldFaces = [old0, old1],
                NewFaces = [f0, f1, f2, f3],
                AffectedEdges = [f0.Edge.Next, f1.Edge.Next, f2.Edge.Next, f3.Edge.Next]
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        TopologyChange SplitNoTwin(Node node)
        {
            /*
                        c                            c        
                        /\                          /|\        
                       /  \                        / | \       
                      /    \                      /  |  \      
                     /      \                    /   |   \      
                    /  f0    \                  /    |    \    
                   /          \                /     |     \   
                  /            \              /  f0  |  f1  \  
               a +--------------+ b        a +-------+-------+ b
                                                    node
            */

            var (a, b, c) = Face;
            Edge ab = Face.Edge;
            Edge bc = ab.Next;
            Edge ca = bc.Next;

            Face f0 = new Face(Face.Index, node, c, a).ComputeArea();
            Face f1 = new Face(-1, node, b, c) { Area = Face.Area - f0.Area };

            f0.Edge.Twin = f1.Edge;
            f0.Edge.Next.Twin = ca.Twin;
            f0.Edge.Prev.Twin = null;

            f1.Edge.Twin = f0.Edge;
            f1.Edge.Next.Twin = null;
            f1.Edge.Prev.Twin = bc.Twin;

            f0.Edge.Next.Constrained = ca.Constrained;
            f1.Edge.Prev.Constrained = bc.Constrained;
            f0.Edge.Prev.Constrained = f1.Edge.Next.Constrained = Constrained;

            return new TopologyChange()
            {
                OldFaces = [Face],
                NewFaces = [f0, f1],
                AffectedEdges = [f0.Edge.Next, f1.Edge.Prev]
            };
        }

        public void Center(out double x, out double y)
        {
            var (a, b) = this;
            x = (a.X + b.X) / 2.0;
            y = (a.Y + b.Y) / 2.0;
        }

        public override string ToString()
        {
            var (a, b) = this;
            return $"{a.Index} {b.Index}";
        }
    }
}
