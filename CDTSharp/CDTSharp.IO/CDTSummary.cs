namespace CDTSharp.IO
{
    public class CDTSummary
    {
        public long Execution { get; set; }

        public int TriangleCount { get; set; }
        public int NodeCount { get; set; }
        
        public double MinEdge { get; set; }
        public double MaxEdge { get; set; }
        public double AvgEdge { get; set; }

        public double MinArea { get; set; }
        public double MaxArea { get; set; }
        public double AvgArea { get; set; }

        public double MinAngle { get; set; }
        public double MaxAngle { get; set; }
        public double AvgAngle { get; set; }
    }
}
