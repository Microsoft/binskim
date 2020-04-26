﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.BinaryParsers;
using Microsoft.CodeAnalysis.BinaryParsers.PortableExecutable;
using Microsoft.CodeAnalysis.IL.Sdk;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.IL.Rules
{
    // Windows specific binary and program database-reading skimmers.
    public abstract class WindowsBinaryAndPdbSkimmerBase : WindowsBinarySkimmerBase
    {
        public sealed override void Analyze(BinaryAnalyzerContext context)
        {
            // Uses PDB Parsing.
            BinaryParsers.PlatformSpecificHelpers.ThrowIfNotOnWindows();
            PEBinary target = context.PEBinary();

            string pdbLoadTrace = target.PdbLoadTrace;
            if (!string.IsNullOrEmpty(pdbLoadTrace))
            {
                LogPdbLoadTrace(
                    context,
                    pdbLoadSucceeded: target.Pdb != null,
                    pdbLoadTrace);
            }

            if (target.Pdb == null)
            {
                LogExceptionLoadingPdb(context, target.PdbParseException);
                return;
            }

            this.AnalyzePortableExecutableAndPdb(context);
        }

        public sealed override AnalysisApplicability CanAnalyze(BinaryAnalyzerContext context, out string reasonForNotAnalyzing)
        {
            AnalysisApplicability result = base.CanAnalyze(context, out reasonForNotAnalyzing);
            if (result != AnalysisApplicability.ApplicableToSpecifiedTarget) { return result; }

            PE portableExecutable = context.PEBinary().PE;
            result = AnalysisApplicability.NotApplicableToSpecifiedTarget;

            reasonForNotAnalyzing = MetadataConditions.ImageIsWixBinary;
            if (portableExecutable.IsWixBinary) { return result; }

            reasonForNotAnalyzing = MetadataConditions.ImageIsILLibraryAssembly;
            if (portableExecutable.IsILLibrary) { return result; }

            reasonForNotAnalyzing = MetadataConditions.ImageIsDotNetCoreBootstrapExe;
            if (portableExecutable.IsDotNetCoreBootstrapExe) { return result; }

            reasonForNotAnalyzing = null;
            return AnalysisApplicability.ApplicableToSpecifiedTarget;
        }

        public abstract void AnalyzePortableExecutableAndPdb(BinaryAnalyzerContext context);

        public static void LogPdbLoadTrace(
            IAnalysisContext context,
            bool pdbLoadSucceeded,
            string pdbLoadTrace)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            string formatString = pdbLoadSucceeded
                // The PDB for '{0}' was found and loaded. Probing details:{1}
                ? RuleResources.PdbLoadSucceeded
                // Could not locate the PDB for '{0'}. Probing details:{1}
                : RuleResources.PdbLoadFailed;

            context.Logger.LogConfigurationNotification(
                Errors.CreateNotification(
                    context.TargetUri,
                    "TRC001.PdbLoad",
                    context.Rule.Id,
                    FailureLevel.Warning,
                    exception: null,
                    persistExceptionStack: false,
                    formatString,
                    context.TargetUri.GetFileName(),
                    pdbLoadTrace));
        }

        public static void LogExceptionLoadingPdb(IAnalysisContext context, Exception exception)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // '{0}' was not evaluated for check '{1}' because its PDB could not be loaded.
            context.Logger.LogConfigurationNotification(
                Errors.CreateNotification(
                    context.TargetUri,
                    "ERR997.ExceptionLoadingPdb",
                    context.Rule.Id,
                    FailureLevel.Error,
                    exception,
                    persistExceptionStack: false,
                    RuleResources.ERR997_ExceptionLoadingPdb,
                    context.TargetUri.GetFileName(),
                    context.Rule.Name));

            context.RuntimeErrors |= RuntimeConditions.ExceptionLoadingPdb;
        }
    }
}
