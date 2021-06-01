using System.Collections.Generic;
using System.Linq;
using System.Xml;
using MassTransitCommVisualizer.Model;
using Microsoft.CodeAnalysis;
using QuikGraph;

namespace MassTransitCommVisualizer
{
    public interface IMessageFlowSymbolConverter
    {
        MessageFlowGraph ConvertToGraph(MessageFlowSymbols messageFlowSymbols);
    }

    public class MessageFlowSymbolConverter : IMessageFlowSymbolConverter
    {
        private readonly MessageDefinition.MessageClassComparer MessageClassComparer;

        public MessageFlowSymbolConverter()
        {
            MessageClassComparer = new MessageDefinition.MessageClassComparer();
        }

        public MessageFlowGraph ConvertToGraph(MessageFlowSymbols messageFlowSymbols)
        {
            var graph = new MessageFlowGraph(false);

            var messageFlowInformation = Map(messageFlowSymbols);
            var producerClassSentMessagesTuples = MergeDictionaries(
                    new[]
                    {
                        messageFlowInformation.MessagePublisherInfoCollection,
                        messageFlowInformation.MessageResponderInfoCollection,
                        messageFlowInformation.MessageSenderInfoCollection,
                        messageFlowInformation.ResponseSenderInfoCollection
                    })
                .ToDictionary(x => x.Key, x => x.Value);

            var consumerClassHandledMessagesTuples = MergeDictionaries(
                    new[]
                    {
                        messageFlowInformation.ConsumerImplementationInfoCollection,
                        messageFlowInformation.ResponseReceiverInfoCollection
                    })
                .ToDictionary(x => x.Key, x => x.Value);

            // We have multiple handler class instances with the same class and module name
            // due to separated producer and consumer collection
            // Those classes must be the same instance in the graph, so we use the handler collection below to pick the right class
            var allMessageHandlers = producerClassSentMessagesTuples.Keys
                .Union(consumerClassHandledMessagesTuples.Keys)
                .Distinct(new MessageHandlerDefinition.MessageHandlerClassComparer())
                .ToArray();
            
            foreach (var producerClassSentMessagesTuple in producerClassSentMessagesTuples)
            {
                var producerClassInstance = allMessageHandlers.First(handler => handler.Equals(producerClassSentMessagesTuple.Key));

                var sentMessageTypes = producerClassSentMessagesTuple.Value;
                foreach (var sentMessageType in sentMessageTypes)
                {
                    var consumerClassCollection =
                        LookupMessageConsumerClasses(sentMessageType, consumerClassHandledMessagesTuples);
                    foreach (var consumerClass in consumerClassCollection)
                    {
                        var consumerClassInstance = allMessageHandlers.First(handler => handler.Equals(consumerClass));
                        graph.AddVerticesAndEdge(
                            new TaggedEdge<MessageHandlerDefinition, MessageDefinition>(producerClassInstance, consumerClassInstance, sentMessageType));
                    }
                }
            }

            return graph;
        }

        private MessageFlowInformation Map(MessageFlowSymbols messageFlowSymbols)
        {
            var messagePublisherInfoCollection = ExtractInformationFromSymbols(messageFlowSymbols.MessagePublisherSymbolCollection);
            var messageResponderSymbolCollection = ExtractInformationFromSymbols(messageFlowSymbols.MessageResponderSymbolCollection);
            var messageSenderSymbolCollection = ExtractInformationFromSymbols(messageFlowSymbols.MessageSenderSymbolCollection);
            var responseSenderSymbolCollection = ExtractInformationFromSymbols(messageFlowSymbols.ResponseSenderSymbolCollection);

            var consumerImplementationSymbolCollection = ExtractInformationFromSymbols(messageFlowSymbols.ConsumerImplementationSymbolCollection);
            var responseReceiverSymbolCollection = ExtractInformationFromSymbols(messageFlowSymbols.ResponseReceiverSymbolCollection);


            return new MessageFlowInformation(messagePublisherInfoCollection, messageResponderSymbolCollection, messageSenderSymbolCollection,
                responseSenderSymbolCollection, consumerImplementationSymbolCollection, responseReceiverSymbolCollection);
        }

        private IDictionary<TMessageHandlerType, IEnumerable<TMessageType>>
            MergeDictionaries<TMessageHandlerType, TMessageType>(IEnumerable<IDictionary<TMessageHandlerType, IEnumerable<TMessageType>>> handlersWithMessages)
        {
            var result = new Dictionary<TMessageHandlerType, IEnumerable<TMessageType>>();
            foreach (var classWithMessagesDictionary in handlersWithMessages)
            {
                foreach (var classWithMessages in classWithMessagesDictionary)
                {
                    var currentMessagesList = result.ContainsKey(classWithMessages.Key) ?
                        result[classWithMessages.Key].ToList() :
                        new List<TMessageType>();
                    currentMessagesList.AddRange(classWithMessages.Value);
                    result[classWithMessages.Key] = currentMessagesList.Distinct().ToArray();
                }
            }

            return result;
        }

        private IEnumerable<MessageHandlerDefinition> LookupMessageConsumerClasses(
            MessageDefinition message,
            IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> consumersWithMessages)
        {
            return consumersWithMessages
                .Where(keyValue => keyValue.Value.Any(msg => MessageClassComparer.Equals(msg, message)))
                .Select(keyValue => keyValue.Key)
                .Distinct(new MessageHandlerDefinition.MessageHandlerClassComparer())
                .ToArray();
        }



        private Dictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>>
            ExtractInformationFromSymbols(IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> dictionary)
        {
            return dictionary
                .ToDictionary(keyValue => MapToMessageHandlerDefinition(keyValue.Key), keyValue =>
                    keyValue.Value.Select(ConvertToMessageDefinition));

        }

        private MessageDefinition ConvertToMessageDefinition(ITypeSymbol messageTypeSymbol)
        {
            return new MessageDefinition(messageTypeSymbol.ToDisplayString());
        }

        private MessageHandlerDefinition MapToMessageHandlerDefinition(INamedTypeSymbol messageHandlerTypeSymbol)
        {
            var fullClassName = messageHandlerTypeSymbol.ToDisplayString();
            var moduleName = GetModuleName(fullClassName);
            var comment = ExtractComment(messageHandlerTypeSymbol.GetDocumentationCommentXml());

            return new MessageHandlerDefinition(fullClassName, moduleName, comment);
        }

        private string GetModuleName(string fullClassName)
        {
            var firstNamespaceParts = fullClassName.Split('.').Take(3);
            var moduleName = string.Join(".", firstNamespaceParts);

            return moduleName;
        }

        private string ExtractComment(string rawXmlComment)
        {
            if (string.IsNullOrEmpty(rawXmlComment))
            {
                return string.Empty;
            }

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(rawXmlComment);

            var summaryNode = xmlDocument.SelectSingleNode("member/summary");

            return summaryNode?.InnerText.Trim();
        }
    }
}
