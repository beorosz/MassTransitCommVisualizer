using System.Collections.Generic;
using System.Linq;

namespace MassTransitCommVisualizer
{
    public abstract class DiagramGeneratorBase
    {
        protected static (string NamespaceName, string ClassName) SplitFullClassName(string fullName)
        {
            var lastDotIndex = fullName.LastIndexOf('.');

            return (fullName.Substring(0, lastDotIndex), fullName.Substring(lastDotIndex + 1));
        }

        protected static IEnumerable<string> LookupMessageConsumerClasses(string message, Dictionary<string, string[]> consumersWithMessages)
        {
            return consumersWithMessages
                .Where(keyValue => keyValue.Value.Any(msg => msg == message))
                .Select(keyValue => keyValue.Key)
                .ToArray();
        }
    }
}
