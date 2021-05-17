using System;
using System.Collections.Generic;

namespace MassTransitCommVisualizer
{
    [Serializable]
    public class MessageClass
    {
        public string FullClassName { get; }

        public MessageClass(string fullClassName)
        {
            FullClassName = fullClassName;
        }

        public class MessageClassComparer : IEqualityComparer<MessageClass>
        {
            public bool Equals(MessageClass x, MessageClass y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.FullClassName == y.FullClassName;
            }

            public int GetHashCode(MessageClass obj)
            {
                return (obj.FullClassName != null ? obj.FullClassName.GetHashCode() : 0);
            }
        }
    }
}
