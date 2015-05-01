using System;
using System.Collections.Generic;
using Helios.Buffers;
using Helios.Net;
using Helios.Serialization;
using Helios.Tracing;

namespace Helios.MLLP
{
    public abstract class MLLPDecoderBase : MessageDecoderBase
    {
        protected readonly byte MLLPFirstEndCharacter;
        protected readonly byte MLLPLastEndCharacter;
        protected readonly byte MLLPStartCharacter;
        protected readonly int MinimiumMessageLength;

        protected MLLPDecoderBase(byte mllpStartCharacter, byte mllpFirstEndCharacter, byte mllpLastEndCharacter,
            int minimiumMessageLength = 0)
        {
            if (minimiumMessageLength < 0)
            {
                throw new ArgumentOutOfRangeException("minimiumMessageLength", "should be zero or bigger");
            }
            MLLPStartCharacter = mllpStartCharacter;
            MLLPFirstEndCharacter = mllpFirstEndCharacter;
            MLLPLastEndCharacter = mllpLastEndCharacter;
            MinimiumMessageLength = minimiumMessageLength;
        }

        public override void Decode(IConnection connection, IByteBuf buffer, out List<IByteBuf> decoded)
        {
            decoded = new List<IByteBuf>();
            var obj = Decode(connection, buffer);
            while (obj != null)
            {
                decoded.Add(obj);
                HeliosTrace.Instance.DecodeSucccess(1);
                obj = Decode(connection, buffer);
            } 
        }

        protected abstract IByteBuf Decode(IConnection connection, IByteBuf input);

        protected static IByteBuf ExtractFrame(IConnection connection, IByteBuf buffer, int index, int length)
        {
            var frame = connection.Allocator.Buffer(length);
            frame.WriteBytes(buffer, index, length);
            return frame;
        }
    }
}