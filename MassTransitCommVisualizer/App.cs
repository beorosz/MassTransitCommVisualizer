using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MassTransitCommVisualizer.Model;
using QuikGraph;
using QuikGraph.Serialization;
using Rubjerg.Graphviz;

namespace MassTransitCommVisualizer
{
    public interface IApp
    {
        Task Run(string solutionFilePath, string inputDataFile, string outputFilePath,
            string startingProducer);
    }

    public class App : IApp
    {
        private readonly IGraphvizDotDiagramGenerator graphvizDotDiagramGenerator;
        private readonly IMessageFlowSymbolCollector messageFlowSymbolCollector;
        private readonly IMessageFlowSymbolConverter messageFlowSymbolConverter;
        private readonly IGraphWalkerAlgorithms graphWalkerAlgorithms;

        public App(IGraphvizDotDiagramGenerator graphvizDotDiagramGenerator, IMessageFlowSymbolCollector messageFlowSymbolCollector,
            IMessageFlowSymbolConverter messageFlowSymbolConverter, IGraphWalkerAlgorithms graphWalkerAlgorithms)
        {
            this.graphvizDotDiagramGenerator = graphvizDotDiagramGenerator;
            this.messageFlowSymbolCollector = messageFlowSymbolCollector;
            this.messageFlowSymbolConverter = messageFlowSymbolConverter;
            this.graphWalkerAlgorithms = graphWalkerAlgorithms;
        }

        public async Task Run(string solutionFilePath, string inputDataFile, string outputFilePath,
            string startingProducer)
        {
            try
            {
                MessageFlowGraph messageFlowGraph;

                if (!string.IsNullOrEmpty(solutionFilePath))
                {
                    var messageFlowSymbols = await messageFlowSymbolCollector.Collect(solutionFilePath);

                    messageFlowGraph = messageFlowSymbolConverter.ConvertToGraph(messageFlowSymbols);
                    using (var stream = File.Open(inputDataFile, FileMode.Create))
                    {
                        messageFlowGraph.SerializeToBinary(stream);
                    }
                }
                else
                {
                    using (var stream = File.Open(inputDataFile, FileMode.Open))
                    {
                        messageFlowGraph = stream.DeserializeFromBinary<MessageHandlerDefinition, TaggedEdge<MessageHandlerDefinition, MessageDefinition>,
                            MessageFlowGraph>();
                    }
                }

                var graphToVisualize = messageFlowGraph;
                var startingMessageHandlerVertex =
                    messageFlowGraph.Vertices.FirstOrDefault(vertex => vertex.FullClassName == startingProducer);
                if (startingMessageHandlerVertex != null)
                {
                    Console.WriteLine("Walking the graph...");
                    graphToVisualize = graphWalkerAlgorithms.WalkOutEdgesFrom(messageFlowGraph, startingMessageHandlerVertex);
                }

                Console.WriteLine("Generating diagram....");
                string graphRepresentation = graphvizDotDiagramGenerator.Generate(graphToVisualize, startingMessageHandlerVertex);

                Console.WriteLine("Writing diagram to output SVG file....");
                var graph = RootGraph.FromDotString(graphRepresentation);
                graph.ComputeLayout();
                graph.ToSvgFile(outputFilePath);

                Console.WriteLine("Finished.");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
