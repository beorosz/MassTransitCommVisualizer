using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MassTransitCommVisualizer.Model
{
    public class MessageFlowSymbols
    {
        public IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> MessageProducerSymbolCollection { get; }
        public IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> MessageConsumerSymbolCollection { get; }

        public MessageFlowSymbols(
            IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> messageProducerSymbolCollection, 
            IDictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> messageConsumerSymbolCollection)
        {
            MessageProducerSymbolCollection = messageProducerSymbolCollection;
            MessageConsumerSymbolCollection = messageConsumerSymbolCollection;
        }
    }
}
