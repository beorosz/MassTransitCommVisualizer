using System;
using System.Collections.Generic;
using System.Text;

namespace MassTransitCommVisualizer
{
    public class UmlStateDiagramGenerator : DiagramGeneratorBase
    {
        private const string startUml = @"@startuml";
        private const string endUml = @"@enduml";
        private const string hideEmptyDescriptionCommand = @"hide empty description";

        public static void Generate(Dictionary<string, string[]> producersWithMessages, Dictionary<string, string[]> consumersWithMessages)
        {
            var umlStateDiagram = new StringBuilder();

            var producerToConsumerMessages = new List<(string producer, string consumer, string message)>();

            umlStateDiagram.AppendLine(startUml);
            umlStateDiagram.AppendLine(hideEmptyDescriptionCommand);
            umlStateDiagram.AppendLine();

            foreach (var producerWithMessages in producersWithMessages)
            {
                foreach (var message in producerWithMessages.Value)
                {
                    var consumers = LookupMessageConsumerClasses(message, consumersWithMessages);
                    foreach (var consumer in consumers)
                    {
                        umlStateDiagram.AppendLine($"[{SplitFullClassName(producerWithMessages.Key)}] ..> [{SplitFullClassName(consumer)}] : {SplitFullClassName(message)}");
                    }
                }
            }


            umlStateDiagram.AppendLine();
            umlStateDiagram.AppendLine(endUml);
            var result = umlStateDiagram.ToString();
            Console.WriteLine(result);

            throw new NotImplementedException("Unfinished generator.");
        }
    }
}
