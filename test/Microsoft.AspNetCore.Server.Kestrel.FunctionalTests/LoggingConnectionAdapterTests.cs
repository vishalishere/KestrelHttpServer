﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class LoggingConnectionAdapterTests
    {
        [Fact]
        public async Task LoggingConnectionAdapterCanBeAddedBeforeAndAfterHttpsAdapter()
        {
            var logger = new TestApplicationErrorLogger();
            var host = new WebHostBuilder()
                .UseLoggerFactory(new KestrelTestLoggerFactory(logger))
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseConnectionLogging();
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                        listenOptions.UseConnectionLogging();
                    });
                })
            .Configure(app =>
            {
                app.Run(context =>
                {
                    context.Response.ContentLength = 12;
                    return context.Response.WriteAsync("Hello World!");
                });
            })
            .Build();

            using (host)
            {
                await host.StartAsync();

                try
                {
                    var response = await HttpClientSlim.GetStringAsync($"https://localhost:{host.GetPort()}/", validateCertificate: false)
                                                       .TimeoutAfter(TimeSpan.FromSeconds(10));

                    Assert.Equal("Hello World!", response);
                }
                catch (TimeoutException)
                {
                    Assert.False(true, string.Join(Environment.NewLine, logger.Messages.Select(m => m.Message)));
                }
            }
        }
    }
}
