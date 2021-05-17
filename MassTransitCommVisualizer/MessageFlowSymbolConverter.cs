using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using QuikGraph;

namespace MassTransitCommVisualizer
{
    public static class MessageFlowSymbolConverter
    {
        private static readonly MessageClass.MessageClassComparer MessageClassComparer;

        static MessageFlowSymbolConverter()
        {
            MessageClassComparer = new MessageClass.MessageClassComparer();
        }

        public static AdjacencyGraph<MessageHandlerClass, TaggedEdge<MessageHandlerClass, MessageClass>>
            ConvertToGraph(MessageFlowSymbols messageFlowSymbols)
        {
            var graph = new AdjacencyGraph<MessageHandlerClass, TaggedEdge<MessageHandlerClass, MessageClass>>();
            var messageFlowInformation = Convert(messageFlowSymbols);
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
          
            foreach (var producerClassSentMessagesTuple in producerClassSentMessagesTuples)
            {
                var producerClass = producerClassSentMessagesTuple.Key;
                var sentMessageTypes = producerClassSentMessagesTuple.Value;
                foreach (var sentMessageType in sentMessageTypes)
                {
                    var consumerClassCollection =
                        LookupMessageConsumerClasses(sentMessageType, consumerClassHandledMessagesTuples);
                    foreach (var consumerClass in consumerClassCollection)
                    {
                        graph.AddVerticesAndEdge(new TaggedEdge<MessageHandlerClass, MessageClass>(producerClass, consumerClass, sentMessageType));
                    }
                }
            }

            return graph;
        }

        private static MessageFlowInformation Convert(MessageFlowSymbols messageFlowSymbols)
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

        private static IDictionary<TMessageHandlerType, IEnumerable<TMessageType>>
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

        private static IEnumerable<MessageHandlerClass> LookupMessageConsumerClasses(
            MessageClass message,
            IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> consumersWithMessages)
        {
            return consumersWithMessages
                .Where(keyValue => keyValue.Value.Any(msg => MessageClassComparer.Equals(msg, message)))
                .Select(keyValue => keyValue.Key)
                .ToArray();
        }



        private static Dictionary<MessageHandlerClass, IEnumerable<MessageClass>>
            ExtractInformationFromSymbols(IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> dictionary)
        {
            return dictionary
                .ToDictionary(keyValue => ConvertToMessageHandlerInformation(keyValue.Key), keyValue =>
                    keyValue.Value.Select(ConvertToMessageInformation));

        }

        private static MessageClass ConvertToMessageInformation(ITypeSymbol messageTypeSymbol)
        {
            return new MessageClass(messageTypeSymbol.ToDisplayString());
        }

        private static MessageHandlerClass ConvertToMessageHandlerInformation(INamedTypeSymbol messageHandlerTypeSymbol)
        {
            var fullClassName = messageHandlerTypeSymbol.ToDisplayString();
            var moduleName = GetModuleName(fullClassName);

            return new MessageHandlerClass(fullClassName, moduleName);
        }

        private static string GetModuleName(string fullClassName)
        {
            var firstNamespaceParts = fullClassName.Split('.').Take(3);
            var moduleName = string.Join(".", firstNamespaceParts);

            return moduleName;
        }
    }
}
