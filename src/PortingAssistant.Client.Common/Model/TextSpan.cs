using System;

namespace PortingAssistant.Client.Model
{
    public class TextSpan
    {
        public long? StartCharPosition { get; set; }
        public long? EndCharPosition { get; set; }
        public long? StartLinePosition { get; set; }
        public long? EndLinePosition { get; set; }

        public override bool Equals(object obj)
        {
            return obj is TextSpan pair &&
                   StartCharPosition == pair.StartCharPosition &&
                   EndCharPosition == pair.EndCharPosition &&
                   StartLinePosition == pair.StartLinePosition &&
                   EndLinePosition == pair.EndLinePosition;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartLinePosition, StartCharPosition, EndLinePosition, EndCharPosition);
        }
    }
}
