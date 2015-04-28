using System;
using System.Collections.Generic;
using System.Text;
using Helios.Buffers;
using NUnit.Framework;

namespace Helios.MLLP.Test
{
    [TestFixture]
    public class SingleMessageDecoderTests : BaseMLLPTests
    {
        [SetUp]
        public override void SetUp()
        {
            Decoder = SimpleMLLPDecoder.Default;
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void MinimumMessageLengthShouldBePositive()
        {
            var obj = new SimpleMLLPDecoder(Convert.ToByte((char)11), Convert.ToByte((char)28), Convert.ToByte((char)13), -1);
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

        /// <summary>
        /// This should return on message concat of the 2 with 3 control characters in between.
        /// </summary>
        [Test]
        public override void ShouldDecodedMultipleMessages()
        {
            var binaryContent1 = Encoding.ASCII.GetBytes("somebytes");
            var binaryContent2 = Encoding.ASCII.GetBytes("moarbytes");

            var buffer = ByteBuffer.AllocateDirect(binaryContent1.Length + binaryContent2.Length + 6)
                .WriteByte(11).WriteBytes(binaryContent1).WriteByte(28).WriteByte(13)
                .WriteByte(11).WriteBytes(binaryContent2).WriteByte(28).WriteByte(13);


            List<IByteBuf> decodedMessages;
            Decoder.Decode(TestConnection, buffer, out decodedMessages);
            Assert.AreEqual(1, decodedMessages.Count);
            Assert.AreEqual(decodedMessages[0].ReadableBytes, binaryContent1.Length + binaryContent2.Length + 3);
        }

        /// <summary>
        /// This should not deliver first message and should survice compact
        /// </summary>
        [Test]
        public override void ShouldProcessReturnFirstMessagesWhenNewFrameNotComplete()
        {
            var binaryContent1 = Encoding.ASCII.GetBytes("somebytes");
            var binaryContent2 = Encoding.ASCII.GetBytes("moarbytes");

            var buffer = ByteBuffer.AllocateDirect(binaryContent1.Length + binaryContent2.Length + 6)
                .WriteByte(11).WriteBytes(binaryContent1).WriteByte(28).WriteByte(13)
                .WriteByte(11).WriteBytes(binaryContent2);


            List<IByteBuf> decodedMessages;
            Decoder.Decode(TestConnection, buffer, out decodedMessages);

            Assert.AreEqual(0, decodedMessages.Count);

            // simulate compact
            buffer.Compact();

            // complete frame and decode again.
            buffer.WriteByte(28).WriteByte(13);
            Decoder.Decode(TestConnection, buffer, out decodedMessages);

            Assert.AreEqual(1, decodedMessages.Count);
            Assert.AreEqual(decodedMessages[0].ReadableBytes, binaryContent1.Length + binaryContent2.Length + 3);
        }
    }
}
