using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuikGraph;
using QuikGraph.Serialization;
using Rubjerg.Graphviz;

namespace MassTransitCommVisualizer
{
    public class App
    {
        public static async Task Run(string solutionFilePath, string inputDataFile, string outputFilePath, bool interModuleCommOnly)
        {
            try
            {
                AdjacencyGraph<MessageHandlerClass, TaggedEdge<MessageHandlerClass, MessageClass>> messageFlowGraph;

                if (solutionFilePath.Any())
                {
                    var messageFlowSymbols = await MessageDataFlowCollector.Generate(solutionFilePath);

                    messageFlowGraph = MessageFlowSymbolConverter.ConvertToGraph(messageFlowSymbols);
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
                string graphRepresentation = GraphvizDotDiagramGenerator.Generate(messageFlowGraph, interModuleCommOnly);
                
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
