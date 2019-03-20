﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.CodeAnalysis.IL.Rules
{
    internal class TestMessageLogger : IAnalysisLogger
    {
        public TestMessageLogger()
        {
            FailTargets = new HashSet<string>();
            PassTargets = new HashSet<string>();
            NotApplicableTargets = new HashSet<string>();
            ConfigurationErrorTargets = new HashSet<string>();
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
            RuntimeErrors = runtimeConditions;
        }

        public void AnalyzingTarget(IAnalysisContext context)
        {
        }

        public void LogMessage(bool verbose, string message)
        {
        }

        public void Log(ReportingDescriptor rule, Result result)
        {
            NoteTestResult(result.Kind, result.Locations.First().PhysicalLocation.ArtifactLocation.Uri.LocalPath);
            NoteTestResult(result.Level, result.Locations.First().PhysicalLocation.ArtifactLocation.Uri.LocalPath);
        }

        public void NoteTestResult(ResultKind messageKind, string targetPath)
        {
            switch (messageKind)
            {
                case ResultKind.Pass:
                {
                    PassTargets.Add(targetPath);
                    break;
                }

                case ResultKind.NotApplicable:
                {
                    NotApplicableTargets.Add(targetPath);
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
                {
                    FailTargets.Add(targetPath);
                    break;
                }

                case FailureLevel.Warning:
                {
                    FailTargets.Add(targetPath);
                    break;
                }

                case FailureLevel.Note:
                {
                    throw new NotImplementedException();
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
            ConfigurationErrorTargets.Add(notification.PhysicalLocation.ArtifactLocation.Uri.LocalPath);
        }
    }
}