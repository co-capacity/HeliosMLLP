using System;
using System.Collections.Generic;
using Helios.Buffers;
using Helios.Exceptions;
using Helios.Net;
using Helios.Serialization;
using Helios.Tracing;

namespace Helios.MLLP
{
    /// <summary>
    /// Generic mllp decoder, use <see cref="SimpleMLLPDecoder"/> if you are only going to receive one message
    /// per request/response cycle.
    /// </summary>
    public class MLLPDecoder : MessageDecoderBase
    {
        private readonly byte _mllpFirstEndCharacter;
        private readonly byte _mllpLastEndCharacter;
        private readonly byte _mllpStartCharacter;

        public MLLPDecoder(byte mllpStartCharacter, byte mllpFirstEndCharacter, byte mllpLastEndCharacter)
        {
            _mllpStartCharacter = mllpStartCharacter;
            _mllpFirstEndCharacter = mllpFirstEndCharacter;
            _mllpLastEndCharacter = mllpLastEndCharacter;
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

        private IByteBuf Decode(IConnection connection, IByteBuf input)
        {
            // we at least need to read our start character
            if (input.ReadableBytes < 1) return null;

            input.MarkReaderIndex();

            // check start byte
            if (!input.ReadByte().Equals(_mllpStartCharacter))
            {
                throw new CorruptedFrameException(string.Format("Message doesn't start with: {0}", _mllpStartCharacter));
            }

            var startMessage = input.ReaderIndex;
            var length = input.ReadableBytes;

            // search for our end characters
            for (var i = 0; i < length; i++)
            {
                if (input.ReadByte().Equals(_mllpFirstEndCharacter) && 
                    input.GetByte(input.ReaderIndex).Equals(_mllpLastEndCharacter))
                {
                    var frame = ExtractFrame(connection, input, startMessage, i);
                    input.SkipBytes(1); // advance over our last character
                    return frame;
                }
            }


            input.ResetReaderIndex();

            // not a complete frame
            return null;
        }

        private static IByteBuf ExtractFrame(IConnection connection, IByteBuf buffer, int index, int length)
        {
            var frame = connection.Allocator.Buffer(length);
            frame.WriteBytes(buffer, index, length);
            return frame;
        }

        public override IMessageDecoder Clone()
        {
            return new MLLPDecoder(_mllpStartCharacter, _mllpFirstEndCharacter, _mllpLastEndCharacter);
        }

        #region Static methods

        public static MLLPDecoder Default
        {
            get
            {
                return new MLLPDecoder(Convert.ToByte((char)11), Convert.ToByte((char)28), Convert.ToByte((char)13));
            }
        }

        #endregion
    }
}