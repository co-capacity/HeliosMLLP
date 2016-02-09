using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Helios.Buffers;
using Helios.MLLP.Test;
using Helios.Net;
using Helios.Serialization;

namespace Helios.MLLP.Performance.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            TimeOperations(1024*4);
        }


        private static void TimeOperations(int messageSize = 1024)
        {
            var nanosecPerTick = 1000L * 1000L * 1000L / Stopwatch.Frequency;
            const long numIterations = 10000;

            // Define the operation title names.
            string[] operationNames = {"MLLPEncoder", "SimpleMLLPDecoder", "MLLPDecoder"};

            // create the message
            var binaryContent = Encoding.ASCII.GetBytes(new string('c', messageSize-3));
            var msgRaw = ByteBuffer.AllocateDirect(binaryContent.Length).WriteBytes(binaryContent);
            var msgEncoded = ByteBuffer.AllocateDirect(binaryContent.Length+3).WriteByte(11).WriteBytes(binaryContent).WriteByte(28).WriteByte(13);

            // create test connection
            IConnection testConnection = new DummyConnection(UnpooledByteBufAllocator.Default);

            // Encoder = MLLP.MLLPEncoder.Default;
            var encoder = MLLPEncoder.Default;

            // simple decoder
            var simple = SimpleMLLPDecoder.Default;

            // default
            var decoder = MLLPDecoder.Default;

            for (int operation = 0; operation <= 3; operation++)
            {
                // Define variables for operation statistics. 
                long numTicks = 0;
                long numRollovers = 0;
                long maxTicks = 0;
                var minTicks = Int64.MaxValue;
                var indexFastest = -1;
                var indexSlowest = -1;
                long milliSec = 0;

                // ReSharper disable once InconsistentNaming
                var time10kOperations = Stopwatch.StartNew();

                // Run the current operation 10001 times. 
                // The first execution time will be tossed 
                // out, since it can skew the average time. 

                for (var i = 0; i <= numIterations; i++)
                {
                    long ticksThisTime = 0;
                    Stopwatch timePerParse;


                    msgEncoded.MarkWriterIndex();
                    switch (operation)
                    {
                        case 0:
                            // Parse a valid integer using 
                            // a try-catch statement. 

                            // Start a new stopwatch timer.
                            timePerParse = Stopwatch.StartNew();

                            List<IByteBuf> encodedMessages;
                            encoder.Encode(testConnection, msgRaw, out encodedMessages);

                            // Stop the timer, and save the 
                            // elapsed ticks for the operation.

                            timePerParse.Stop();
                            ticksThisTime = timePerParse.ElapsedTicks;
                            break;
                        case 1:
                            // Parse a valid integer using 
                            // the TryParse statement. 

                            // Start a new stopwatch timer.
                            timePerParse = Stopwatch.StartNew();

                            SplitLargeMessage(simple, testConnection, msgEncoded);

                            // Stop the timer, and save the 
                            // elapsed ticks for the operation.
                            timePerParse.Stop();
                            ticksThisTime = timePerParse.ElapsedTicks;
                            break;
                        case 2:
                            // Parse an invalid value using 
                            // a try-catch statement. 

                            // Start a new stopwatch timer.
                            timePerParse = Stopwatch.StartNew();

                            SplitLargeMessage(decoder, testConnection, msgEncoded);

                            // Stop the timer, and save the 
                            // elapsed ticks for the operation.
                            timePerParse.Stop();
                            ticksThisTime = timePerParse.ElapsedTicks;
                            break;
                    }

                    // Skip over the time for the first operation, 
                    // just in case it caused a one-time 
                    // performance hit. 
                    if (i == 0)
                    {
                        time10kOperations.Reset();
                        time10kOperations.Start();
                    }
                    else
                    {

                        // Update operation statistics 
                        // for iterations 1-10001. 
                        if (maxTicks < ticksThisTime)
                        {
                            indexSlowest = i;
                            maxTicks = ticksThisTime;
                        }
                        if (minTicks > ticksThisTime)
                        {
                            indexFastest = i;
                            minTicks = ticksThisTime;
                        }
                        numTicks += ticksThisTime;
                        if (numTicks < ticksThisTime)
                        {
                            // Keep track of rollovers.
                            numRollovers++;
                        }
                    }
                    msgEncoded.SetReaderIndex(0);
                    msgRaw.SetReaderIndex(0);
                }
                msgEncoded.ResetWriterIndex();

                // Display the statistics for 10000 iterations.

                time10kOperations.Stop();
                milliSec = time10kOperations.ElapsedMilliseconds;

                Console.WriteLine();
                Console.WriteLine("{0} Summary:", operationNames[operation]);
                Console.WriteLine("  Slowest time:  #{0}/{1} = {2} ticks",
                    indexSlowest, numIterations, maxTicks);
                Console.WriteLine("  Fastest time:  #{0}/{1} = {2} ticks",
                    indexFastest, numIterations, minTicks);
                Console.WriteLine("  Average time:  {0} ticks = {1} nanoseconds",
                    numTicks / numIterations,
                    (numTicks * nanosecPerTick) / numIterations);
                Console.WriteLine("  Total time looping through {0} operations: {1} milliseconds",
                    numIterations, milliSec);
                Console.ReadLine();
            }
        }

        private static void SplitLargeMessage(IMessageDecoder decoder, IConnection connection, IByteBuf buffer)
        {
            var readable = buffer.ReadableBytes;
            var upper = readable / 1024;
            // simulate sending in 1024 bytes
            for (int i = 1; i < upper+1; i++)
            {
                buffer.SetWriterIndex(Math.Min(i * 1024, readable));
                List<IByteBuf> output;
                decoder.Decode(connection, buffer, out output);
            }
        }
    }
}
