using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Helios.Buffers;
using Helios.MLLP.Test;
using Helios.Net;

namespace Helios.MLLP.Performance.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            TimeOperations(1024*8);
        }


        private static void TimeOperations(int messageSize = 512)
        {
            long nanosecPerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;
            const long numIterations = 10000;

            // Define the operation title names.
            String[] operationNames = {"MLLPEncoder", "SimpleMLLPDecoder", "MLLPDecoder"};

            // create the message
            var binaryContent = Encoding.ASCII.GetBytes(new String('c', messageSize));
            var msgRaw = ByteBuffer.AllocateDirect(binaryContent.Length).WriteBytes(binaryContent);
            var msgEncoded = ByteBuffer.AllocateDirect(binaryContent.Length + 3).WriteByte(11).WriteBytes(binaryContent);

            // create test connection
            IConnection TestConnection = new DummyConnection(UnpooledByteBufAllocator.Default);

            // Encoder = MLLP.MLLPEncoder.Default;
            var encoder = MLLPEncoder.Default;

            // simple decoder
            var simple = SimpleMLLPDecoder.Default;

            // default
            var decoder = MLLPDecoder.Default;
            decoder.MinimiumMessageLength = messageSize / 2;


            for (int operation = 0; operation <= 2; operation++)
            {
                // Define variables for operation statistics. 
                long numTicks = 0;
                long numRollovers = 0;
                long maxTicks = 0;
                long minTicks = Int64.MaxValue;
                int indexFastest = -1;
                int indexSlowest = -1;
                long milliSec = 0;

                Stopwatch time10kOperations = Stopwatch.StartNew();

                // Run the current operation 10001 times. 
                // The first execution time will be tossed 
                // out, since it can skew the average time. 

                for (int i = 0; i <= numIterations; i++)
                {
                    long ticksThisTime = 0;
                    int inputNum;
                    Stopwatch timePerParse;

                    switch (operation)
                    {
                        case 0:
                            // Parse a valid integer using 
                            // a try-catch statement. 

                            // Start a new stopwatch timer.
                            timePerParse = Stopwatch.StartNew();

                            List<IByteBuf> encodedMessages;
                            encoder.Encode(TestConnection, msgRaw, out encodedMessages);

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

                            List<IByteBuf> decodedMessages1;
                            simple.Decode(TestConnection, msgEncoded, out decodedMessages1);

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

                            List<IByteBuf> decodedMessages2;
                            decoder.Decode(TestConnection, msgEncoded, out decodedMessages2);

                            // Stop the timer, and save the 
                            // elapsed ticks for the operation.
                            timePerParse.Stop();
                            ticksThisTime = timePerParse.ElapsedTicks;
                            break;
                        default:
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
                }

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
                //Console.ReadLine();
            }
        }
    }
}
