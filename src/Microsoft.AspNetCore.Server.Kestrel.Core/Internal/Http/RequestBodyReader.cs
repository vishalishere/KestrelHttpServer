﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public class RequestBodyReader
    {
        public static readonly RequestBodyReader ZeroContentLengthClose = new EmptyRequestBodyReader();

        private readonly IPipe _pipe;

        public RequestBodyReader(IPipe pipe)
        {
            _pipe = pipe;
        }

        public async Task StartAsync(MessageBody messageBody, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                while (!messageBody.Consumed)
                {
                    var writableBuffer = _pipe.Writer.Alloc(1);
                    int bytesRead;

                    try
                    {
                        bytesRead = await messageBody.ReadAsync(writableBuffer.Buffer.GetArray(), cancellationToken);
                        writableBuffer.Advance(bytesRead);
                    }
                    finally
                    {
                        writableBuffer.Commit();
                    }

                    await writableBuffer.FlushAsync();
                }

                _pipe.Writer.Complete();
            }
            catch (Exception ex)
            {
                _pipe.Writer.Complete(ex);
            }
        }

        public void Reset()
        {
            // TODO: ensure there are no readers
            // TODO: ensure start task has finished
            _pipe.Reader.Complete();
            _pipe.Writer.Complete();
            _pipe.Reset();
        }

        public virtual async Task<int> ReadAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            while (true)
            {
                var result = await _pipe.Reader.ReadAsync();
                var readableBuffer = result.Buffer;

                try
                {
                    if (!readableBuffer.IsEmpty)
                    {
                        var count = Math.Min(result.Buffer.Length, buffer.Count);
                        readableBuffer = result.Buffer.Slice(0, count);
                        readableBuffer.CopyTo(buffer);
                        return count;
                    }
                    else if (result.IsCompleted)
                    {
                        return 0;
                    }
                }
                finally
                {
                    _pipe.Reader.Advance(readableBuffer.End);
                }
            }
        }

        public virtual async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default(CancellationToken))
        {
            while (true)
            {
                var result = await _pipe.Reader.ReadAsync();
                var readableBuffer = result.Buffer;

                try
                {
                    if (!readableBuffer.IsEmpty)
                    {
                        foreach (var memory in readableBuffer)
                        {
                            var array = memory.GetArray();
                            await destination.WriteAsync(array.Array, array.Offset, array.Count);
                        }
                    }
                    else if (result.IsCompleted)
                    {
                        return;
                    }
                }
                finally
                {
                    _pipe.Reader.Advance(readableBuffer.End);
                }
            }
        }

        public async Task Consume(CancellationToken cancellationToken = default(CancellationToken))
        {
            while (true)
            {
                var result = await _pipe.Reader.ReadAsync();
                var readableBuffer = result.Buffer;
                _pipe.Reader.Advance(readableBuffer.End);

                if (result.IsCompleted)
                {
                    return;
                }
            }
        }

        private class EmptyRequestBodyReader : RequestBodyReader
        {
            public EmptyRequestBodyReader()
                : base(null)
            {
            }

            public override Task<int> ReadAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
                => Task.FromResult(0);

            public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default(CancellationToken))
                => TaskCache.CompletedTask;
        }
    }
}
