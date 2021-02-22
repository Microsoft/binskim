﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Globalization;

using Microsoft.CodeAnalysis.BinaryParsers;
using Microsoft.CodeAnalysis.BinaryParsers.PortableExecutable;
using Microsoft.CodeAnalysis.BinaryParsers.ProgramDatabase;
using Microsoft.CodeAnalysis.IL.Sdk;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.IL.Rules
{
    [Export(typeof(Skimmer<BinaryAnalyzerContext>)), Export(typeof(ReportingDescriptor)), Export(typeof(IOptionsProvider))]
    public class BuildWithSecureTools : WindowsBinaryAndPdbSkimmerBase, IOptionsProvider
    {
        /// <summary>
        /// BA2006
        /// </summary>
        public override string Id => RuleIds.BuildWithSecureTools;

        /// <summary>
        /// Application code should be compiled with the most up-to-date tool sets
        /// possible to take advantage of the most current compile-time security
        /// features. Among other things, these features provide address space
        /// layout randomization, help prevent arbitrary code execution and enable
        /// code generation that can help prevent speculative execution side-channel
        /// attacks.
        /// </summary>
        public override MultiformatMessageString FullDescription => new MultiformatMessageString { Text = RuleResources.BA2006_BuildWithSecureTools_Description };

        protected override IEnumerable<string> MessageResourceNames => new string[] {
                    nameof(RuleResources.BA2006_Error),
                    nameof(RuleResources.BA2006_Error_BadModule),
                    nameof(RuleResources.BA2006_Pass),
                    nameof(RuleResources.NotApplicable_InvalidMetadata)};

        public IEnumerable<IOption> GetOptions()
        {
            return new List<IOption>
            {
                AllowedLibraries,
                MinimumToolVersions,
                AdvancedMitigationsEnforced
            }.ToImmutableArray();
        }

        private const string AnalyzerName = RuleIds.BuildWithSecureTools + "." + nameof(BuildWithSecureTools);

        private const string MIN_XBOX_COMPILER_VER = "MinimumXboxCompilerVersion";

        public static PerLanguageOption<StringToVersionMap> MinimumToolVersions { get; } =
            new PerLanguageOption<StringToVersionMap>(
                AnalyzerName, nameof(MinimumToolVersions), defaultValue: () => BuildMinimumToolVersionsMap());

        public static PerLanguageOption<StringToVersionMap> AllowedLibraries { get; } =
            new PerLanguageOption<StringToVersionMap>(
                AnalyzerName, nameof(AllowedLibraries), defaultValue: () => BuildAllowedLibraries());

        public static PerLanguageOption<AdvancedMitigations> AdvancedMitigationsEnforced { get; } =
            new PerLanguageOption<AdvancedMitigations>(
                AnalyzerName, nameof(AdvancedMitigationsEnforced), defaultValue: () => AdvancedMitigations.None);

        public override AnalysisApplicability CanAnalyzePE(PEBinary target, PropertiesDictionary policy, out string reasonForNotAnalyzing)
        {
            PE portableExecutable = target.PE;
            AnalysisApplicability result = AnalysisApplicability.NotApplicableToSpecifiedTarget;

            reasonForNotAnalyzing = MetadataConditions.ImageIsILOnlyAssembly;
            if (portableExecutable.IsILOnly) { return result; }

            reasonForNotAnalyzing = MetadataConditions.ImageIsResourceOnlyBinary;
            if (portableExecutable.IsResourceOnly) { return result; }

            reasonForNotAnalyzing = null;
            return AnalysisApplicability.ApplicableToSpecifiedTarget;
        }

        public override void AnalyzePortableExecutableAndPdb(BinaryAnalyzerContext context)
        {
            PEBinary target = context.PEBinary();
            Pdb pdb = target.Pdb;

            /*
            if (target.PE.IsManaged && !target.PE.IsMixedMode)
            {
                AnalyzeManagedPE(context);
            }
            */

            Version minCompilerVersion = new Version(int.MaxValue, int.MaxValue);

            var badModules = new List<ObjectModuleDetails>();
            StringToVersionMap allowedLibraries = context.Policy.GetProperty(AllowedLibraries);

            var languageToBadModules = new Dictionary<Language, List<ObjectModuleDetails>>();

            foreach (DisposableEnumerableView<Symbol> omView in pdb.CreateObjectModuleIterator())
            {
                Symbol om = omView.Value;
                ObjectModuleDetails omDetails = om.GetObjectModuleDetails();

                switch (omDetails.Language)
                {
                    case Language.LINK:
                    {
                        continue;
                    }

                    case Language.C:
                    case Language.Cxx:
                    {
                        minCompilerVersion = (target.PE.IsXBox)
                            ? context.Policy.GetProperty(MinimumToolVersions)[MIN_XBOX_COMPILER_VER]
                            : context.Policy.GetProperty(MinimumToolVersions)[nameof(Language.C)];
                        break;
                    }

                    case Language.MASM:
                    {
                        minCompilerVersion =
                            context.Policy.GetProperty(MinimumToolVersions)[nameof(Language.MASM)];
                        break;
                    }

                    case Language.CVTRES:
                    {
                        minCompilerVersion =
                            context.Policy.GetProperty(MinimumToolVersions)[nameof(Language.CVTRES)];
                        break;
                    }

                    case Language.CSharp:
                    {
                        minCompilerVersion =
                            context.Policy.GetProperty(MinimumToolVersions)[nameof(Language.CSharp)];
                        break;
                    }

                    case Language.Unknown:
                    {
                        minCompilerVersion =
                            context.Policy.GetProperty(MinimumToolVersions)[nameof(Language.Unknown)];
                        break;
                    }

                    default:
                    {
                        continue;
                    }
                }


                // See if the item is in our skip list
                if (!string.IsNullOrEmpty(om.Lib))
                {
                    string libFileName = string.Concat(System.IO.Path.GetFileName(om.Lib), ",", omDetails.Language.ToString()).ToLowerInvariant();

                    if (allowedLibraries.TryGetValue(libFileName, out Version minAllowedVersion) &&
                        omDetails.CompilerBackEndVersion >= minAllowedVersion)
                    {
                        continue;
                    }
                }

                Version actualVersion;
                Version minimumVersion = minCompilerVersion;
                Language omLanguage = omDetails.Language;
                switch (omLanguage)
                {
                    case Language.C:
                    case Language.Cxx:
                    {
                        actualVersion = Minimum(omDetails.CompilerBackEndVersion, omDetails.CompilerFrontEndVersion);
                        break;
                    }

                    case Language.LINK:
                    case Language.MASM:
                    case Language.CVTRES:
                    {
                        actualVersion = omDetails.CompilerBackEndVersion;
                        break;
                    }

                    default:
                        continue;
                }

                bool foundIssue = actualVersion < minimumVersion;

                AdvancedMitigations advancedMitigations = context.Policy.GetProperty(AdvancedMitigationsEnforced);
                if (!foundIssue &&
                    (advancedMitigations & AdvancedMitigations.Spectre) == AdvancedMitigations.Spectre)
                {
                    var machineType = (ExtendedMachine)target.PE.Machine;

                    // Current toolchain is within the version range to validate.
                    // Now we'll retrieve relevant compiler mitigation details to
                    // ensure this object module's build and revision meet
                    // expectations.
                    CompilerMitigations newMitigationData =
                        EnableSpectreMitigations.GetAvailableMitigations(context, machineType, actualVersion);

                    // Current compiler version does not support Spectre mitigations.
                    foundIssue = !newMitigationData.HasFlag(CompilerMitigations.D2GuardSpecLoadAvailable)
                                 && !newMitigationData.HasFlag(CompilerMitigations.QSpectreAvailable);

                    if (foundIssue)
                    {
                        // Get the closest compiler version that has mitigations--i.e. if the user is using a 19.0 (VS2015) compiler, we should be recommending an upgrade to the 
                        // 19.0 version that has the mitigations, not an upgrade to a 19.10+ (VS2017) compiler.
                        // Limitation--if there are multiple 'upgrade to' versions to recommend, this just going to give users the last one we see in the error.
                        minCompilerVersion = EnableSpectreMitigations.GetClosestCompilerVersionWithSpectreMitigations(context, machineType, actualVersion);

                        // Indicates Spectre mitigations are not supported on this platform.  We won't flag this case.
                        if (minCompilerVersion == null)
                        {
                            foundIssue = false;
                        }
                    }
                }

                if (foundIssue)
                {
                    badModules.Add(omDetails);
                }
            }

            if (badModules.Count != 0)
            {
                string badModulesText = badModules.CreateOutputCoalescedByCompiler();
                string minimumRequiredCompilers = BuildMinimumCompilersList(context, languageToBadModules);

                // '{0}' was compiled with one or more modules which were not built using
                // minimum required tool versions ({1}). More recent toolchains
                // contain mitigations that make it more difficult for an attacker to exploit
                // vulnerabilities in programs they produce. To resolve this issue, compile
                // and /or link your binary with more recent tools. If you are servicing a
                // product where the tool chain cannot be modified (e.g. producing a hotfix
                // for an already shipped version) ignore this warning. Modules built outside
                // of policy: {2}
                context.Logger.Log(this,
                    RuleUtilities.BuildResult(FailureLevel.Error, context, null,
                    nameof(RuleResources.BA2006_Error),
                        context.TargetUri.GetFileName(),
                        minimumRequiredCompilers,
                        badModulesText));
                return;
            }

            // All linked modules of '{0}' generated by the Microsoft front-end
            // satisfy configured policy (compiler minimum version {1}).
            context.Logger.Log(this,
                    RuleUtilities.BuildResult(ResultKind.Pass, context, null,
                    nameof(RuleResources.BA2006_Pass),
                        context.TargetUri.GetFileName(),
                        minCompilerVersion.ToString()));
        }

        private string BuildMinimumCompilersList(BinaryAnalyzerContext context, Dictionary<Language, List<ObjectModuleDetails>> languageToBadModules)
        {
            var languages = new List<string>();

            foreach(Language language in languageToBadModules.Keys)
            {
                Version version = context.Policy.GetProperty(MinimumToolVersions)[language.ToString()];
                languages.Add($"{language} ({version})");
            }
            return string.Join(", ", languages);
        }

        private string BuildBadModulesList(Dictionary<Language, List<ObjectModuleDetails>> languageToBadModules)
        {
            var coalescedModules = new List<string>();

            foreach (Language language in languageToBadModules.Keys)
            {
                string modulesText = languageToBadModules[language].CreateOutputCoalescedByCompiler();
                coalescedModules.Add(modulesText);
            }
            return string.Join(string.Empty, coalescedModules);
        }
    

    private void AnalyzeManagedPE(BinaryAnalyzerContext context)
        {
            Version minCscVersion =
                context.Policy.GetProperty(MinimumToolVersions)[nameof(Language.CSharp)];

            PE pe = context.PEBinary().PE;

            if (pe.LinkerVersion < minCscVersion)
            {
                // '{0}' is a managed assembly that was compiled with an outdated toolchain
                // ({1}) that does not support security features (such as SHA256 PDB
                // checksums and reproducible builds) that must be enabled by policy. To
                // resolve this issue, compile with more recent tools ({2} or later).
                context.Logger.Log(this,
                    RuleUtilities.BuildResult(FailureLevel.Error, context, null,
                    nameof(RuleResources.BA2006_Error_OutdatedCsc),
                        context.TargetUri.GetFileName(),
                        pe.LinkerVersion.ToString(),
                        minCscVersion.ToString()));

                return;
            }

            // '{0}' is a managed assembly that was compiled with toolchain ({1}) that supports all security features that must be enabled by policy.
            context.Logger.Log(this,
                RuleUtilities.BuildResult(ResultKind.Pass, context, null,
                nameof(RuleResources.BA2006_Pass_Csc),
                    context.TargetUri.GetFileName(),
                    pe.LinkerVersion.ToString()));
        }

        public static Version Minimum(Version lhs, Version rhs)
        {
            return (lhs < rhs) ? lhs : rhs;
        }

        private static StringToVersionMap BuildMinimumToolVersionsMap()
        {
            var result = new StringToVersionMap
            {
                [nameof(Language.C)] = new Version(17, 0, 65501, 17013),
                [nameof(Language.Cxx)] = new Version(17, 0, 65501, 17013),
                [nameof(Language.MASM)] = new Version(14, 15, 26706, 0),
                [nameof(Language.LINK)] = new Version(17, 0, 65501, 17013),
                [nameof(Language.CSharp)] = new Version(19, 0, 0, 0),
                [nameof(Language.CVTRES)] = new Version(14, 14, 26430, 0),
                [nameof(Language.Unknown)] = new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue),
                [MIN_XBOX_COMPILER_VER] = new Version(16, 0, 11886, 0)
            };

            foreach (string name in Enum.GetNames(typeof(Language)))
            {
                if (!result.ContainsKey(name))
                {
                    // If we don't have entry for a language, fire on everything.
                    result[name] = new Version(int.MaxValue, int.MaxValue);
                }
            }

            return result;
        }

        private static StringToVersionMap BuildAllowedLibraries()
        {
            var result = new StringToVersionMap();

            // Example entries
            result["libeay32.lib,unknown"] = new Version("0.0.0.0"); 
            // result["cplusplusExample.lib,cxx"] = new Version("1.0.0.0")
            // result["masmExample.lib,masm"] = new Version("1.0.0.0")

            return result;
        }
    }
}

namespace Microsoft.CodeAnalysis
{
    [Flags]
    public enum AdvancedMitigations
    {
        None = 0x0,
        Spectre = 0x1
    }
}
