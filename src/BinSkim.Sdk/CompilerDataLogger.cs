// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.CodeAnalysis.BinaryParsers.ProgramDatabase;
using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.CodeAnalysis.IL.Sdk
{
    public class CompilerDataLogger : ICompilerDataLogger
    {
        private const string CompilerEventName = "CompilerInformation";

        private readonly bool appInsightsRegistered;

        private readonly string sha256;
        private readonly string localPath;
        private readonly string repositoryUri;
        private readonly string pipelineName;

        private static TelemetryClient s_telemetryClient;
        private static TelemetryConfiguration s_telemetryConfiguration;

        public CompilerDataLogger(IAnalysisContext analysisContext, string repositoryUri, string pipelineName)
        {
            try
            {
                string appInsightsKey = Environment.GetEnvironmentVariable("BinskimAppInsightsKey");
                if (!string.IsNullOrEmpty(appInsightsKey) && Guid.TryParse(appInsightsKey, out _))
                {
                    Initialize(appInsightsKey);
                    appInsightsRegistered = true;
                }
            }
            catch (SecurityException)
            {
                // User does not have access to retrieve information from environment variables.
            }

            this.pipelineName = pipelineName;
            this.repositoryUri = repositoryUri;
            this.sha256 = analysisContext?.Hashes?.Sha256;
            this.localPath = analysisContext?.TargetUri?.LocalPath;
        }

        public static void Initialize(string instrumentationKey)
        {
            s_telemetryConfiguration = new TelemetryConfiguration(instrumentationKey);
            s_telemetryClient = new TelemetryClient(s_telemetryConfiguration);
        }

        public static void Flush()
        {
            s_telemetryClient?.Flush();
        }

        public void PrintHeader()
        {
            if (!appInsightsRegistered)
            {
                Console.WriteLine("RepositoryUri,PipelineName,Target,Compiler Name,Compiler BackEnd Version,Compiler FrontEnd Version,Language,Dialect,Module Name,Module Library,Hash,Error");
            }
        }

        public void Write(string compilerData, ObjectModuleDetails omDetails)
        {
            string name = omDetails?.Name?.Replace(",", "_");
            string library = omDetails?.Library?.Replace(",", ";");
            if (appInsightsRegistered)
            {
                string[] compilerDataParts = compilerData.Split(',');

                s_telemetryClient.TrackEvent(CompilerEventName, properties: new Dictionary<string, string>
                {
                    { "repositoryUri", repositoryUri ?? string.Empty },
                    { "pipelineName", pipelineName ?? string.Empty },
                    { "target", this.localPath },
                    { "compilerName", compilerDataParts[0] },
                    { "compilerBackEndVersion", compilerDataParts[1] },
                    { "compilerFrontEndVersion", compilerDataParts[2] },
                    { "language", compilerDataParts[3] },
                    { "dialect", string.Empty },
                    { "moduleName", name ?? string.Empty },
                    { "moduleLibrary", (name == library ? string.Empty : library) },
                    { "hash", this.sha256 ?? string.Empty },
                    { "error", string.Empty }
                });
            }
            else
            {
                Console.Write($"{repositoryUri},");
                Console.Write($"{pipelineName},");
                Console.Write($"{this.localPath},");
                Console.Write($"{compilerData},");
                Console.Write($",");
                Console.Write($"{name},");
                Console.Write($"{(name == library ? string.Empty : library)},");
                Console.Write($"{this.sha256},");
                Console.WriteLine();
            }
        }

        public void Write(string compilerName, string version, string language, string file)
        {
            if (appInsightsRegistered)
            {
                s_telemetryClient.TrackEvent(CompilerEventName, properties: new Dictionary<string, string>
                {
                    { "repositoryUri", repositoryUri ?? string.Empty },
                    { "pipelineName", pipelineName ?? string.Empty },
                    { "target", this.localPath },
                    { "compilerName", compilerName ?? string.Empty },
                    { "compilerBackEndVersion", version ?? string.Empty },
                    { "compilerFrontEndVersion", version ?? string.Empty },
                    { "language", language ?? string.Empty },
                    { "dialect", string.Empty },
                    { "moduleName", file ?? string.Empty },
                    { "moduleLibrary", string.Empty },
                    { "hash", this.sha256 ?? string.Empty },
                    { "error", string.Empty }
                });
            }
            else
            {
                Console.Write($"{repositoryUri},");
                Console.Write($"{pipelineName},");
                Console.Write($"{this.localPath},");
                Console.Write($"{compilerName},");
                Console.Write($"{version},");
                Console.Write($"{version},");
                Console.Write($"{language},");
                Console.Write($",");
                Console.Write($"{file},");
                Console.Write(",");
                Console.Write($"{this.sha256},");
                Console.WriteLine();
            }
        }

        public void WriteException(string errorMessage)
        {
            if (appInsightsRegistered)
            {
                s_telemetryClient.TrackEvent(CompilerEventName, properties: new Dictionary<string, string>
                {
                    { "repositoryUri", repositoryUri ?? string.Empty },
                    { "pipelineName", pipelineName ?? string.Empty },
                    { "target", this.localPath },
                    { "compilerName", string.Empty },
                    { "compilerBackEndVersion", string.Empty },
                    { "compilerFrontEndVersion", string.Empty },
                    { "language", string.Empty },
                    { "dialect", string.Empty },
                    { "moduleName", string.Empty },
                    { "moduleLibrary", string.Empty },
                    { "hash", this.sha256 ?? string.Empty },
                    { "error", errorMessage }
                });
            }
            else
            {
                Console.Write(this.localPath + ",");
                Console.WriteLine($",,,,,,,,,{this.sha256},{errorMessage}");
            }
        }
    }
}
