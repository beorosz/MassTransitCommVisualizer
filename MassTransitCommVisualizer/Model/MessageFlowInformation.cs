using System.Collections.Generic;

namespace MassTransitCommVisualizer.Model
{
    public class MessageFlowInformation
    {
        public IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> MessageProducerInformationCollection { get; }
        public IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> MessageConsumerInformationCollection { get; }

        public MessageFlowInformation(
            IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> messageProducerInformationCollection, 
            IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> messageConsumerInformationCollection)
        {
            MessageProducerInformationCollection = messageProducerInformationCollection;
            MessageConsumerInformationCollection = messageConsumerInformationCollection;
        }
    }
}
