using System;
using System.Collections.Generic;
using System.Text;
using Helios.Buffers;
using Helios.Net;
using Helios.Serialization;
using NUnit.Framework;

namespace Helios.MLLP.Test
{
    [TestFixture]
    public class EncoderTests
    {
        protected IMessageEncoder Encoder;
        protected IConnection TestConnection = new DummyConnection(UnpooledByteBufAllocator.Default);

        [SetUp]
        public void SetUp()
        {
            Encoder = MLLP.MLLPEncoder.Default;
        }

        [Test]
        public void TestEncoding()
        {
            var binaryContent = Encoding.ASCII.GetBytes("somebytes");
            var expectedBytes = binaryContent.Length;
            var data = ByteBuffer.AllocateDirect(expectedBytes).WriteBytes(binaryContent);

            List<IByteBuf> encodedMessages;
            Encoder.Encode(TestConnection, data, out encodedMessages);

            var encodedData = encodedMessages[0];

            // check lenght
            Assert.AreEqual(expectedBytes + 3, encodedData.ReadableBytes);

            // check frame
            Assert.AreEqual(encodedData.GetByte(0), Convert.ToByte((char)11));
            Assert.AreEqual(encodedData.GetByte(encodedData.ReadableBytes - 2), Convert.ToByte((char)28));
            Assert.AreEqual(encodedData.GetByte(encodedData.ReadableBytes - 1), Convert.ToByte((char)13));
        }

        [Test]
        public void TestCloneEncoding()
        {
            var binaryContent = Encoding.ASCII.GetBytes("somebytes");
            var expectedBytes = binaryContent.Length;
            var data = ByteBuffer.AllocateDirect(expectedBytes).WriteBytes(binaryContent);

            List<IByteBuf> encodedMessages;
            Encoder.Clone().Encode(TestConnection, data, out encodedMessages);

            var encodedData = encodedMessages[0];

            // check lenght
            Assert.AreEqual(expectedBytes + 3, encodedData.ReadableBytes);

            // check frame
            Assert.AreEqual(encodedData.GetByte(0), Convert.ToByte((char)11));
            Assert.AreEqual(encodedData.GetByte(encodedData.ReadableBytes - 2), Convert.ToByte((char)28));
            Assert.AreEqual(encodedData.GetByte(encodedData.ReadableBytes - 1), Convert.ToByte((char)13));
        }
    }
}
