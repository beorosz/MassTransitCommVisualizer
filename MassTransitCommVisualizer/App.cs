using System;
using System.IO;
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

        public App(IGraphvizDotDiagramGenerator graphvizDotDiagramGenerator, IMessageFlowSymbolCollector messageFlowSymbolCollector,
            IMessageFlowSymbolConverter messageFlowSymbolConverter)
        {
            this.graphvizDotDiagramGenerator = graphvizDotDiagramGenerator;
            this.messageFlowSymbolCollector = messageFlowSymbolCollector;
            this.messageFlowSymbolConverter = messageFlowSymbolConverter;
        }

        public async Task Run(string solutionFilePath, string inputDataFile, string outputFilePath, 
            string startingProducer)
        {
            try
            {
                AdjacencyGraph<MessageHandlerClass, TaggedEdge<MessageHandlerClass, MessageClass>> messageFlowGraph;

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
                        messageFlowGraph = stream.DeserializeFromBinary<MessageHandlerClass, TaggedEdge<MessageHandlerClass, MessageClass>, 
                        AdjacencyGraph<MessageHandlerClass, TaggedEdge<MessageHandlerClass, MessageClass>>>();
                    }
                }

                Console.WriteLine("Generating diagram....");
                string graphRepresentation = string.IsNullOrEmpty(startingProducer) ?
                    graphvizDotDiagramGenerator.Generate(messageFlowGraph) :
                    graphvizDotDiagramGenerator.GenerateFromProducer(messageFlowGraph, startingProducer);
                
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
