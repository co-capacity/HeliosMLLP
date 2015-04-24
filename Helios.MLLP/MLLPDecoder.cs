using System;
using Helios.Buffers;
using Helios.Exceptions;
using Helios.Net;
using Helios.Serialization;

namespace Helios.MLLP
{
    /// <summary>
    /// Generic mllp decoder, use <see cref="SimpleMLLPDecoder"/> if you are only going to receive one message
    /// per request/response cycle.
    /// </summary>
    public class MLLPDecoder : MLLPDecoderBase
    {
        public MLLPDecoder(byte mllpStartCharacter, byte mllpFirstEndCharacter, byte mllpLastEndCharacter)
            : base(mllpStartCharacter, mllpFirstEndCharacter, mllpLastEndCharacter, 3)
        {
        }


        public MLLPDecoder(byte mllpStartCharacter, byte mllpFirstEndCharacter, byte mllpLastEndCharacter, int minimiumMessageLength) : base(mllpStartCharacter, mllpFirstEndCharacter, mllpLastEndCharacter, minimiumMessageLength)
        {
        }

        protected override IByteBuf Decode(IConnection connection, IByteBuf input)
        {
            // we at least need to read our controll characters
            if (input.ReadableBytes < MinimiumMessageLength) return null;

            input.MarkReaderIndex();

            // check start byte
            if (!input.ReadByte().Equals(MLLPStartCharacter))
            {
                throw new CorruptedFrameException(string.Format("Message doesn't start with: {0}", MLLPStartCharacter));
            }

            var startMessage = input.ReaderIndex;
            var length = input.ReadableBytes;

            // search for our end characters, this part doesn't scale well with bigger messages.
            for (var i = 0; i < length; i++)
            {
                if (input.ReadByte().Equals(MLLPFirstEndCharacter) && 
                    input.GetByte(input.ReaderIndex).Equals(MLLPLastEndCharacter))
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

        public override IMessageDecoder Clone()
        {
            return new MLLPDecoder(MLLPStartCharacter, MLLPFirstEndCharacter, MLLPLastEndCharacter);
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