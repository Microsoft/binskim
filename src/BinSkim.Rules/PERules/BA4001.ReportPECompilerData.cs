// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;

using Microsoft.CodeAnalysis.BinaryParsers;
using Microsoft.CodeAnalysis.BinaryParsers.ProgramDatabase;
using Microsoft.CodeAnalysis.IL.Sdk;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.IL.Rules
{
    [Export(typeof(Skimmer<BinaryAnalyzerContext>)), Export(typeof(ReportingDescriptor))]
    public class ReportPECompilerData : WindowsBinaryAndPdbSkimmerBase
    {
        /// <summary>
        /// BA4001
        /// </summary>
        public override string Id => RuleIds.ReportPECompilerData;

        public override bool LogPdbLoadException => false;

        /// <summary>
        /// This rule emits CSV data to the console for every compiler/language/version
        /// combination that's observed in any PDB-linked compiland.
        /// </summary>
        public override MultiformatMessageString FullDescription => new MultiformatMessageString { Text = RuleResources.BA4001_ReportPECompilerData_Description };

        public override bool EnabledByDefault => false;

        protected override IEnumerable<string> MessageResourceNames => Array.Empty<string>();

        private bool PrintHeader = true;

        public override AnalysisApplicability CanAnalyzePE(PEBinary target, PropertiesDictionary policy, out string reasonForNotAnalyzing)
        {
            reasonForNotAnalyzing = null;
            return AnalysisApplicability.ApplicableToSpecifiedTarget;
        }

        public override void AnalyzePortableExecutableAndPdb(BinaryAnalyzerContext context)
        {
            PEBinary target = context.PEBinary();
            Pdb pdb = target.Pdb;

            if (PrintHeader)
            {
                context.CompilerDataLogger.PrintHeader();
                PrintHeader = false;
            }

            if (pdb == null)
            {
                string errorMessage = target.PdbParseException.Message;
                context.CompilerDataLogger.WriteException(errorMessage);
                return;
            }

            var records = new Dictionary<string, ObjectModuleDetails>();

            if (target.PE.IsManaged)
            {
                string record = $".NET Compiler,{target.PE.LinkerVersion},{target.PE.LinkerVersion},{Language.MSIL}";

                if (!records.TryGetValue(record, out ObjectModuleDetails value))
                {
                    records[record] = null;
                }
            }
            else
            {
                foreach (DisposableEnumerableView<Symbol> omView in pdb.CreateObjectModuleIterator())
                {
                    Symbol om = omView.Value;
                    ObjectModuleDetails omDetails = om.GetObjectModuleDetails();

                    string record =
                        omDetails.CompilerName?.Replace(",", "_").Trim() + "," +
                        omDetails.CompilerBackEndVersion + "," +
                        omDetails.CompilerFrontEndVersion + "," +
                        omDetails.Language;

                    if (!records.ContainsKey(record))
                    {
                        records[record] = omDetails;
                    }
                }
            }

            foreach (KeyValuePair<string, ObjectModuleDetails> kv in records)
            {
                context.CompilerDataLogger.Write(kv.Key, kv.Value);
            }
        }
    }
}
