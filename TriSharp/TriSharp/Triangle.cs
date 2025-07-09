namespace TriSharp
{
    using static Int3;

    public struct Triangle
    {
        public int index;
        public Int3 indices;
        public Int3 adjacent;
        public Int3 constraints;

        public Triangle(int index, 
            int a, int b, int c, 
            int abAdj = NO_INDEX, int bcAdj = NO_INDEX, int caAdj = NO_INDEX, 
            int abCon = NO_INDEX, int bcCon = NO_INDEX, int caCon = NO_INDEX)
        {
            this.index = index;
            this.indices = new Int3(a, b, c);
            this.adjacent = new Int3(abAdj, bcAdj, caAdj);
            this.constraints = new Int3(abCon, bcCon, caCon);
        }
    }
}
