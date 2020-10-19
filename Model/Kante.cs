using ExRam.Gremlinq.Core.GraphElements;

namespace ExRam.Gremlinq.Samples
{
    public abstract class Kante : IEdge
    {
        public object Id { get; set; }
        public string Label { get; set; }
    }
}