using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MassTransitCommVisualizer.Model
{
    [Serializable]
    [DebuggerDisplay("{FullClassName}")]
    public class MessageHandlerClass : IEquatable<MessageHandlerClass>
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

        public bool Equals(MessageHandlerClass other)
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
            return Equals((MessageHandlerClass) obj);
        }

        public override int GetHashCode()
        {
            return (FullClassName != null ? FullClassName.GetHashCode() : 0);
        }
    }
}
