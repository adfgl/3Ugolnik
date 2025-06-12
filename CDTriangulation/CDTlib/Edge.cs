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

        public SplitResult Split(Node node)
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

            Edge ac = this;
            Edge cd = ac.Next;
            Edge da = cd.Next;

            Edge ca = Twin;
            Edge ab = ca.Next;
            Edge bc = ab.Next;

            var (a, c) = this;
            Node d = Prev.Origin;
            Node b = Twin.Prev.Origin;

            Face f0 = new Face(old0.Index, node, d, a);
            Face f1 = new Face(old1.Index, node, c, d);
            Face f2 = new Face(-1, node, b, c);
            Face f3 = new Face(-1, node, a, b);

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

            return new SplitResult()
            {
                OldFaces = [old0, old1],
                NewFaces = [f0, f1, f2, f3],
                AffectedEdges = [f0.Edge.Next, f1.Edge.Next, f2.Edge.Next, f3.Edge.Next]
            };
        }


        SplitResult SplitNoTwin(Node node)
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

            Face f0 = new Face(Face.Index, node, c, a);
            Face f1 = new Face(-1, node, b, c);

            f0.Edge.Twin = f1.Edge;
            f0.Edge.Next.Twin = ca.Twin;
            f0.Edge.Prev.Twin = null;

            f1.Edge.Twin = f0.Edge;
            f1.Edge.Next.Twin = null;
            f1.Edge.Prev.Twin = bc.Twin;

            f0.Edge.Next.Constrained = ca.Constrained;
            f1.Edge.Prev.Constrained = bc.Constrained;
            f0.Edge.Prev.Constrained = f1.Edge.Next.Constrained = Constrained;

            return new SplitResult()
            {
                OldFaces = [Face],
                NewFaces = [f0, f1],
                AffectedEdges = [f0.Edge.Next, f1.Edge.Prev]
            };
        }

        public override string ToString()
        {
            var (a, b) = this;
            return $"{a.Index} {b.Index}";
        }
    }
}
