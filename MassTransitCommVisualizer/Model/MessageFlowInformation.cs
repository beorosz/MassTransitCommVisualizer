using System.Collections.Generic;

namespace MassTransitCommVisualizer.Model
{
    public class MessageFlowInformation
    {
        public IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> MessagePublisherInfoCollection { get; }
        public IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> MessageResponderInfoCollection { get; }
        public IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> MessageSenderInfoCollection { get; }
        public IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> ResponseSenderInfoCollection { get; }

        public IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> ConsumerImplementationInfoCollection { get; }
        public IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> ResponseReceiverInfoCollection { get; }

        public MessageFlowInformation(
            IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> messagePublisherInfoCollection, 
            IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> messageResponderInfoCollection, 
            IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> messageSenderInfoCollection, 
            IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> responseSenderInfoCollection, 
            IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> consumerImplementationInfoCollection, 
            IDictionary<MessageHandlerDefinition, IEnumerable<MessageDefinition>> responseReceiverInfoCollection)
        {
            MessagePublisherInfoCollection = messagePublisherInfoCollection;
            MessageResponderInfoCollection = messageResponderInfoCollection;
            MessageSenderInfoCollection = messageSenderInfoCollection;
            ResponseSenderInfoCollection = responseSenderInfoCollection;
            ConsumerImplementationInfoCollection = consumerImplementationInfoCollection;
            ResponseReceiverInfoCollection = responseReceiverInfoCollection;
        }
    }
}
