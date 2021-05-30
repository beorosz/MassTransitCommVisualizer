using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using MassTransitCommVisualizer.Model;
using QuikGraph;

namespace MassTransitCommVisualizer
{
    public interface IGraphvizDotDiagramGenerator
    {
        string Generate(IEdgeListGraph<MessageHandlerClass, TaggedEdge<MessageHandlerClass, MessageClass>> graph);

        string GenerateFromProducer(AdjacencyGraph<MessageHandlerClass, TaggedEdge<MessageHandlerClass, MessageClass>> graph,
            string startingProducerFullClassName);
    }

    public class GraphvizDotDiagramGenerator : IGraphvizDotDiagramGenerator
    {
        private List<string> etrModulesList;

        private Color[] colorsForModules = {
            Color.LightBlue,
            Color.DarkCyan,
            Color.SandyBrown,
            Color.GreenYellow,
            Color.LightPink,
            Color.Green,
            Color.DarkGray,
            Color.Magenta,
            Color.Aquamarine,
            Color.White
        };

        public GraphvizDotDiagramGenerator()
        {
            etrModulesList = new List<string>();
        }

        public string Generate(IEdgeListGraph<MessageHandlerClass, TaggedEdge<MessageHandlerClass, MessageClass>> graph)
        {
            etrModulesList = CollectModuleNames(graph.Vertices);

            var dotDiagram = new StringBuilder();
            AppendMainGraphDefinitionStart(dotDiagram);

            AppendMessageHandlersByModules(graph.Vertices, dotDiagram);

            dotDiagram.AppendLine();

            foreach (var edge in graph.Edges)
            {
                var namespaceClassNameTuple = SplitFullClassName(edge.Tag.FullClassName);
                dotDiagram.AppendLine(
                    $"\t\"{edge.Source.FullClassName}\" -> \"{edge.Target.FullClassName}\" [label=\"{namespaceClassNameTuple.ClassName}\", fontsize=10.0]");
            }

            AppendMainGraphDefinitionEnd(dotDiagram);

            return dotDiagram.ToString();
        }

        public string GenerateFromProducer(AdjacencyGraph<MessageHandlerClass, TaggedEdge<MessageHandlerClass, MessageClass>> graph,
            string startingProducerFullClassName)
        {
            etrModulesList = CollectModuleNames(graph.Vertices);

            var startingVertex =
                graph.Vertices.First(vertex => vertex.FullClassName == startingProducerFullClassName);

            if (startingVertex == null)
            {
                var emptyDiagram = new StringBuilder();
                AppendMainGraphDefinitionStart(emptyDiagram);
                AppendMainGraphDefinitionEnd(emptyDiagram);
                
                return emptyDiagram.ToString();
            }

            var dotDiagram = new StringBuilder();
            AppendMainGraphDefinitionStart(dotDiagram, "TB");

            dotDiagram.AppendLine();

            var edgeDefinitionCollection = new List<EdgeDefinition>();
            WalkTheGraphOutEdgesRecursively(graph, startingVertex, edgeDefinitionCollection);

            var vertices = edgeDefinitionCollection
                .Select(edge => edge.Source)
                .Union(edgeDefinitionCollection.Select(edge => edge.Target))
                .Distinct(new MessageHandlerClass.MessageHandlerClassComparer())
                .ToArray();

            AppendMessageHandlersByModules(vertices, dotDiagram, startingVertex);

            foreach (var edgeDefinition in edgeDefinitionCollection)
            {
                dotDiagram.AppendLine(
                    $"\t\"{edgeDefinition.Source.FullClassName}\" -> \"{edgeDefinition.Target.FullClassName}\" [label=\"{edgeDefinition.Edge.FullClassName}\", fontsize=10.0]");
            }

            AppendMainGraphDefinitionEnd(dotDiagram);

            return dotDiagram.ToString();
        }

