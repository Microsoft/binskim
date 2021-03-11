// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.CodeAnalysis.IL.Rules
{
    public class TestMessageLogger : IAnalysisLogger
    {
        public TestMessageLogger()
        {
            this.FailTargets = new HashSet<string>();
            this.PassTargets = new HashSet<string>();
            this.NotApplicableTargets = new HashSet<string>();
            this.ConfigurationErrorTargets = new HashSet<string>();
        }

        public RuntimeConditions RuntimeErrors { get; set; }

        public HashSet<string> PassTargets { get; set; }

        public HashSet<string> FailTargets { get; set; }

        public HashSet<string> ConfigurationErrorTargets { get; set; }

        public HashSet<string> NotApplicableTargets { get; set; }

        public void AnalysisStarted()
        {
        }

        public void AnalysisStopped(RuntimeConditions runtimeConditions)
        {
            this.RuntimeErrors = runtimeConditions;
        }

        public void AnalyzingTarget(IAnalysisContext context)
        {
        }

        public void LogMessage(bool verbose, string message)
        {
        }

        public void Log(ReportingDescriptor rule, Result result)
        {
            this.NoteTestResult(result.Kind, result.Locations.First().PhysicalLocation.ArtifactLocation.Uri.LocalPath);
            this.NoteTestResult(result.Level, result.Locations.First().PhysicalLocation.ArtifactLocation.Uri.LocalPath);
        }

        public void NoteTestResult(ResultKind messageKind, string targetPath)
        {
            switch (messageKind)
            {
                case ResultKind.Pass:
                {
                    this.PassTargets.Add(targetPath);
                    break;
                }

                case ResultKind.NotApplicable:
                {
                    this.NotApplicableTargets.Add(targetPath);
                    break;
                }

                default:
                {
                    break;
                }
            }
        }

        public void NoteTestResult(FailureLevel messageKind, string targetPath)
        {
            switch (messageKind)
            {
                case FailureLevel.Error:
                case FailureLevel.Warning:
                case FailureLevel.Note:
                {
                    this.FailTargets.Add(targetPath);
                    break;
                }

                default:
                {
                    break;
                }
            }
        }

        public void LogToolNotification(Sarif.Notification notification)
        {
            throw new NotImplementedException();
        }

        public void LogConfigurationNotification(Sarif.Notification notification)
        {
            this.ConfigurationErrorTargets.Add(notification.Locations[0].PhysicalLocation.ArtifactLocation.Uri.LocalPath);
        }
    }
}
