using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MassTransitCommVisualizer
{
    public class GraphvizDotDiagramGenerator : DiagramGeneratorBase
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

        public static string Generate(Dictionary<string, string[]> producerClassSentMessagesTuples, Dictionary<string, string[]> consumerClassHandledMessagesTuples)
        {
            etrModulesList = ExtractModuleNames(producerClassSentMessagesTuples.Keys.Union(consumerClassHandledMessagesTuples.Keys));
            var classListByModules = new ConcurrentDictionary<string, IList<string>>();
            var messages = new List<(string ProducerClassName, string ConsumerClassName, string MessageClassName)>();

            foreach (var producerClassSentMessagesTuple in producerClassSentMessagesTuples)
            {
                var producerClassName = producerClassSentMessagesTuple.Key;
                var sentMessageTypesByProducer = producerClassSentMessagesTuple.Value;
                foreach (var sentMessageType in sentMessageTypesByProducer)
                {
                    var consumerClassNames = LookupMessageConsumerClasses(sentMessageType, consumerClassHandledMessagesTuples);
                    if (consumerClassNames.Any())
                    {
                        var moduleName = GetModuleName(producerClassName);
                        classListByModules.AddOrUpdate(moduleName,
                            new List<string> { producerClassName },
                            (key, existingValues) =>
                            {
                                existingValues.Add(producerClassName);
                                return existingValues;
                            });
                    }
                    foreach (var consumerClassName in consumerClassNames)
                    {
                        var moduleName = GetModuleName(consumerClassName);
                        classListByModules.AddOrUpdate(moduleName,
                            new List<string> { consumerClassName },
                            (key, existingValues) =>
                            {
                                existingValues.Add(consumerClassName);
                                return existingValues;
                            });
                        messages.Add((producerClassName, consumerClassName, sentMessageType));
                    }
                }
            }

            var umlStateDiagram = GenerateUmlStateDiagram(classListByModules, messages);
            var graphStringRepresentation = umlStateDiagram.ToString();

            return graphStringRepresentation;
        }

        private static StringBuilder GenerateUmlStateDiagram(ConcurrentDictionary<string, IList<string>> classListByModules, List<(string ProducerClassName, string ConsumerClassName, string MessageClassName)> messages)
        {
            var umlStateDiagram = new StringBuilder();
            umlStateDiagram.AppendLine("digraph G {");
            umlStateDiagram.AppendLine("\trankdir=LR");
            umlStateDiagram.AppendLine("\tnode [shape=plaintext]");
            umlStateDiagram.AppendLine("\tsplines=ortho");
            umlStateDiagram.AppendLine();

            int i = 1;
            foreach (var moduleCluster in classListByModules)
            {
                umlStateDiagram.AppendLine($"\tsubgraph \"cluster_{moduleCluster.Key}\"");
                umlStateDiagram.AppendLine("\t{");
                umlStateDiagram.AppendLine($"\t\tlabel = \"{moduleCluster.Key}\"");
                umlStateDiagram.AppendLine($"\t\tcolor = blue");
                umlStateDiagram.AppendLine($"\t\trank=\"same\"");
                foreach (var className in moduleCluster.Value)
                {
                    umlStateDiagram.AppendLine($"\t\t\"{className}\" [label={GetNodeHtmlRepresentation(className)}]");
                }

                umlStateDiagram.AppendLine("\t}");
            }

            umlStateDiagram.AppendLine();

            foreach (var msg in messages)
            {
                var namespaceClassNameTuple = SplitFullClassName(msg.MessageClassName);
                umlStateDiagram.AppendLine(
                    $"\t\"{msg.ProducerClassName}\" -> \"{msg.ConsumerClassName}\" [label=\"{namespaceClassNameTuple.ClassName}\", fontsize=10.0]");
            }

            umlStateDiagram.AppendLine("}");
            return umlStateDiagram;
        }

        private static List<string> ExtractModuleNames(IEnumerable<string> fullClassNames)
        {
            var result = new List<string>();
            foreach (var etrFullClassName in fullClassNames)
            {
                var moduleName = GetModuleName(etrFullClassName);
                result.Add(moduleName);
            }

            return result.Distinct().ToList();
        }

        private static string GetModuleName(string fullClassName)
        {
            var firstNamespaceParts = fullClassName.Split('.').Take(3);
            var moduleName = string.Join(".", firstNamespaceParts);

            return moduleName;
        }

        private static string GetNodeHtmlRepresentation(string fullClassName)
        {
            var namespaceColor = GetNamespaceColor(fullClassName);
            var nodeName = SplitFullClassName(fullClassName);

            return $@"<
                <TABLE BORDER=""0"" CELLBORDER=""1"" CELLSPACING=""0"">
                <TR><TD BGCOLOR = ""{ColorTranslator.ToHtml(namespaceColor)}"" >{nodeName.NamespaceName}</TD></TR>
                <TR><TD PORT=""out"" BGCOLOR=""white"">{nodeName.ClassName}</TD></TR>
                </TABLE>>";
        }

        private static Color GetNamespaceColor(string fullClassName)
        {
            var moduleName = GetModuleName(fullClassName);
            var moduleIndex = etrModulesList.FindIndex(name => name == moduleName);

            return moduleIndex == -1 ? Color.Red : colorsForModules[moduleIndex % colorsForModules.Length];
        }
    }
}
