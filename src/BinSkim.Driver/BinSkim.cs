﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using CommandLine;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.IL
{
    internal class BinSkim
    {
        private static int Main(string[] args)
        {
            args = EntryPointUtilities.GenerateArguments(args, new FileSystem(), new EnvironmentVariables());

            var rewrittenArgs = new List<string>(args);

            bool richResultCode = rewrittenArgs.RemoveAll(arg => arg.Equals("--rich-return-code")) == 0;

            return Parser.Default.ParseArguments<
                AnalyzeOptions,
                ExportRulesMetadataOptions,
                ExportConfigurationOptions,
                DumpOptions>(args)
              .MapResult(
                (AnalyzeOptions analyzeOptions) => new AnalyzeCommand().Run(analyzeOptions),
                (ExportRulesMetadataOptions exportRulesMetadataOptions) => new ExportRulesMetadataCommand().Run(exportRulesMetadataOptions),
                (ExportConfigurationOptions exportConfigurationOptions) => new ExportConfigurationCommand().Run(exportConfigurationOptions),
                (DumpOptions dumpOptions) => new DumpCommand().Run(dumpOptions),
                errs => richResultCode ? (int)RuntimeConditions.InvalidCommandLineOption : 1);
        }
    }
}
