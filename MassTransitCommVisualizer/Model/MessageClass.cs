using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MassTransitCommVisualizer.Model
{
    [Serializable]
    [DebuggerDisplay("{FullClassName}")]
    public class MessageClass : IEquatable<MessageClass>
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

        public bool Equals(MessageClass other)
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
            return Equals((MessageClass) obj);
        }

        public override int GetHashCode()
        {
            return (FullClassName != null ? FullClassName.GetHashCode() : 0);
        }
    }
}
