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

        public override string ToString()
        {
            return $@"
                CDT Summary
                -----------
                Execution time : {Execution} ms

                Triangles      : {TriangleCount}
                Nodes          : {NodeCount}

                Edge Lengths:
                  Min          : {MinEdge:F4}
                  Max          : {MaxEdge:F4}
                  Avg          : {AvgEdge:F4}

                Triangle Areas:
                  Min          : {MinArea:F4}
                  Max          : {MaxArea:F4}
                  Avg          : {AvgArea:F4}

                Angles (°):
                  Min          : {MinAngle:F2}
                  Max          : {MaxAngle:F2}
                  Avg          : {AvgAngle:F2}
                ".Trim();
        }

    }
}
