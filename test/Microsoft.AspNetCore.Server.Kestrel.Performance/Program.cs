// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class Program
    {
        private static IEnumerable<Type> _allBenchmarks = new[]
        {
            typeof(RequestParsing),
            typeof(Writing),
            typeof(PipeThroughput),
            typeof(KnownStrings)
        };

        private static Dictionary<string, IConfig> _configurations = new Dictionary<string, IConfig>(StringComparer.OrdinalIgnoreCase)
        {
            { "Default" , new CoreConfig() },
            { "Fast" , new FastConfig() }
        };

        public static int Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.HelpOption("-?|-h|--help");

            var scenarioTypeOption = app.Option("-t", "Scenario type", CommandOptionType.SingleValue);
            var scenarioMethodOption = app.Option("-m", "Scenario method", CommandOptionType.SingleValue);
            var configurationOption = app.Option("-r", "Configuration name", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                var type = scenarioTypeOption.HasValue() ? scenarioTypeOption.Value() : null;
                var method = scenarioMethodOption.HasValue() ? scenarioMethodOption.Value() : null;
                var configuration = configurationOption.HasValue() ? configurationOption.Value() : "Default";
                return Run(type, method, configuration);
            });

            return app.Execute(args);
        }

        private static int Run(string type, string method, string configuration)
        {
            IConfig config = null;
            if (configuration != null)
            {
                config = _configurations[configuration];
            }

            var types = _allBenchmarks;
            if (type != null)
            {
                types = types.Where(t => t.Name.IndexOf(type, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            }

            var benchmarks = types.SelectMany(t => BenchmarkConverter.TypeToBenchmarks(t, config));

            if (method != null)
            {
                benchmarks = benchmarks.Where(b => b.Target.Method.Name.IndexOf(method, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var summary = BenchmarkRunner.Run(benchmarks.ToArray(), config);
            Console.WriteLine(summary);
            Console.ReadLine();
            return 0;
        }
    }
}
