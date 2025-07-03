using CDTISharp.Geometry;
using CDTISharp.Meshing;

namespace CDTISharpTests
{
    public class SplitTests
    {
        [Fact]
        public void TriangleSplit()
        {
            Mesh m = TestCases.Case1();

            int triangleIndex = 3;
            Triangle triangle = m.Triangles[triangleIndex];
            Assert.Equal(triangleIndex, triangle.index);

            Node toInsert = m.Center(triangle); 
            toInsert.Index = m.Nodes.Count;

            Triangle[] tris = Splitting.Split(m.Triangles, m.Nodes, triangle.index, toInsert);
            Assert.Equal(3, tris.Length);

            int a = triangle.indices[0];
            int b = triangle.indices[1];
            int c = triangle.indices[2];
            int d = toInsert.Index;

            Triangle t0 = tris[0];
            Triangle t1 = tris[1];
            Triangle t2 = tris[2];

            Assert.Equal(triangle.index, t0.index);
            Assert.Equal(m.Triangles.Count, t1.index);
            Assert.Equal(m.Triangles.Count + 1, t2.index);

            Assert.Equal([a, b, d], t0.indices);
            Assert.Equal([b, c, d], t1.indices);
            Assert.Equal([c, a, d], t2.indices);

            Assert.Equal([triangle.adjacent[0], t1.index, t2.index], t0.adjacent);
            Assert.Equal([triangle.adjacent[1], t2.index, t0.index], t1.adjacent);
            Assert.Equal([triangle.adjacent[2], t0.index, t1.index], t2.adjacent);

            Assert.Equal([triangle.constraints[0], -1, -1], t0.constraints);
            Assert.Equal([triangle.constraints[1], -1, -1], t1.constraints);
            Assert.Equal([triangle.constraints[2], -1, -1], t2.constraints);
        }

        [Fact]
        public void EdgeSplit()
        {
            Mesh m = TestCases.Case1();

            int triangleIndex = 3;
            int startIndex = 5;
            int endIndex = 4;

            Triangle triangle = m.Triangles[triangleIndex];
            Assert.Equal(triangleIndex, triangle.index);

            Node start = m.Nodes[startIndex];
            Assert.Equal(startIndex, start.Index);

            Node end = m.Nodes[endIndex];
            Assert.Equal(endIndex, end.Index);

            Node toInsert = Node.Between(start, end);
            toInsert.Index = m.Nodes.Count;

            int edge = triangle.IndexOf(start.Index, end.Index);
            Triangle[] tris = Splitting.Split(m.Triangles, m.Nodes, triangle.index, edge, toInsert);
            Assert.Equal(4, tris.Length);

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

            Triangle adjacent = m.Triangles[triangle.adjacent[edge]];

            int ab = edge;
            int bc = Mesh.NEXT[ab];
            int ca = Mesh.PREV[ab];

            int a = triangle.indices[ab];
            int b = triangle.indices[bc];
            int c = triangle.indices[ca];

            int ba = adjacent.IndexOf(b, a);
            int ad = Mesh.NEXT[ba];
            int db = Mesh.PREV[ba];

            int d = adjacent.indices[db];
            int e = toInsert.Index;

            Triangle t0 = tris[0];
            Triangle t1 = tris[1];
            Triangle t2 = tris[2];
            Triangle t3 = tris[3];

            Assert.Equal(triangle.index, t0.index);
            Assert.Equal(adjacent.index, t1.index);
            Assert.Equal(m.Triangles.Count, t2.index);
            Assert.Equal(m.Triangles.Count + 1, t3.index);

            Assert.Equal([c, a, e], t0.indices);
            Assert.Equal([b, c, e], t1.indices);
            Assert.Equal([d, b, e], t2.indices);
            Assert.Equal([a, d, e], t3.indices);

            Assert.Equal([triangle.adjacent[ca], t3.index, t1.index], t0.adjacent);
            Assert.Equal([triangle.adjacent[bc], t0.index, t2.index], t1.adjacent);
            Assert.Equal([adjacent.adjacent[db], t1.index, t3.index], t2.adjacent);
            Assert.Equal([adjacent.adjacent[ad], t2.index, t0.index], t3.adjacent);

            int constraint = triangle.constraints[ab];
            Assert.Equal(constraint, adjacent.constraints[ba]);

            Assert.Equal([triangle.constraints[ca], constraint, -1], t0.constraints);
            Assert.Equal([triangle.constraints[bc], -1, constraint], t1.constraints);
            Assert.Equal([adjacent.constraints[db], constraint, -1], t2.constraints);
            Assert.Equal([adjacent.constraints[ad], -1, constraint], t3.constraints);
        }

        [Fact]
        public void EdgeSplitNoAdjacent()
        {
            Mesh m = TestCases.Case1();

            int triangleIndex = 1;
            int startIndex = 4;
            int endIndex = 1;

            Triangle triangle = m.Triangles[triangleIndex];
            Assert.Equal(triangleIndex, triangle.index);

            Node start = m.Nodes[startIndex];
            Assert.Equal(startIndex, start.Index);

            Node end = m.Nodes[endIndex];
            Assert.Equal(endIndex, end.Index);

            Node toInsert = Node.Between(start, end);
            toInsert.Index = m.Nodes.Count;

            int edge = triangle.IndexOf(start.Index, end.Index);
            Triangle[] tris = Splitting.Split(m.Triangles, m.Nodes, triangle.index, edge, toInsert);
            Assert.Equal(2, tris.Length);

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
    

            int ab = edge;
            int bc = Mesh.NEXT[ab];
            int ca = Mesh.PREV[ab];

            int a = triangle.indices[ab];
            int b = triangle.indices[bc];
            int c = triangle.indices[ca];
            int e = toInsert.Index;

            Triangle t0 = tris[0];
            Triangle t1 = tris[1];

            Assert.Equal(triangle.index, t0.index);
            Assert.Equal(m.Triangles.Count, t1.index);

            Assert.Equal([c, a, e], t0.indices);
            Assert.Equal([b, c, e], t1.indices);

            Assert.Equal([triangle.adjacent[ca], -1, t1.index], t0.adjacent);
            Assert.Equal([triangle.adjacent[bc], t0.index, -1], t1.adjacent);

            Assert.Equal([triangle.constraints[ca], triangle.constraints[ab], -1], t0.constraints);
            Assert.Equal([triangle.constraints[bc], -1, triangle.constraints[ab]], t1.constraints);
        }
    }
}
