namespace TriSharp
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    public struct Triangle
    {
        public const int NO_INDEX = -1;

        public int indxA, indxB, indxC;
        public int adjAB, adjBC, adjCA;
        public int conAB, conBC, conCA;
        public int index;

        public Triangle(int index, int indxA, int indxB, int indxC, int adjAB, int adjBC, int adjCA, int conAB, int conBC, int conCA)
        {
            this.index = index;

            this.indxA = indxA;
            this.indxB = indxB;
            this.indxC = indxC;

            this.adjAB = adjAB;
            this.adjBC = adjBC;
            this.adjCA = adjCA;

            this.conAB = conAB;
            this.conBC = conBC;
            this.conCA = conCA;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EdgeIndex(int a, int b)
        {
            if (indxA == a && indxB == b) return 0;
            if (indxB == a && indxC == b) return 1;
            if (indxC == a && indxA == b) return 2;
            return NO_INDEX;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Triangle Orient(int edge)
        {
            return edge switch
            {
                0 => this,
                1 => new Triangle(index, indxB, indxC, indxA, adjBC, adjCA, adjAB, conBC, conCA, conAB),
                2 => new Triangle(index, indxC, indxA, indxB, adjCA, adjAB, adjBC, conCA, conAB, conBC),
                _ => throw new IndexOutOfRangeException($"Expected index 0, 1 or 2 but got {edge}."),
            };
        }

        public static int Flip(IReadOnlyList<Triangle> triangles, int triangle, int edge, Span<Triangle> output)
        {
            /*
                   d - is inserted point, we want to propagate flip away from it, otherwise we 
                   are risking ending up in flipping degeneracy
                        c                          c
                        /\                        /|\
                       /  \                      / | \
                      /    \                    /  |  \
                     /      \                  /   |   \ 
                    /   t0   \                /    |    \
                   /          \              /     |     \ 
                  /            \            /      |      \
               a +--------------+ b      a +   t0  |  t1   + b
                  \            /            \      |      /
                   \          /              \     |     /
                    \   t1   /                \    |    /
                     \      /                  \   |   / 
                      \    /                    \  |  /
                       \  /                      \ | /
                        \/                        \|/
                        d                          d
             */

            int t0 = triangle;
            Triangle old0 = triangles[t0].Orient(edge);
            Debug.Assert(t0 == old0.index);

            int a = old0.indxA;
            int b = old0.indxB;
            int c = old0.indxC;

            int t1 = old0.adjAB;
            Triangle old1 = triangles[t1]; Debug.Assert(t1 == old1.index);
            int twin = old1.EdgeIndex(b, a);
            old1 = old1.Orient(twin);

            int d = old1.indxC;

            output[0] = new Triangle(t0, a, d, c, old1.adjBC, t1, old0.adjCA, old1.conBC, NO_INDEX, old0.adjCA);
            output[1] = new Triangle(t1, d, b, c, old1.adjCA, old0.adjBC, t0, old1.conCA, old0.conBC, NO_INDEX);
            return 2;
        }

        public static int Split(IReadOnlyList<Triangle> triangles, int triangle, int vtx, Span<Triangle> output)
        {
            /*
                        * c



                    /     vtx   ^
                   ^ new2 X  new1\

                        new0

              a *        ->         * b

           */

            int t0 = triangle;
            int t1 = triangles.Count;
            int t2 = t1 + 1;

            Triangle t = triangles[t0]; 
            Debug.Assert(t0 == t.index);

            int a = t.indxA;
            int b = t.indxB;
            int c = t.indxC;

            output[0] = new Triangle(t0, a, b, vtx, t.adjAB, t1, t2, t.conAB, NO_INDEX, NO_INDEX);
            output[1] = new Triangle(t1, b, c, vtx, t.adjBC, t2, t0, t.conBC, NO_INDEX, NO_INDEX);
            output[2] = new Triangle(t2, c, a, vtx, t.adjCA, t0, t1, t.conCA, NO_INDEX, NO_INDEX);
            return 3;
        }

        public static int Split(IReadOnlyList<Triangle> triangles, int triangle, int edge, int vtx, Span<Triangle> output)
        {
            int t0 = triangle;
            Triangle old0 = triangles[t0].Orient(edge); 
            Debug.Assert(t0 == old0.index);

            int a = old0.indxA;
            int b = old0.indxB;
            int c = old0.indxC;

            if (old0.adjAB == NO_INDEX)
            {
                /*
                           c                            c        
                           /\                          /|\        
                          /  \                        / | \       
                         /    \                      /  |  \      
                        /      \                    /   |   \      
                       /  old   \                  /    |    \    
                      /          \                /     |     \   
                     /            \              /  new0|new1  \  
                  a +--------------+ b        a +-------x-------+ b
                                                        vtx
               */

                int t1 = triangles.Count;

                output[0] = new Triangle(t0, c, a, vtx, old0.adjCA, NO_INDEX, t1, old0.conCA, old0.conAB, NO_INDEX);
                output[1] = new Triangle(t1, b, c, vtx, old0.adjBC, t0, NO_INDEX, old0.conBC, NO_INDEX, old0.conAB);
                return 2;
            }
            else
            {
                /*
                           c                            c           
                           /\                          /|\             
                          /  \                        / | \           
                         /    \                      /  |  \          
                        /      \                    /   |   \       
                       /  old0  \                  /new0|new1\        
                      /          \                /     |     \       
                     /            \              /      |      \      
                  a +--------------+ b        a +-------X-------+ b  
                     \            /              \      |vtx   /      
                      \          /                \     |     /       
                       \  old1  /                  \new3|new2/        
                        \      /                    \   |   /      
                         \    /                      \  |  /          
                          \  /                        \ | /           
                           \/                          \|/            
                           d                            d            
               */


                int t1 = old0.adjAB;
                Triangle old1 = triangles[t1]; 
                Debug.Assert(t1 == old1.index);

                int twin = old1.EdgeIndex(b, a);
                old1 = old1.Orient(twin);

                int t2 = triangles.Count;
                int t3 = t2 + 1;

                int d = old1.indxC;
                Debug.Assert(old0.conAB == old1.conAB);

                output[0] = new Triangle(t0, c, a, vtx, old0.adjCA, t3, t1, old0.conCA, old0.conAB, NO_INDEX);
                output[1] = new Triangle(t1, b, c, vtx, old0.adjBC, t0, t2, old0.conBC, NO_INDEX, old0.conAB);

                output[2] = new Triangle(t2, d, b, vtx, old1.adjCA, t1, t3, old1.conCA, old1.conAB, NO_INDEX);
                output[3] = new Triangle(t3, a, d, vtx, old1.adjBC, t2, t0, old1.conBC, NO_INDEX, old1.conAB);
                return 4;
            }
        }

        public override string ToString()
        {
            return $"[{index}] {indxA} {indxB} {indxC}";
        }
    }
}
