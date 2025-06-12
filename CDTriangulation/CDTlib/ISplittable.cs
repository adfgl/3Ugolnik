namespace CDTlib
{
    public interface ISplittable
    {
        TopologyChange Split(Node node);
        void Center(out double x, out double y);
    }
}
