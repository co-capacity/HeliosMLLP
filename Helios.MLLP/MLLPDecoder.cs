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
       private int _skipBytes;

        public MLLPDecoder(byte mllpStartCharacter, byte mllpFirstEndCharacter, byte mllpLastEndCharacter)
            : this(mllpStartCharacter, mllpFirstEndCharacter, mllpLastEndCharacter, 0)
        {
        }


        public MLLPDecoder(byte mllpStartCharacter, byte mllpFirstEndCharacter, byte mllpLastEndCharacter, int minimiumMessageLength)
            : base(mllpStartCharacter, mllpFirstEndCharacter, mllpLastEndCharacter, minimiumMessageLength)
        {
        }

        protected override IByteBuf Decode(IConnection connection, IByteBuf input)
        {
            // we at least need to read our controll characters and minimum message length
            if (input.ReadableBytes < MinimiumMessageLength + 3) return null;

            // check start byte
            if (!input.GetByte(input.ReaderIndex).Equals(MLLPStartCharacter))
            {
                throw new CorruptedFrameException(string.Format("Message doesn't start with: {0}", MLLPStartCharacter));
            }

            // mark the start of our frame and skip start character
            input.MarkReaderIndex();
            input.SkipBytes(1);

            // mark start of the message
            var startMessage = input.ReaderIndex;
            var length = input.ReadableBytes;

            // skip already read bytes or skip minimum length bits
            input.SkipBytes(_skipBytes > 0 ? _skipBytes : MinimiumMessageLength);

            // search for our end characters
            for (var i = _skipBytes; i < length; i++)
            {
                if (input.ReadByte().Equals(MLLPFirstEndCharacter) && 
                    input.GetByte(input.ReaderIndex).Equals(MLLPLastEndCharacter))
                {
                    var frame = ExtractFrame(connection, input, startMessage, i);
                    input.SkipBytes(1); // advance over our last character
                    _skipBytes = 0; // reset
                    return frame;
                }
            }

            // set skipBytes to current ReaderIndex
            _skipBytes = input.ReaderIndex - startMessage - 1;

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