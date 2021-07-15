// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using CommandLine;

using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.IL
{
    [Verb("analyze", HelpText = "Analyze one or more binary files for security and correctness issues.")]
    public class AnalyzeOptions : AnalyzeOptionsBase
    {
        [Option(
            "trace",
            Separator = ';',
            Default = null,
            HelpText = "Execution traces, expressed as a semicolon-delimited list, that " +
                       "should be emitted to the console and log file (if appropriate). " +
                       "Valid values: PdbLoad.")]
        public override IEnumerable<string> Traces { get; set; }

        [Option(
            "sympath",
            HelpText = "Symbols path value, e.g., Cache*c:\\symbols;SRV*http://msdl.microsoft.com/download/symbols " +
                       "or Cache*d:\\symbols;Srv*http://symweb. See " +
                       "https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/advanced-symsrv-use for " +
                       "syntax information. Note that BinSkim will clear the _NT_SYMBOL_PATH and _NT_ALT_SYMBOL_PATH " +
                       "environment variables at runtime. Use this argument instead for specifying the symbol path." +
                       "WARNING: Be sure to specify a local file cache in the symbol path if at all possible, in order " +
                       "to avoid the possibility of significance time-to-analyze performance degradataion.")]
        public string SymbolsPath { get; internal set; }

        [Option(
            "local-symbol-directories",
            HelpText = "A set of semicolon-delimited local directory paths that will be examined when attempting to locate PDBs.")]
        public string LocalSymbolDirectories { get; internal set; }

        [Option(
            "verbose",
            HelpText = "Emit verbose output. The resulting comprehensive report is designed to provide appropriate evidence for compliance scenarios.")]
        [Obsolete("Use --level and --kind instead.")]
        public bool Verbose { get; set; }

        [Option(
            "repository-uri",
            HelpText = "RepositoryUri is required to rule BA4001 and BA4002.")]
        public string RepositoryUri { get; set; }

        [Option(
            "pipeline-name",
            HelpText = "PipelineName is required to rule BA4001 and BA4002.")]
        public string PipelineName { get; set; }
    }
}
