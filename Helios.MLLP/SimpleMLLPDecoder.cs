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
    /// Use this decoder if you are sure only ONE message will be send per request/response cycle.
    /// It will be faster for large messages then <see cref="MLLPDecoder"/> 
    /// </summary>
    public class SimpleMLLPDecoder : MessageDecoderBase
    {
        private readonly byte _mllpFirstEndCharacter;
        private readonly byte _mllpLastEndCharacter;
        private readonly byte _mllpStartCharacter;

        public SimpleMLLPDecoder(byte mllpStartCharacter, byte mllpFirstEndCharacter, byte mllpLastEndCharacter)
        {
            _mllpStartCharacter = mllpStartCharacter;
            _mllpFirstEndCharacter = mllpFirstEndCharacter;
            _mllpLastEndCharacter = mllpLastEndCharacter;
        }

        public override void Decode(IConnection connection, IByteBuf buffer, out List<IByteBuf> decoded)
        {
            decoded = new List<IByteBuf>();
            var obj = Decode(connection, buffer);
            if (obj != null)
            {
                decoded.Add(obj);
                HeliosTrace.Instance.DecodeSucccess(1);
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

            // check if we have a complete frame
            if (input.GetByte(input.ReadableBytes).Equals(_mllpLastEndCharacter) &&
                input.GetByte(input.ReadableBytes - 1).Equals(_mllpFirstEndCharacter))
            {
                var startMessage = input.ReaderIndex;
                var actualFrameLength = input.ReadableBytes;
                var messageLength = actualFrameLength - 2;
                var frame = ExtractFrame(connection, input, startMessage, messageLength);
                input.SetReaderIndex(startMessage + actualFrameLength);
                return frame;
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
            return new SimpleMLLPDecoder(_mllpStartCharacter, _mllpFirstEndCharacter, _mllpLastEndCharacter);
        }

        #region Static methods

        public static SimpleMLLPDecoder Default
        {
            get
            {
                return new SimpleMLLPDecoder(Convert.ToByte((char)11), Convert.ToByte((char)28), Convert.ToByte((char)13));
            }
        }

        #endregion
    }
}