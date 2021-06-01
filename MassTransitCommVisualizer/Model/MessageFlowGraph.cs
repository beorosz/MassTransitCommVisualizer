using System;
using QuikGraph;

namespace MassTransitCommVisualizer.Model
{
    [Serializable]
    public class MessageFlowGraph : AdjacencyGraph<MessageHandlerDefinition, TaggedEdge<MessageHandlerDefinition, MessageDefinition>>
    {
        public MessageFlowGraph(bool allowParallelEdges = true) : base(allowParallelEdges)
        {
        }
    }
}
