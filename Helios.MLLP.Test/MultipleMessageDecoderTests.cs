using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helios.Buffers;
using Helios.Exceptions;
using Helios.Net;
using Helios.Serialization;
using NUnit.Framework;

namespace Helios.MLLP.Test
{
    [TestFixture]
    public class MultipleMessageDecoderTests
    {
        protected IMessageEncoder Encoder;
        protected IMessageDecoder Decoder;
        protected IConnection TestConnection = new DummyConnection(UnpooledByteBufAllocator.Default);

        [SetUp]
        public void SetUp()
        {
            Encoder = MLLP.MLLPEncoder.Default;
            Decoder = MLLPDecoder.Default;
        }

        [Test]
        public void ShouldDecodeSingleMessage()
        {
            var binaryContent = Encoding.ASCII.GetBytes("somebytes");
            var expectedBytes = binaryContent.Length;

            var data = ByteBuffer.AllocateDirect(expectedBytes).WriteBytes(binaryContent);

            List<IByteBuf> encodedMessages;
            Encoder.Encode(TestConnection, data, out encodedMessages);

            List<IByteBuf> decodedMessages;
            Decoder.Decode(TestConnection, encodedMessages[0], out decodedMessages);

            Assert.AreEqual(1, decodedMessages.Count);
            Assert.IsTrue(binaryContent.SequenceEqual(decodedMessages[0].ToArray()), String.Format("'{0}' != '{1}'", Encoding.ASCII.GetString(binaryContent), Encoding.ASCII.GetString(decodedMessages[0].ToArray())));
        }

        [Test]
        public void ShouldDecodeEmptyMessage()
        {
            var binaryContent = Encoding.ASCII.GetBytes("");
            var expectedBytes = binaryContent.Length;

            var data = ByteBuffer.AllocateDirect(expectedBytes).WriteBytes(binaryContent);

            List<IByteBuf> encodedMessages;
            Encoder.Encode(TestConnection, data, out encodedMessages);

            List<IByteBuf> decodedMessages;
            Decoder.Decode(TestConnection, encodedMessages[0], out decodedMessages);

            Assert.AreEqual(1, decodedMessages.Count);
            Assert.IsTrue(binaryContent.SequenceEqual(decodedMessages[0].ToArray()));
        }

        [Test]
        public void ShouldDecodedMultipleMessages()
        {
            var binaryContent1 = Encoding.ASCII.GetBytes("somebytes");
            var binaryContent2 = Encoding.ASCII.GetBytes("moarbytes");

            var buffer = ByteBuffer.AllocateDirect(binaryContent1.Length+binaryContent2.Length+6)
                .WriteByte(11).WriteBytes(binaryContent1).WriteByte(28).WriteByte(13)
                .WriteByte(11).WriteBytes(binaryContent2).WriteByte(28).WriteByte(13);


            List<IByteBuf> decodedMessages;
            Decoder.Decode(TestConnection, buffer, out decodedMessages);
            Assert.AreEqual(2, decodedMessages.Count);
            Assert.IsTrue(binaryContent1.SequenceEqual(decodedMessages[0].ToArray()));
            Assert.IsTrue(binaryContent2.SequenceEqual(decodedMessages[1].ToArray()));
        }

        [Test]
        public void ShouldProcessIncompleteMessages()
        {
            var binaryContent = Encoding.ASCII.GetBytes("somebytes");

            var buffer = ByteBuffer.AllocateDirect(binaryContent.Length + 3)
                .WriteByte(11).WriteBytes(binaryContent);


            List<IByteBuf> decodedMessages;
            Decoder.Decode(TestConnection, buffer, out decodedMessages);

            Assert.AreEqual(0, decodedMessages.Count);

            // complete frame and decode again.
            buffer.WriteByte(28).WriteByte(13);
            Decoder.Decode(TestConnection, buffer, out decodedMessages);

            Assert.AreEqual(1, decodedMessages.Count);
            Assert.IsTrue(binaryContent.SequenceEqual(decodedMessages[0].ToArray()));
        }

        [Test]
        public void ShouldProcessReturnFirstMessagesWhenNewFrameNotComplete()
        {
            var binaryContent1 = Encoding.ASCII.GetBytes("somebytes");
            var binaryContent2 = Encoding.ASCII.GetBytes("moarbytes");

            var buffer = ByteBuffer.AllocateDirect(binaryContent1.Length + binaryContent2.Length + 6)
                .WriteByte(11).WriteBytes(binaryContent1).WriteByte(28).WriteByte(13)
                .WriteByte(11).WriteBytes(binaryContent2);


            List<IByteBuf> decodedMessages;
            Decoder.Decode(TestConnection, buffer, out decodedMessages);

            Assert.AreEqual(1, decodedMessages.Count);
            Assert.IsTrue(binaryContent1.SequenceEqual(decodedMessages[0].ToArray()));

            // complete frame and decode again.
            buffer.WriteByte(28).WriteByte(13);
            Decoder.Decode(TestConnection, buffer, out decodedMessages);

            Assert.AreEqual(1, decodedMessages.Count);
            Assert.IsTrue(binaryContent2.SequenceEqual(decodedMessages[0].ToArray()));
        }

        [Test]
        [ExpectedException(typeof(CorruptedFrameException))]
        public void ShouldThrowExceptionWhenDecodingIncorrectFrameStart()
        {
            var binaryContent = Encoding.UTF8.GetBytes("somebytes");
            var buffer = ByteBuffer.AllocateDirect(binaryContent.Length).WriteBytes(binaryContent);

            List<IByteBuf> decodedMessages;
            Decoder.Decode(TestConnection, buffer, out decodedMessages);
        }
    }
}
