using System.Collections.Generic;

namespace MassTransitCommVisualizer
{
    public class MessageFlowInformation
    {
        private static IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> Empty => 
            new Dictionary<MessageHandlerClass, IEnumerable<MessageClass>>();

        public IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> MessagePublisherInfoCollection { get; }
        public IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> MessageResponderInfoCollection { get; }
        public IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> MessageSenderInfoCollection { get; }
        public IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> ResponseSenderInfoCollection { get; }

        public IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> ConsumerImplementationInfoCollection { get; }
        public IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> ResponseReceiverInfoCollection { get; }

        public MessageFlowInformation(
            IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> messagePublisherInfoCollection, 
            IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> messageResponderInfoCollection, 
            IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> messageSenderInfoCollection, 
            IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> responseSenderInfoCollection, 
            IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> consumerImplementationInfoCollection, 
            IDictionary<MessageHandlerClass, IEnumerable<MessageClass>> responseReceiverInfoCollection)
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
