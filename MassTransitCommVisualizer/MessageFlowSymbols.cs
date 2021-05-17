using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MassTransitCommVisualizer
{
    public class MessageFlowSymbols
    {
        public IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> MessagePublisherSymbolCollection { get; }
        public IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> MessageResponderSymbolCollection { get; }
        public IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> MessageSenderSymbolCollection { get; }
        public IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> ResponseSenderSymbolCollection { get; }

        public IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> ConsumerImplementationSymbolCollection { get; }
        public IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> ResponseReceiverSymbolCollection { get; }

        public MessageFlowSymbols(
            IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> messagePublisherSymbolCollection, 
            IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> messageResponderSymbolCollection, 
            IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> messageSenderSymbolCollection, 
            IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> responseSenderSymbolCollection, 
            IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> consumerImplementationSymbolCollection, 
            IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> responseReceiverSymbolCollection)
        {
            MessagePublisherSymbolCollection = messagePublisherSymbolCollection;
            MessageResponderSymbolCollection = messageResponderSymbolCollection;
            MessageSenderSymbolCollection = messageSenderSymbolCollection;
            ResponseSenderSymbolCollection = responseSenderSymbolCollection;
            ConsumerImplementationSymbolCollection = consumerImplementationSymbolCollection;
            ResponseReceiverSymbolCollection = responseReceiverSymbolCollection;
        }
    }
}
