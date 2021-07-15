// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;

using Microsoft.CodeAnalysis.BinaryParsers;
using Microsoft.CodeAnalysis.BinaryParsers.Dwarf;
using Microsoft.CodeAnalysis.IL.Sdk;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.IL.Rules
{
    [Export(typeof(Skimmer<BinaryAnalyzerContext>)), Export(typeof(ReportingDescriptor))]
    public class ReportDwarfCompilerData : DwarfSkimmerBase
    {
        /// <summary>
        /// BA4002
        /// </summary>
        public override string Id => RuleIds.ReportELFCompilerData;

        /// <summary>
        /// This rule emits CSV data to the console
        /// for every compiler/language/version combination that's observed.
        /// </summary>
        public override MultiformatMessageString FullDescription => new MultiformatMessageString { Text = RuleResources.BA4002_ReportELFCompilerData_Description };

        public override bool EnabledByDefault => false;

        protected override IEnumerable<string> MessageResourceNames => Array.Empty<string>();

        private bool PrintHeader = true;

        public override AnalysisApplicability CanAnalyzeDwarf(IDwarfBinary target, Sarif.PropertiesDictionary policy, out string reasonForNotAnalyzing)
        {
            reasonForNotAnalyzing = null;
            return AnalysisApplicability.ApplicableToSpecifiedTarget;
        }

        public override void Analyze(BinaryAnalyzerContext context)
        {
            IDwarfBinary binary = context.DwarfBinary();
            List<string> symbolTableFiles;
            if (binary is ElfBinary elf)
            {
                symbolTableFiles = elf.GetSymbolTableFiles().Select(entry => entry.Name).ToList();
                this.PrintCompilerData(context, binary.GetLanguage().ToString(), binary.Compilers, symbolTableFiles);
            }

            if (binary is MachOBinary machO)
            {
                machO.MachOs.ToList().ForEach
                (
                    machO => this.PrintCompilerData(
                                        context,
                                        machO.GetLanguage().ToString(),
                                        machO.Compilers,
                                        machO.GetSymbolTableFiles())
                );
            }
        }

        private void PrintCompilerData(BinaryAnalyzerContext context, string language, ICompiler[] compilers, List<string> files)
        {
            if (this.PrintHeader)
            {
                Console.WriteLine("Target,Compiler Name,Compiler BackEnd Version,Compiler FrontEnd Version,Language,Module Name,Module Library,Hash,Error");
                this.PrintHeader = false;
            }

            var processedRecords = new HashSet<string>();

            foreach (ICompiler compiler in compilers)
            {
                if (compiler.Compiler == ElfCompilerType.Unknown)
                {
                    continue;
                }

                foreach (string file in files)
                {
                    string currentRecord = compiler.Compiler + "," + compiler.Version + "," + language;

                    if (processedRecords.Contains(currentRecord))
                    {
                        continue;
                    }

                    processedRecords.Add(currentRecord);

                    context.CompilerDataLogger.Write(compiler.Compiler.ToString(), compiler.Version.ToString(), language, file);
                }
            }
        }
    }
}
