using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MassTransitCommVisualizer.Model
{
    [Serializable]
    [DebuggerDisplay("{FullClassName}")]
    public class MessageDefinition : IEquatable<MessageDefinition>
    {
        public string FullClassName { get; }

        public MessageDefinition(string fullClassName)
        {
            FullClassName = fullClassName;
        }

        public class MessageClassComparer : IEqualityComparer<MessageDefinition>
        {
            public bool Equals(MessageDefinition x, MessageDefinition y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.FullClassName == y.FullClassName;
            }

            public int GetHashCode(MessageDefinition obj)
            {
                return (obj.FullClassName != null ? obj.FullClassName.GetHashCode() : 0);
            }
        }

        public bool Equals(MessageDefinition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return FullClassName == other.FullClassName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MessageDefinition) obj);
        }

        public override int GetHashCode()
        {
            return (FullClassName != null ? FullClassName.GetHashCode() : 0);
        }
    }
}
