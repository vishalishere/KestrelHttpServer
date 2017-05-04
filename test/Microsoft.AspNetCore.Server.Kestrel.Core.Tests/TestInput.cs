// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Tests.TestHelpers;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    class TestInput : IFrameControl, IDisposable
    {
        private MemoryPool _memoryPool;
        private PipeFactory _pipeFactory;

        public TestInput()
        {
            _pipeFactory = new PipeFactory();

            var innerContext = new FrameContext
            {
                ServiceContext = new TestServiceContext(),
                ConnectionInformation = new MockConnectionInformation
                {
                    PipeFactory = _pipeFactory
                }
            };

            FrameContext = new Frame<object>(null, innerContext);
            FrameContext.FrameControl = this;

            _memoryPool = new MemoryPool();
            Pipe = _pipeFactory.Create();
            FrameContext.Input = Pipe.Reader;
        }

        public IPipe Pipe { get;  }

        public PipeFactory PipeFactory => _pipeFactory;

        public Frame FrameContext { get; set; }

        public void Add(string text)
        {
            var data = Encoding.ASCII.GetBytes(text);
            Pipe.Writer.WriteAsync(data).Wait();
        }

        public void ProduceContinue()
        {
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void End(ProduceEndType endType)
        {
        }

        public void Abort()
        {
        }

        public void Write(ArraySegment<byte> data, Action<Exception, object> callback, object state)
        {
        }

        void IFrameControl.ProduceContinue()
        {
        }

        void IFrameControl.Write(ArraySegment<byte> data)
        {
        }

        Task IFrameControl.WriteAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            return TaskCache.CompletedTask;
        }

        void IFrameControl.Flush()
        {
        }

        Task IFrameControl.FlushAsync(CancellationToken cancellationToken)
        {
            return TaskCache.CompletedTask;
        }

        public void Dispose()
        {
            _pipeFactory.Dispose();
            _memoryPool.Dispose();
        }
    }
}

