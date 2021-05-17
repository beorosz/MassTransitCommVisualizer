using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using QuikGraph;

namespace MassTransitCommVisualizer
{
    public class GraphvizDotDiagramGenerator
    {
        private static List<string> etrModulesList;

        private static Color[] colorsForModules = {
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

        public static string Generate(IEdgeListGraph<MessageHandlerClass, TaggedEdge<MessageHandlerClass, MessageClass>> graph,
            bool interModuleCommOnly)
        {
            etrModulesList = CollectModuleNames(graph.Vertices);

            var umlStateDiagram = new StringBuilder();
            umlStateDiagram.AppendLine("digraph G {");
            umlStateDiagram.AppendLine("\trankdir=LR");
            umlStateDiagram.AppendLine("\tnode [shape=plaintext]");
            //umlStateDiagram.AppendLine("\tsplines=ortho");
            umlStateDiagram.AppendLine();

            foreach (var verticesByModules in graph.Vertices.GroupBy(vertex => vertex.ModuleName))
            {
                umlStateDiagram.AppendLine($"\tsubgraph \"cluster_{verticesByModules.Key}\"");
                umlStateDiagram.AppendLine("\t{");
                umlStateDiagram.AppendLine($"\t\tlabel = \"{verticesByModules.Key}\"");
                umlStateDiagram.AppendLine($"\t\tcolor = blue");
                umlStateDiagram.AppendLine($"\t\trank=\"same\"");
                foreach (var vertex in verticesByModules)
                {
                    umlStateDiagram.AppendLine($"\t\t\"{vertex.FullClassName}\" [label={GetNodeHtmlRepresentation(vertex)}]");
                }

                umlStateDiagram.AppendLine("\t}");
            }

            umlStateDiagram.AppendLine();

            foreach (var edge in graph.Edges)
            {
                if (interModuleCommOnly && edge.Source.ModuleName == edge.Target.ModuleName)
                {
                    continue;
                }
                var namespaceClassNameTuple = SplitFullClassName(edge.Tag.FullClassName);
                umlStateDiagram.AppendLine(
                    $"\t\"{edge.Source.FullClassName}\" -> \"{edge.Target.FullClassName}\" [label=\"{namespaceClassNameTuple.ClassName}\", fontsize=10.0]");
            }

            umlStateDiagram.AppendLine("}");

            return umlStateDiagram.ToString();
        }

        private static List<string> CollectModuleNames(IEnumerable<MessageHandlerClass> messageHandlerClassCollection)
        {
            var result = new List<string>();
            foreach (var messageHandlerInformation in messageHandlerClassCollection)
            {
                result.Add(messageHandlerInformation.ModuleName);
            }

            return result.Distinct().ToList();
        }

        private static string GetNodeHtmlRepresentation(MessageHandlerClass messageHandlerClass)
        {
            var namespaceColor = GetNamespaceColor(messageHandlerClass);
            var nodeName = SplitFullClassName(messageHandlerClass.FullClassName);

            return $@"<
                <TABLE BORDER=""0"" CELLBORDER=""1"" CELLSPACING=""0"">
                <TR><TD BGCOLOR = ""{ColorTranslator.ToHtml(namespaceColor)}"" >{nodeName.NamespaceName}</TD></TR>
                <TR><TD PORT=""out"" BGCOLOR=""white"">{nodeName.ClassName}</TD></TR>
                </TABLE>>";
        }

        private static (string NamespaceName, string ClassName) SplitFullClassName(string fullName)
        {
            var lastDotIndex = fullName.LastIndexOf('.');

            return (fullName.Substring(0, lastDotIndex), fullName.Substring(lastDotIndex + 1));
        }

        private static Color GetNamespaceColor(MessageHandlerClass messageHandlerClass)
        {
            var moduleIndex = etrModulesList.FindIndex(name => name == messageHandlerClass.ModuleName);

            return moduleIndex == -1 ? Color.Red : colorsForModules[moduleIndex % colorsForModules.Length];
        }
    }
}
