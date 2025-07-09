namespace TriSharp
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;
    using System.Xml.Linq;
    using static Int3;

    public struct Triangle
    {
        public int index;
        public Int3 inds;
        public Int3 adjs;
        public Int3 cons;
        public Circle circle;

        public Triangle(int index,
            Vertex a, Vertex b, Vertex c, 
            int abAdj = NO_INDEX, int bcAdj = NO_INDEX, int caAdj = NO_INDEX, 
            int abCon = NO_INDEX, int bcCon = NO_INDEX, int caCon = NO_INDEX)
        {
            this.index = index;
            this.inds = new Int3(a.Index, b.Index, c.Index);
            this.adjs = new Int3(abAdj, bcAdj, caAdj);
            this.cons = new Int3(abCon, bcCon, caCon);
            this.circle = new Circle(a.X, a.Y, b.X, b.Y, c.X, c.Y);
        }

        public static Triangle[] Flip(IReadOnlyList<Triangle> triangles, IReadOnlyList<Vertex> nodes, int triangle, int edge)
        {
            /*
                   v3 - is inserted point, we want to propagate flip away from it, otherwise we 
                   are risking ending up in flipping degeneracy
                        v2                         v2
                        /\                        /|\
                       /  \                      / | \
                      /    \                    /  |  \
                     /      \                  /   |   \ 
                    /   t0   \                /    |    \
                   /          \              /     |     \ 
                  /            \            /      |      \
              v0 +--------------+ v1    v0 +   t0  |  t1   + v1
                  \            /            \      |      /
                   \          /              \     |     /
                    \   t1   /                \    |    /
                     \      /                  \   |   / 
                      \    /                    \  |  /
                       \  /                      \ | /
                        \/                        \|/
                        v3                         v3
             */

            int t0 = triangle;
            Triangle old0 = triangles[t0]; Debug.Assert(t0 == old0.index);
            Vertex v0 = nodes[old0.inds.a]; Debug.Assert(old0.inds.a == v0.Index);
            Vertex v1 = nodes[old0.inds.b]; Debug.Assert(old0.inds.b == v1.Index);
            Vertex v2 = nodes[old0.inds.c]; Debug.Assert(old0.inds.c == v2.Index);

            Deconstruct(old0, edge, out _, out int adj01, out int adj12, out int adj20, out int con01, out int con12, out int con20);

            int t1 = adj01;
            Triangle old1 = triangles[t1]; Debug.Assert(t1 == old1.index);

            int twin = old1.inds.IndexOf(v1.Index, v0.Index);
            Deconstruct(old1, twin, out int i3, out int adj10, out int adj03, out int adj31, out int con10, out int con03, out int con31);

            Debug.Assert(con01 == con10);
            Debug.Assert(adj01 == t1);
            Debug.Assert(adj10 == t0);

            Vertex v3 = nodes[i3]; Debug.Assert(i3 == v3.Index);

            return [
                new Triangle(t0, v0, v3, v2, adj03, t1, adj20, con03, -1, con20),
                new Triangle(t1, v3, v1, v2, adj31, adj12, t0,con31, con12, -1)
            ];
        }

        public static Triangle[] Split(IReadOnlyList<Triangle> triangles, IReadOnlyList<Vertex> nodes, int triangle, Vertex node)
        {
            /*
                        * v2



                    /     v3   ^
                   ^ new2*  new1\

                        new0

              v0 *        ->         * v1

           */

            int t0 = triangle;
            int t1 = triangles.Count;
            int t2 = t1 + 1;

            Triangle t = triangles[t0];  Debug.Assert(t0 == t.index);
            Vertex v0 = nodes[t.inds.a]; Debug.Assert(t.inds.a == v0.Index);
            Vertex v1 = nodes[t.inds.b]; Debug.Assert(t.inds.b == v1.Index);
            Vertex v2 = nodes[t.inds.c]; Debug.Assert(t.inds.c == v2.Index);
            Vertex v3 = node;

            return [
                new Triangle(t0, v0, v1, v3, t.adjs.a, t1, t2, t.cons.a, -1, -1),
                new Triangle(t1, v1, v2, v3, t.adjs.b, t2, t0, t.cons.b, -1, -1),
                new Triangle(t2, v2, v0, v3, t.adjs.c, t0, t1, t.cons.c, -1, -1)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        static void Deconstruct(Triangle t, int edge, out int opposite, out int adj0, out int adj1, out int adj2, out int con0, out int con1, out int con2)
        {
            switch (edge)
            {
                case 0:
                    adj0 = t.adjs.a;
                    adj1 = t.adjs.b;
                    adj2 = t.adjs.c;

                    con0 = t.cons.a;
                    con1 = t.cons.b;
                    con2 = t.cons.c;

                    opposite = t.inds.c;
                    break;

                case 1:
                    adj0 = t.adjs.b;
                    adj1 = t.adjs.c;
                    adj2 = t.adjs.a;

                    con0 = t.cons.b;
                    con1 = t.cons.c;
                    con2 = t.cons.a;

                    opposite = t.inds.a;
                    break;

                case 2:
                    adj0 = t.adjs.c;
                    adj1 = t.adjs.a;
                    adj2 = t.adjs.b;

                    con0 = t.cons.c;
                    con1 = t.cons.a;
                    con2 = t.cons.b;

                    opposite = t.inds.b;
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public static Triangle[] Split(IReadOnlyList<Triangle> triangles, IReadOnlyList<Vertex> nodes, int triangle, int edge, Vertex node)
        {
            /*
                         v2                           v2           
                         /\                          /|\             
                        /  \                        / | \           
                       /    \                      /  |  \          
                      /      \                    /   |   \       
                     /  old0  \                  /new0|new1\        
                    /          \                /     |     \       
                   /            \              /      |      \      
               v0 +--------------+ v1      v0 +-------X-------+ v1  
                   \            /              \      |node  /      
                    \          /                \     |     /       
                     \  old1  /                  \new3|new2/        
                      \      /                    \   |   /      
                       \    /                      \  |  /          
                        \  /                        \ | /           
                         \/                          \|/            
                         v3                           v3            
             */

            int t0 = triangle;
            Triangle old0 = triangles[t0];  Debug.Assert(t0 == old0.index);
            Vertex v0 = nodes[old0.inds.a]; Debug.Assert(old0.inds.a == v0.Index);
            Vertex v1 = nodes[old0.inds.b]; Debug.Assert(old0.inds.b == v1.Index);
            Vertex v2 = nodes[old0.inds.c]; Debug.Assert(old0.inds.c == v2.Index);

            Deconstruct(old0, edge, out _, out int adj01, out int adj12, out int adj20, out int con01, out int con12, out int con20);
            if (adj01 == NO_INDEX)
            {
                /*
                           v2                           v2        
                           /\                          /|\        
                          /  \                        / | \       
                         /    \                      /  |  \      
                        /      \                    /   |   \      
                       /  old   \                  /    |    \    
                      /          \                /     |     \   
                     /            \              /  new0|new1  \  
                 v0 +--------------+ v1      v0 +-------x-------+ v1
                                                        node
               */

                int t1 = triangles.Count;
                return [
                    new Triangle(t0, v2, v0, node, adj20, NO_INDEX, t1, con20, con01, NO_INDEX),
                    new Triangle(t1, v1, v2, node, adj12, t0, NO_INDEX, con12, NO_INDEX, con01)];
            }
            else
            {
                int t1 = adj01;
                Triangle old1 = triangles[t1]; Debug.Assert(t1 == old1.index);

                int t2 = triangles.Count;
                int t3 = t2 + 1;

                int twin = old1.inds.IndexOf(v1.Index, v0.Index);
                Deconstruct(old1, twin, out int i3, out int adj10, out int adj03, out int adj31, out int con10, out int con03, out int con31);

                Debug.Assert(con01 == con10);
                Debug.Assert(adj01 == t1);
                Debug.Assert(adj10 == t0);

                Vertex v3 = nodes[i3]; Debug.Assert(i3 == v3.Index);
                return [

                    new Triangle(t0, v2, v0, node, adj20, t3, t1, con20, con01, -1),
                    new Triangle(t1, v1, v2, node, adj12, t0, t2, con12, -1, con01),
                    new Triangle(t2, v3, v1, node, adj31, t1, t3, con31, con10, -1),
                    new Triangle(t3, v0, v3, node, adj03, t2, t0, con03, -1, con10)];
            }
        }

        public override string ToString()
        {
            return $"[{index}] {inds}";
        }
    }
}
