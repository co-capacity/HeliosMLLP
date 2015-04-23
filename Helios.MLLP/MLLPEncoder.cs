using System;
using System.Collections.Generic;
using Helios.Buffers;
using Helios.Net;
using Helios.Serialization;
using Helios.Tracing;

namespace Helios.MLLP
{
    /// <summary>
    /// Encoder that surrounds the outgoing frame with MLLP characters
    /// </summary>
    public class MLLPEncoder : MessageEncoderBase
    {
        private readonly byte _mllpFirstEndCharacter;
        private readonly byte _mllpLastEndCharacter;
        private readonly byte _mllpStartCharacter;

        public MLLPEncoder(byte mllpStartCharacter, byte mllpFirstEndCharacter, byte mllpLastEndCharacter)
        {
            _mllpStartCharacter = mllpStartCharacter;
            _mllpFirstEndCharacter = mllpFirstEndCharacter;
            _mllpLastEndCharacter = mllpLastEndCharacter;
        }

        public override void Encode(IConnection connection, IByteBuf buffer, out List<IByteBuf> encoded)
        {
            encoded = new List<IByteBuf>();
            
            var sourceByteBuf = connection.Allocator.Buffer(buffer.ReadableBytes + 3);
            sourceByteBuf.WriteByte(_mllpStartCharacter);
            sourceByteBuf.WriteBytes(buffer);
            sourceByteBuf.WriteByte(_mllpFirstEndCharacter);
            sourceByteBuf.WriteByte(_mllpLastEndCharacter);
            encoded.Add(sourceByteBuf);

            HeliosTrace.Instance.EncodeSuccess();
        }

        public override IMessageEncoder Clone()
        {
            return new MLLPEncoder(_mllpStartCharacter, _mllpFirstEndCharacter, _mllpLastEndCharacter);
        }

        #region Static methods

        public static MLLPEncoder Default
        {
            get
            {
                return new MLLPEncoder(Convert.ToByte((char)11), Convert.ToByte((char)28), Convert.ToByte((char)13));
            }
        }

        #endregion
    }
}