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
    public class SingleMessageDecoderTests
    {
        protected IMessageEncoder Encoder;
        protected IMessageDecoder Decoder;
        protected IConnection TestConnection = new DummyConnection(UnpooledByteBufAllocator.Default);

        [SetUp]
        public void SetUp()
        {
            Encoder = MLLPEncoder.Default;
            Decoder = SimpleMLLPDecoder.Default;
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

            Assert.IsTrue(binaryContent.SequenceEqual(decodedMessages[0].ToArray()));
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

            Assert.IsTrue(binaryContent.SequenceEqual(decodedMessages[0].ToArray()));
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
        public void ShouldNotDecodedMultipleMessages()
        {
            var binaryContent1 = Encoding.ASCII.GetBytes("somebytes");
            var binaryContent2 = Encoding.ASCII.GetBytes("moarbytes");

            var buffer = ByteBuffer.AllocateDirect(binaryContent1.Length + binaryContent2.Length + 6)
                .WriteByte(11).WriteBytes(binaryContent1).WriteByte(28).WriteByte(13)
                .WriteByte(11).WriteBytes(binaryContent2).WriteByte(28).WriteByte(13);


            List<IByteBuf> decodedMessages;
            Decoder.Decode(TestConnection, buffer, out decodedMessages);
            Assert.AreEqual(1, decodedMessages.Count);
            Assert.AreEqual(binaryContent1.Length + binaryContent2.Length + 3, decodedMessages[0].ReadableBytes);
//            Assert.IsTrue((binaryContent1 + binaryContent2).SequenceEqual(decodedMessages[0].ToArray()));
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
