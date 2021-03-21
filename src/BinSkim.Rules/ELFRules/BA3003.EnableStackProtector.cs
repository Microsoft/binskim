// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;

using ELFSharp.ELF;
using ELFSharp.ELF.Sections;

using Microsoft.CodeAnalysis.BinaryParsers;
using Microsoft.CodeAnalysis.IL.Sdk;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.IL.Rules
{
    [Export(typeof(Skimmer<BinaryAnalyzerContext>)), Export(typeof(ReportingDescriptor))]
    public class EnableStackProtector : ELFBinarySkimmerBase
    {
        private readonly string[] stack_check_symbols = new string[]{
            "__stack_chk_fail",
            "__stack_chk_fail_local" // Optimization for some architectures, according to compiler comments.
        };

        /// <summary>
        /// BA3003
        /// </summary>
        public override string Id => RuleIds.EnableStackProtector;

        /// <summary>
        /// The stack protector ensures that all functions that use buffers over a certain size will
        /// use a stack cookie(and check it) to prevent stack based buffer overflows, exiting if stack
        /// smashing is detected.Use '--fstack-protector-strong' (all buffers of 4 bytes or more) or
        /// '--fstack-protector-all' (all functions) to enable this.
        /// </summary>
        public override MultiformatMessageString FullDescription => new MultiformatMessageString { Text = RuleResources.BA3003_EnableStackProtector_Description };

        protected override IEnumerable<string> MessageResourceNames => new string[] {
                    nameof(RuleResources.BA3003_Pass),
                    nameof(RuleResources.BA3003_Error),
                    nameof(RuleResources.NotApplicable_InvalidMetadata)
                };

        public override AnalysisApplicability CanAnalyzeElf(ELFBinary target, Sarif.PropertiesDictionary policy, out string reasonForNotAnalyzing)
        {
            IELF elf = target.ELF;

            if (elf.Type == FileType.Core || elf.Type == FileType.None || elf.Type == FileType.Relocatable)
            {
                reasonForNotAnalyzing = MetadataConditions.ElfIsCoreNoneOrObject;
                return AnalysisApplicability.NotApplicableToSpecifiedTarget;
            }

            reasonForNotAnalyzing = null;
            return AnalysisApplicability.ApplicableToSpecifiedTarget;
        }

        public override void Analyze(BinaryAnalyzerContext context)
        {
            IELF elf = context.ELFBinary().ELF;

            var symbolNames =
                new HashSet<string>
                (
                    ELFUtility.GetAllSymbols(elf).Select<ISymbolEntry, string>(sym => sym.Name)
                );

            foreach (string stack_chk in this.stack_check_symbols)
            {
                if (symbolNames.Contains(stack_chk))
                {
                    context.Logger.Log(this,
                        RuleUtilities.BuildResult(ResultKind.Pass, context, null,
                            nameof(RuleResources.BA3003_Pass),
                            context.TargetUri.GetFileName()));
                    return;
                }
            }
            // If we haven't found the stack protector, we assume it wasn't used.
            context.Logger.Log(this,
                RuleUtilities.BuildResult(FailureLevel.Error, context, null,
                    nameof(RuleResources.BA3003_Error),
                    context.TargetUri.GetFileName()));
        }
    }
}
