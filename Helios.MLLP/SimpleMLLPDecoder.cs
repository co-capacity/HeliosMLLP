﻿using System;
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
        public SimpleMLLPDecoder(byte mllpStartCharacter, byte mllpFirstEndCharacter, byte mllpLastEndCharacter)
            : this(mllpStartCharacter, mllpFirstEndCharacter, mllpLastEndCharacter, 0)
        {
        }

        public SimpleMLLPDecoder(byte mllpStartCharacter, byte mllpFirstEndCharacter, byte mllpLastEndCharacter, int minimiumMessageLength)
            : base(mllpStartCharacter, mllpFirstEndCharacter, mllpLastEndCharacter, minimiumMessageLength)
        {
        }

        protected override IByteBuf Decode(IConnection connection, IByteBuf input)
        {
            if (input.ReadableBytes < MinimiumMessageLength + 3) return null;

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

        /// <summary>
        /// Don't use this default provider if you know more about the messages you are going to receive.
        /// 
        /// If you are using HL7 encoded message, mininum lenght can be bigger as every message requires MSH section. 
        /// </summary>
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