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
            : base(mllpStartCharacter, mllpFirstEndCharacter, mllpLastEndCharacter, 0)
        {
        }


        public MLLPDecoder(byte mllpStartCharacter, byte mllpFirstEndCharacter, byte mllpLastEndCharacter, int minimiumMessageLength) : base(mllpStartCharacter, mllpFirstEndCharacter, mllpLastEndCharacter, minimiumMessageLength)
        {
        }

        protected override IByteBuf Decode(IConnection connection, IByteBuf input)
        {
            // we at least need to read our controll characters
            if (input.ReadableBytes < MinimiumMessageLength + 3) return null;

            // mark the start of our frame
            input.MarkReaderIndex();

            // check start byte
            if (!input.ReadByte().Equals(MLLPStartCharacter))
            {
                throw new CorruptedFrameException(string.Format("Message doesn't start with: {0}", MLLPStartCharacter));
            }

            var startMessage = input.ReaderIndex;
            var length = input.ReadableBytes;

            // search for our end characters, this is performance hotspot 
            // TODO: first we should skip MinimiumMessageLength
            // TODO: find other ways to speed up this search
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

            // we have to reset as our frame could get compacted away.
            input.ResetReaderIndex();

            // not a complete frame
            return null;
        }

        /// <summary>
        /// Called for every new connection
        /// <see cref="Helios.Reactor.Response.ReactorResponseChannel"/>
        /// </summary>
        /// <returns></returns>
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