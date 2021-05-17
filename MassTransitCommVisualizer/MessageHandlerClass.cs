using System;
using System.Collections.Generic;

namespace MassTransitCommVisualizer
{
    [Serializable]
    public class MessageHandlerClass
    {
        public string FullClassName { get; }
        public string ModuleName { get; }

        public MessageHandlerClass(string fullClassName, string moduleName)
        {
            FullClassName = fullClassName;
            ModuleName = moduleName;
        }

        public class MessageHandlerClassComparer : IEqualityComparer<MessageHandlerClass>
        {
            public bool Equals(MessageHandlerClass x, MessageHandlerClass y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.FullClassName == y.FullClassName;
            }

            public int GetHashCode(MessageHandlerClass obj)
            {
                return (obj.FullClassName != null ? obj.FullClassName.GetHashCode() : 0);
            }
        }
    }
}
