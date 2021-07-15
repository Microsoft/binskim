// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.IL.Sdk;
using Microsoft.CodeAnalysis.Sarif;

using Xunit;

namespace Microsoft.CodeAnalysis.BinSkim.Rules
{
    public class CompilerDataLoggerTests
    {
        [Fact]
        public void ShouldNotThrowWhenInitializedWithBadArguments()
        {
            var testCases = new[]
            {
                new
                {
                    Context = (IAnalysisContext)null,
                    RepositoryUri = (string)null,
                    PipelineName = (string)null
                },
                new
                {
                    Context = new BinaryAnalyzerContext() as IAnalysisContext,
                    RepositoryUri = (string)null,
                    PipelineName = (string)null
                },
                new
                {
                    Context = new BinaryAnalyzerContext() as IAnalysisContext,
                    RepositoryUri = "repository-uri",
                    PipelineName = "pipeline-name"
                },
            };

            foreach (var testCase in testCases)
            {
                Exception exception = Record.Exception(() => new CompilerDataLogger(testCase.Context, testCase.RepositoryUri, testCase.PipelineName));
                Assert.Null(exception);
            }
        }
    }
}
