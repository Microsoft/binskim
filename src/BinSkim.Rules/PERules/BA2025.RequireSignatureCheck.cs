// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Reflection.PortableExecutable;

using Microsoft.CodeAnalysis.BinaryParsers;
using Microsoft.CodeAnalysis.IL.Sdk;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.IL.Rules
{
    [Export(typeof(Skimmer<BinaryAnalyzerContext>)), Export(typeof(ReportingDescriptor))]
    public class RequireSignatureCheck : PEBinarySkimmerBase
    {
        /// <summary>
        /// https://docs.microsoft.com/windows/win32/debug/pe-format#dll-characteristics
        /// </summary>
        private const int ImageDllCharacteristicsForceIntegrity = 0x80;

        /// <summary>
        /// BA2025
        /// </summary>
        public override string Id => RuleIds.RequireSignatureCheck;

        /// <summary>
        /// Forced Integrity checking is a policy that ensures a binary that is being loaded is signed prior
        /// to loading. The IMAGE_DLLCHARACTERISTICS_FORCE_INTEGRITY flag is set in the PE header at link time
        /// by using the /INTEGRITYCHECK linker flag to indicate that the binary being loaded must be signed.
        /// This flag causes the Windows memory manager to enforce a signature check at load time on the binary
        /// file. Applications check for presence of the flag when the binary is loaded in order to ensure that
        /// the identity of publisher is known. The Forced Integrity policy is enforced on Windows Vista,
        /// Windows Server 2008 and later releases when the IMAGE_DLLCHARACTERISTICS_FORCE_INTEGRITY flag is set.
        /// The Forced Integrity policy is enforced on both 32 bit and 64 bit systems and in both user and
        /// kernel mode. However, Force Integrity is not enforced for boot start drivers. The following Windows
        /// features enforce integrity checking on third-party binaries: Windows Security Center, Appinit.dll
        /// extensions, Object Manager filter registration, extended process filter registration APIs, and for
        /// Windows 7, Windows Biometric Framework, LSA extensions, and NTOS kernel extensions. There is a
        /// development cost associated with signing binaries with the IMAGE_DLLCHARACTERISTICS_FORCE_INTEGRITY
        /// flag set, so do not set the IMAGE_DLLCHARACTERISTICS_FORCE_INTEGRITY flag unless your component
        /// requires it. See https://social.technet.microsoft.com/wiki/contents/articles/255.forced-integrity-signing-of-portable-executable-pe-files.aspx
        /// for more information.
        /// </summary>
        public override MultiformatMessageString FullDescription => new MultiformatMessageString { Text = RuleResources.BA2025_RequireSignatureCheck_Description };

        protected override IEnumerable<string> MessageResourceNames => new string[] {
            nameof(RuleResources.BA2025_Warning),
            nameof(RuleResources.BA2025_Pass),
            nameof(RuleResources.NotApplicable_InvalidMetadata)
        };

        public override void Analyze(BinaryAnalyzerContext context)
        {
            PEBinary target = context.PEBinary();
            PEHeader peHeader = target.PE.PEHeaders.PEHeader;

            DllCharacteristics dllCharacterstics = peHeader.DllCharacteristics;
            if (!dllCharacterstics.HasFlag((DllCharacteristics)ImageDllCharacteristicsForceIntegrity))
            {
                // '{0}' is a kernel mode binary that was not compiled with /INTEGRITYCHECK, requiring the
                // Windows memory manager to check for a digital signature when loading the image.
                context.Logger.Log(this,
                    RuleUtilities.BuildResult(FailureLevel.Note, context, null,
                        nameof(RuleResources.BA2025_Warning),
                        context.TargetUri.GetFileName()));
                return;
            }

            // '{0}' is a kernel mode binary that was compiled with /INTEGRITYCHECK, requiring the Windows
            // memory manager to check for a digital signature when loading the image.
            context.Logger.Log(this,
                RuleUtilities.BuildResult(ResultKind.Pass, context, null,
                    nameof(RuleResources.BA2025_Pass),
                        context.TargetUri.GetFileName()));
        }

        public override AnalysisApplicability CanAnalyzePE(PEBinary target, PropertiesDictionary policy, out string reasonForNotAnalyzing)
        {
            if (!target.PE.IsKernelMode)
            {
                reasonForNotAnalyzing = MetadataConditions.ImageIsNotKernelModeBinary;
                return AnalysisApplicability.NotApplicableToSpecifiedTarget;
            }

            reasonForNotAnalyzing = null;
            return AnalysisApplicability.ApplicableToSpecifiedTarget;
        }
    }
}
