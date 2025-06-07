using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using MassTransitCommVisualizer.Model;

namespace MassTransitCommVisualizer
{
    public interface IGraphvizDotDiagramGenerator
    {
        string Generate(MessageFlowGraph graph, MessageHandlerDefinition vertexForHighlight);
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

        public string Generate(MessageFlowGraph graph, MessageHandlerDefinition vertexForHighlight)
        {
            etrModulesList = CollectModuleNames(graph.Vertices);

            var dotDiagram = new StringBuilder();
            AppendMainGraphDefinitionStart(dotDiagram);

            AppendMessageHandlersByModules(graph.Vertices, dotDiagram, vertexForHighlight);

            dotDiagram.AppendLine();

            foreach (var edge in graph.Edges)
            {
                var namespaceClassNameTuple = SplitFullClassName(edge.Tag.FullClassName);
                dotDiagram.AppendLine(
                    $"\t\"{edge.Source.FullClassName}\" -> \"{edge.Target.FullClassName}\" [label=\"{namespaceClassNameTuple.ClassName}\"{GetLabelTooltip(edge.Tag.Comment)}, fontsize=10.0]");
            }

            AppendMainGraphDefinitionEnd(dotDiagram);

            return dotDiagram.ToString();
        }

        private void AppendMessageHandlersByModules(IEnumerable<MessageHandlerDefinition> vertices, StringBuilder dotDiagram, 
            MessageHandlerDefinition startingVertex = null)
        {
            foreach (var verticesByModules in vertices.GroupBy(vertex => vertex.ModuleName))
            {
                AppendSubgraphDefinitionStart(dotDiagram, verticesByModules.Key);
                foreach (var vertex in verticesByModules)
                {
                    var emphasizeMessageHandler = vertex.Equals(startingVertex);
                    dotDiagram.AppendLine($"\t\t\"{vertex.FullClassName}\" [label={GetNodeHtmlRepresentation(vertex, emphasizeMessageHandler)}{GetTooltip(vertex.Comment)}]");
                }

                AppendSubgraphDefinitionEnd(dotDiagram);
            }
        }

        private string GetTooltip(string comment)
        {
            return string.IsNullOrEmpty(comment) ? "" : $" tooltip=\"{comment}\"";
        }

        private string GetLabelTooltip(string comment)
        {
            return string.IsNullOrEmpty(comment) ? "" : $" labeltooltip=\"{comment}\"";
        }

        private void AppendSubgraphDefinitionEnd(StringBuilder dotDiagram)
        {
            dotDiagram.AppendLine("\t}");
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

        private List<string> CollectModuleNames(IEnumerable<MessageHandlerDefinition> messageHandlerClassCollection)
        {
            var result = new List<string>();
            foreach (var messageHandlerInformation in messageHandlerClassCollection)
            {
                result.Add(messageHandlerInformation.ModuleName);
            }

            return result.OrderBy(item => item).Distinct().ToList();
        }

        private string GetNodeHtmlRepresentation(MessageHandlerDefinition messageHandlerClass, bool emphasizeMessageHandler)
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

            if (lastDotIndex == -1)
            {
                return (fullName, fullName);
            }

            return (fullName.Substring(0, lastDotIndex), fullName.Substring(lastDotIndex + 1));
        }

        private Color GetNamespaceColor(MessageHandlerDefinition messageHandlerClass)
        {
            var moduleIndex = etrModulesList.FindIndex(name => name == messageHandlerClass.ModuleName);

            return moduleIndex == -1 ? Color.Red : colorsForModules[moduleIndex % colorsForModules.Length];
        }
    }
}
