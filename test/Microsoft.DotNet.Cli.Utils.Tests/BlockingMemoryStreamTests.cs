﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Cli.Utils
{
    public class BlockingMemoryStreamTests : TestBase
    {
        /// <summary>
        /// Tests reading a bigger buffer than what is available.
        /// </summary>
        [Fact]
        public void ReadBiggerBuffer()
        {
            using (var stream = new BlockingMemoryStream())
            {
                stream.Write(new byte[] { 1, 2, 3 }, 0, 3);

                byte[] buffer = new byte[10];
                int count = stream.Read(buffer, 0, buffer.Length);
                Assert.Equal(3, count);
                Assert.Equal(1, buffer[0]);
                Assert.Equal(2, buffer[1]);
                Assert.Equal(3, buffer[2]);
            }
        }

        /// <summary>
        /// Tests reading smaller buffers than what is available.
        /// </summary>
        [Fact]
        public void ReadSmallerBuffers()
        {
            using (var stream = new BlockingMemoryStream())
            {
                stream.Write(new byte[] { 1, 2, 3, 4 }, 0, 4);
                stream.Write(new byte[] { 5, 6, 7, 8, 9 }, 0, 5);

                byte[] buffer = new byte[3];

                int count = stream.Read(buffer, 0, buffer.Length);
                Assert.Equal(3, count);
                Assert.Equal(1, buffer[0]);
                Assert.Equal(2, buffer[1]);
                Assert.Equal(3, buffer[2]);

                count = stream.Read(buffer, 0, buffer.Length);
                Assert.Equal(1, count);
                Assert.Equal(4, buffer[0]);

                count = stream.Read(buffer, 0, buffer.Length);
                Assert.Equal(3, count);
                Assert.Equal(5, buffer[0]);
                Assert.Equal(6, buffer[1]);
                Assert.Equal(7, buffer[2]);

                count = stream.Read(buffer, 0, buffer.Length);
                Assert.Equal(2, count);
                Assert.Equal(8, buffer[0]);
                Assert.Equal(9, buffer[1]);
            }
        }

        /// <summary>
        /// Tests reading will block until the stream is written to.
        /// </summary>
        [Fact]
        public void TestReadBlocksUntilWrite()
        {
            using (var stream = new BlockingMemoryStream())
            {
                ManualResetEvent readerThreadExecuting = new ManualResetEvent(false);
                bool readerThreadSuccessful = false;

                Thread readerThread = new Thread(() =>
                {
                    byte[] buffer = new byte[10];
                    readerThreadExecuting.Set();
                    int count = stream.Read(buffer, 0, buffer.Length);

                    Assert.Equal(3, count);
                    Assert.Equal(1, buffer[0]);
                    Assert.Equal(2, buffer[1]);
                    Assert.Equal(3, buffer[2]);

                    readerThreadSuccessful = true;
                });

                readerThread.IsBackground = true;
                readerThread.Start();

                // ensure the thread is executing
                readerThreadExecuting.WaitOne();

                Assert.True(readerThread.IsAlive);
               
                // give it a little while to ensure it is blocking
                Thread.Sleep(10);
                Assert.True(readerThread.IsAlive);

                stream.Write(new byte[] { 1, 2, 3 }, 0, 3);

                Assert.True(readerThread.Join(1000));
                Assert.True(readerThreadSuccessful);
            }
        }
    }
}