        private void AppendMessageHandlersByModules(IEnumerable<MessageHandlerClass> vertices, StringBuilder dotDiagram, 
            MessageHandlerClass startingVertex = null)
        {
            foreach (var verticesByModules in vertices.GroupBy(vertex => vertex.ModuleName))
            {
                AppendSubgraphDefinitionStart(dotDiagram, verticesByModules.Key);
                foreach (var vertex in verticesByModules)
                {
                    var emphasizeMessageHandler = vertex.Equals(startingVertex);
                    dotDiagram.AppendLine($"\t\t\"{vertex.FullClassName}\" [label={GetNodeHtmlRepresentation(vertex, emphasizeMessageHandler)}]");
                }

                AppendSubgraphDefinitionEnd(dotDiagram);
            }
        }

        private void AppendSubgraphDefinitionEnd(StringBuilder dotDiagram)
        {
            dotDiagram.AppendLine("\t}");
        }

        private void WalkTheGraphOutEdgesRecursively(
            AdjacencyGraph<MessageHandlerClass, TaggedEdge<MessageHandlerClass, MessageClass>> graph,
            MessageHandlerClass vertex, IList<EdgeDefinition> edgeDefinitionCollection)
        {
            // if we've already visited the vertex, skip it to avoid infinite cycle
            if (edgeDefinitionCollection.Any(edgeDefinition => edgeDefinition.Source.Equals(vertex)))
            {
                return;
            }

            foreach (var outEdge in graph.OutEdges(vertex))
            {
                var edgeDefinition = new EdgeDefinition(outEdge.Source, outEdge.Target, outEdge.Tag);
                edgeDefinitionCollection.AddIfNotExist(edgeDefinition);
                WalkTheGraphOutEdgesRecursively(graph, outEdge.Target, edgeDefinitionCollection);
            }
        }

        private void AppendMainGraphDefinitionEnd(StringBuilder dotDiagram)
        {
            dotDiagram.AppendLine("}");
        }

        private void AppendSubgraphDefinitionStart(StringBuilder dotDiagram, string moduleName)
        {
            dotDiagram.AppendLine($"\tsubgraph \"cluster_{moduleName}\"");
            dotDiagram.AppendLine("\t{");
            dotDiagram.AppendLine($"\t\tlabel = \"{moduleName}\"");
            dotDiagram.AppendLine("\t\tcolor = blue");
            dotDiagram.AppendLine("\t\trank=\"same\"");
        }

        private void AppendMainGraphDefinitionStart(StringBuilder dotDiagram, string rankdir = "LR")
        {
            dotDiagram.AppendLine("digraph G {");
            dotDiagram.AppendLine($"\trankdir={rankdir}");
            dotDiagram.AppendLine("\tnode [shape=plaintext]");
            dotDiagram.AppendLine();
        }

        private List<string> CollectModuleNames(IEnumerable<MessageHandlerClass> messageHandlerClassCollection)
        {
            var result = new List<string>();
            foreach (var messageHandlerInformation in messageHandlerClassCollection)
            {
                result.Add(messageHandlerInformation.ModuleName);
            }

            return result.Distinct().ToList();
        }

        private string GetNodeHtmlRepresentation(MessageHandlerClass messageHandlerClass, bool emphasizeMessageHandler)
        {
            var namespaceColor = GetNamespaceColor(messageHandlerClass);
            var nodeName = SplitFullClassName(messageHandlerClass.FullClassName);
            var bgColor = emphasizeMessageHandler ? "pink" : "white";

            return $@"<
                <TABLE BORDER=""0"" CELLBORDER=""1"" CELLSPACING=""0"">
                <TR><TD BGCOLOR = ""{ColorTranslator.ToHtml(namespaceColor)}"" >{nodeName.NamespaceName}</TD></TR>
                <TR><TD PORT=""out"" BGCOLOR=""{bgColor}"">{nodeName.ClassName}</TD></TR>
                </TABLE>>";
        }

        private (string NamespaceName, string ClassName) SplitFullClassName(string fullName)
        {
            var lastDotIndex = fullName.LastIndexOf('.');

            return (fullName.Substring(0, lastDotIndex), fullName.Substring(lastDotIndex + 1));
        }

        private Color GetNamespaceColor(MessageHandlerClass messageHandlerClass)
        {
            var moduleIndex = etrModulesList.FindIndex(name => name == messageHandlerClass.ModuleName);

            return moduleIndex == -1 ? Color.Red : colorsForModules[moduleIndex % colorsForModules.Length];
        }
    }
}
