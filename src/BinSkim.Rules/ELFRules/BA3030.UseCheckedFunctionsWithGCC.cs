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
    public class UseCheckedFunctionsWithGcc : ELFBinarySkimmerBase
    {
        // This list comes from listing all of the functions available in glibc (using readelf), 
        // then filtering to ones with a checked variant (_*_chk).
        private static readonly string[] fortifiableFunctionNames = new string[]{
            "asprintf",
            "confstr",
            "dprintf",
            "fdelt",
            "fgets",
            "fgetws",
            "fprintf",
            "fread",
            "fwprintf",
            "getcwd",
            "getdomainname",
            "getgroups",
            "gethostname",
            "gets",
            "getwd",
            "longjmp",
            "mbsnrtowcs",
            "mbsrtowcs",
            "mbstowcs",
            "memcpy",
            "memmove",
            "mempcpy",
            "memset",
            "poll",
            "ppoll",
            "pread",
            "printf",
            "read",
            "readlink",
            "readlinkat",
            "realpath",
            "recv",
            "recvfrom",
            "snprintf",
            "sprintf",
            "stack",
            "stpcpy",
            "stpncpy",
            "strcat",
            "strcpy",
            "strncat",
            "strncpy",
            "swprintf",
            "syslog",
            "vasprintf",
            "vdprintf",
            "vfprintf",
            "vfwprintf",
            "vprintf",
            "vsnprintf",
            "vsprintf",
            "vswprintf",
            "vsyslog",
            "vwprintf",
            "wcpcpy",
            "wcpncpy",
            "wcrtomb",
            "wcscat",
            "wcscpy",
            "wcsncat",
            "wcsncpy",
            "wcsnrtombs",
            "wcsrtombs",
            "wcstombs",
            "wctomb",
            "wmemcpy",
            "wmemmove",
            "wmempcpy",
            "wmemset",
            "wprintf"
        };

        private static readonly HashSet<string> unfortifiedFunctions =
            new HashSet<string>(fortifiableFunctionNames);

        private static readonly HashSet<string> fortifiedFunctions =
            new HashSet<string>(fortifiableFunctionNames.Select(f => "__" + f + "_chk"));

        /// <summary>
        /// BA3030
        /// </summary>
        public override string Id => RuleIds.UseCheckedFunctionsWithGcc;

        /// <summary>
        /// The stack protector ensures that all functions that use buffers over a certain size will
        /// use a stack cookie(and check it) to prevent stack based buffer overflows, exiting if stack
        /// smashing is detected.Use '--fstack-protector-strong' (all buffers of 4 bytes or more) or
        /// '--fstack-protector-all' (all functions) to enable this.
        /// </summary>
        public override MultiformatMessageString FullDescription => new MultiformatMessageString { Text = RuleResources.BA3030_UseCheckedFunctionsWithGcc_Description };

        protected override IEnumerable<string> MessageResourceNames => new string[] {
                    nameof(RuleResources.BA3030_Pass_AllFunctionsChecked),
                    nameof(RuleResources.BA3030_Pass_SomeFunctionsChecked),
                    nameof(RuleResources.BA3030_Pass_NoCheckableFunctions),
                    nameof(RuleResources.BA3030_Error),
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

            // We check for "any usage of non-gcc" as a default/standard compilation with clang leads to [GCC, Clang]
            // either because it links with a gcc-compiled object (cstdlib) or the linker also reading as GCC.
            // This has a potential for a False Negative if teams are using GCC and other tools.
            if (target.Compilers.Any(c => c.Compiler != ELFCompilerType.GCC))
            {
                reasonForNotAnalyzing = MetadataConditions.ElfNotBuiltWithGcc;
                return AnalysisApplicability.NotApplicableToSpecifiedTarget;
            }

            reasonForNotAnalyzing = null;
            return AnalysisApplicability.ApplicableToSpecifiedTarget;
        }

        /// <summary>
        /// Checks if Fortified functions are used--the -DFORTIFY_SOURCE=2 flag enables these when -O2 is enabled.
        ///
        /// Check implementation:
        /// -Get all function symbols in the ELF binary
        /// -Check for any fortified functions--if we find any, we used the option.
        /// -Check for any unfortified functions.  If we only find unfortified functions, one of two things is true:
        ///     1) Fortify Source wasn't used; or
        ///     2) Fortify Source was used, but gcc/clang was unable to statically find anything that needed to be fortified.
        ///     We report on both cases.
        /// -If no fortifiable functions were used at all, the rule doesn't apply.
        /// </summary>
        public override void Analyze(BinaryAnalyzerContext context)
        {
            IELF elf = context.ELFBinary().ELF;

            IEnumerable<ISymbolEntry> symbols =
                ELFUtility.GetAllSymbols(elf).Where(sym => sym.Type == SymbolType.Function || sym.Type == SymbolType.Object);

            var protectedFunctions = new List<ISymbolEntry>();
            var unprotectedFunctions = new List<ISymbolEntry>();
            foreach (ISymbolEntry e in symbols)
            {
                if (unfortifiedFunctions.Contains(e.Name))
                {
                    unprotectedFunctions.Add(e);
                }
                else if (fortifiedFunctions.Contains(e.Name))
                {
                    protectedFunctions.Add(e);
                }
            }

            if (protectedFunctions.Any())
            {
                if (unprotectedFunctions.Any())
                {
                    context.Logger.Log(this,
                       RuleUtilities.BuildResult(ResultKind.Pass, context, null,
                           nameof(RuleResources.BA3030_Pass_SomeFunctionsChecked),
                           context.TargetUri.GetFileName()));
                }
                else
                {
                    context.Logger.Log(this,
                       RuleUtilities.BuildResult(ResultKind.Pass, context, null,
                           nameof(RuleResources.BA3030_Pass_AllFunctionsChecked),
                           context.TargetUri.GetFileName()));
                }
            }
            else if (unprotectedFunctions.Any())
            {
                context.Logger.Log(this,
                       RuleUtilities.BuildResult(FailureLevel.Error, context, null,
                           nameof(RuleResources.BA3030_Error),
                           context.TargetUri.GetFileName()));
            }
            else
            {
                context.Logger.Log(this,
                       RuleUtilities.BuildResult(ResultKind.Pass, context, null,
                           nameof(RuleResources.BA3030_Pass_NoCheckableFunctions),
                           context.TargetUri.GetFileName()));
            }
        }
    }
}
