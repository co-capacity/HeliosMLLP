using System;
using NUnit.Framework;

namespace Helios.MLLP.Test
{
    [TestFixture]
    public class MultipleMessageDecoderTests : BaseMLLPTests
    {
        [SetUp]
        public override void SetUp()
        {
            Decoder = MLLPDecoder.Default;
        }

        [Test]
        //[ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void MinimumMessageLengthShouldBePositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var obj = new MLLPDecoder(Convert.ToByte((char)11), Convert.ToByte((char)28), Convert.ToByte((char)13), -1);
            });
        }
    }
}
