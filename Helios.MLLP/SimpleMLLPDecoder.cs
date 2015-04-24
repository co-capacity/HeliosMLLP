using System;
using Helios.Buffers;
using Helios.Exceptions;
using Helios.Net;
using Helios.Serialization;

namespace Helios.MLLP
{
    /// <summary>
    /// Use this decoder if you are sure only ONE message will be send per request/response cycle.
    /// It will be faster for large messages then <see cref="MLLPDecoder"/> 
    /// </summary>
    public class SimpleMLLPDecoder : MLLPDecoderBase
    {
        public SimpleMLLPDecoder(byte mllpStartCharacter, byte mllpFirstEndCharacter, byte mllpLastEndCharacter) : base(mllpStartCharacter, mllpFirstEndCharacter, mllpLastEndCharacter)
        {
        }

        protected override IByteBuf Decode(IConnection connection, IByteBuf input)
        {
            if (input.ReadableBytes < MinimiumMessageLength) return null;

            input.MarkReaderIndex();

            // check start byte
            if (!input.ReadByte().Equals(MLLPStartCharacter))
            {
                throw new CorruptedFrameException(string.Format("Message doesn't start with: {0}", MLLPStartCharacter));
            }

            // check if we have a complete frame
            if (input.GetByte(input.ReadableBytes).Equals(MLLPLastEndCharacter) &&
                input.GetByte(input.ReadableBytes - 1).Equals(MLLPFirstEndCharacter))
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

        public override IMessageDecoder Clone()
        {
            return new SimpleMLLPDecoder(MLLPStartCharacter, MLLPFirstEndCharacter, MLLPLastEndCharacter);
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