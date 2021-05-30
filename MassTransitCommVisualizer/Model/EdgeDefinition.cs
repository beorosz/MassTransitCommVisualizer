using System;

namespace MassTransitCommVisualizer.Model
{
    public class EdgeDefinition : IEquatable<EdgeDefinition>
    {
        public MessageHandlerClass Source { get; }
        public MessageHandlerClass Target { get; }
        public MessageClass Edge { get; }

        public EdgeDefinition(MessageHandlerClass source, MessageHandlerClass target, MessageClass edge)
        {
            Source = source;
            Target = target;
            Edge = edge;
        }

        public bool Equals(EdgeDefinition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Source, other.Source) && Equals(Target, other.Target) && Equals(Edge, other.Edge);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EdgeDefinition) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Source != null ? Source.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Target != null ? Target.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Edge != null ? Edge.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
