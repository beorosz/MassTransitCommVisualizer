using System.Linq;
using MassTransitCommVisualizer.Model;
using QuikGraph;

namespace MassTransitCommVisualizer
{
    public interface IGraphWalkerAlgorithms
    {
        MessageFlowGraph WalkOutEdgesFrom(MessageFlowGraph originalGraph, MessageHandlerDefinition startingMessageHandlerDefinition);
    }

    public class GraphWalkerAlgorithms : IGraphWalkerAlgorithms
    {
        public MessageFlowGraph WalkOutEdgesFrom(MessageFlowGraph originalGraph, MessageHandlerDefinition startingMessageHandlerDefinition)
        {
            var subGraph = new MessageFlowGraph();
            WalkTheGraphOutEdgesRecursively(originalGraph, startingMessageHandlerDefinition, subGraph);

            return subGraph;
        }

        private void WalkTheGraphOutEdgesRecursively(MessageFlowGraph originalGraph, MessageHandlerDefinition vertex, MessageFlowGraph generatedGraph)
        {
            // if we've already visited the vertex, skip it to avoid infinite cycle
            if (generatedGraph.Edges.Any(edge => edge.Source.Equals(vertex)))
            {
                return;
            }

            foreach (var outEdge in originalGraph.OutEdges(vertex))
            {
                generatedGraph.AddVerticesAndEdge(
                    new TaggedEdge<MessageHandlerDefinition, MessageDefinition>(outEdge.Source, outEdge.Target, outEdge.Tag));
                WalkTheGraphOutEdgesRecursively(originalGraph, outEdge.Target, generatedGraph);
            }
        }
    }
}
