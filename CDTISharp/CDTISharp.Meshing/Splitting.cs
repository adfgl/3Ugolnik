using CDTISharp.Geometry;

namespace CDTISharp.Meshing
{
    public static class Splitting
    {
        public static Triangle[] Split(List<Triangle> triangles, List<Node> nodes, int triangle, int edge, Node node)
        {
            Triangle old0 = triangles[triangle];
            int constraint = old0.constraints[edge];

            Node a = nodes[old0.indices[0]];
            Node b = nodes[old0.indices[1]];
            Node c = nodes[old0.indices[2]];
            Node e = node;

            int bc = Mesh.NEXT[edge];
            int ca = Mesh.PREV[edge];

            int adjIndex = old0.adjacent[edge];

            Triangle[] newTris;
            if (adjIndex == -1)
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
                                                          e
                 */

                int t0 = triangle;
                int t1 = triangles.Count;

                Triangle new0 = new Triangle(t0, c, a, e);
                new0.adjacent[0] = old0.adjacent[ca];
                new0.adjacent[1] = -1;
                new0.adjacent[2] = t1;

                new0.constraints[0] = old0.constraints[ca];
                new0.constraints[1] = constraint;
                new0.constraints[2] = -1;

                Triangle new1 = new Triangle(t1, b, c, e);
                new1.adjacent[0] = old0.adjacent[bc];
                new1.adjacent[1] = t0;
                new1.adjacent[2] = -1;

                new1.constraints[0] = old0.constraints[bc];
                new1.constraints[1] = -1;
                new1.constraints[2] = constraint;

                newTris = [new0, new1];
            }
            else
            {
                Triangle old1 = triangles[adjIndex];


                /*
                         c                            c            
                         /\                          /|\             
                        /  \                        / | \           
                       /    \                      /  |  \          
                      /      \                    /   |   \       
                     /   f0   \                  /    |    \        
                    /          \                / f0  |  f1 \       
                   /            \              /      |      \      
                a +--------------+ b        a +-------e-------+ b  
                   \            /              \      |      /      
                    \          /                \ f3  |  f2 /       
                     \   f1   /                  \    |    /        
                      \      /                    \   |   /      
                       \    /                      \  |  /          
                        \  /                        \ | /           
                         \/                          \|/            
                         d                            d            
             */

                int twin = old1.IndexOf(b.Index, a.Index);
                Node d = nodes[old1.indices[Mesh.PREV[twin]]];

                int ad = Mesh.NEXT[edge];
                int db = Mesh.PREV[edge];

                int t0 = old0.index;
                int t1 = old1.index;
                int t2 = triangles.Count;
                int t3 = t2 + 1;

                Triangle new0 = new Triangle(t0, c, a, e);
                new0.adjacent[0] = old0.adjacent[ca];
                new0.adjacent[1] = t3;
                new0.adjacent[2] = t1;

                new0.constraints[0] = old0.constraints[ca];
                new0.constraints[1] = constraint;
                new0.constraints[2] = -1;

                Triangle new1 = new Triangle(t1, b, c, e);
                new1.adjacent[0] = old0.adjacent[bc];
                new1.adjacent[1] = t0;
                new1.adjacent[2] = t2;

                new1.constraints[0] = old0.constraints[bc];
                new1.constraints[1] = -1;
                new1.constraints[2] = constraint;

                Triangle new2 = new Triangle(t2, d, b, e);
                new2.adjacent[0] = old1.adjacent[db];
                new2.adjacent[1] = t1;
                new2.adjacent[2] = t3;

                new2.constraints[0] = old0.constraints[db];
                new2.constraints[1] = constraint;
                new2.constraints[2] = -1;

                Triangle new3 = new Triangle(t3, a, d, e);
                new3.adjacent[0] = old1.adjacent[ad];
                new3.adjacent[1] = t2;
                new3.adjacent[2] = t0;

                new3.constraints[0] = old0.constraints[ad];
                new3.constraints[1] = -1;
                new3.constraints[2] = constraint;

                newTris = [new0, new1, new2, new3];
            }
            return newTris;
        }

        public static Triangle[] Split(List<Triangle> triangles, List<Node> nodes, int triangle, Node node)
        {
            /*
                         * v2



                     /           ^
                    ^      *v3     \



               v0 *        ->         * v1

            */

            Triangle old = triangles[triangle];
            Node v0 = nodes[old.indices[0]];
            Node v1 = nodes[old.indices[1]];
            Node v2 = nodes[old.indices[2]];
            Node v3 = node;

            int t0 = triangle;
            int t1 = triangles.Count;
            int t2 = t1 + 1;

            Triangle new0 = new Triangle(t0, v0, v1, v3);
            new0.adjacent[0] = old.adjacent[0];
            new0.adjacent[1] = t1;
            new0.adjacent[2] = t2;

            new0.constraints[0] = old.constraints[0];

            Triangle new1 = new Triangle(t1, v1, v2, v3);
            new1.adjacent[0] = old.adjacent[1];
            new1.adjacent[1] = t2;
            new1.adjacent[2] = t0;

            new1.constraints[0] = old.constraints[1];

            Triangle new2 = new Triangle(t2, v2, v0, v3);
            new2.adjacent[0] = old.adjacent[2];
            new2.adjacent[1] = t0;
            new2.adjacent[2] = t1;

            new2.constraints[0] = old.constraints[2];

            return [new0, new1, new2];
        }
    }
}
