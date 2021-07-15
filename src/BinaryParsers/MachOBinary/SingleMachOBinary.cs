// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using ELFSharp.MachO;

using Microsoft.CodeAnalysis.BinaryParsers.Dwarf;

namespace Microsoft.CodeAnalysis.BinaryParsers
{
    public class SingleMachOBinary : BinaryBase, IDwarfBinary
    {
        private const string SECTIONNAME_DEBUG_INFO = "__debug_info";
        private const string SECTIONNAME_DEBUG_ABBREV = "__debug_abbrev";
        private const string SECTIONNAME_DEBUG_STR = "__debug_str";
        private const string SECTIONNAME_DEBUG_LINE = "__debug_line";
        private const string SECTIONNAME_DEBUG_FRAME = "__debug_frame";
        private const string SECTIONNAME_EH_FRAME = "__eh_frame";
        private const string SECTIONNAME_TEXT = "__text";
        private const string SECTIONNAME_DATA = "__data";

        public SingleMachOBinary(MachO singleMachO, Uri uri) : base(uri)
        {
            this.MachO = singleMachO;
            CompilationUnits = LoadCompilationUnits();
        }

        public MachO MachO { get; }

        public IEnumerable<Segment> Segments => this.MachO.GetCommandsOfType<Segment>();

        public IEnumerable<SymbolTable> SymbolTables => this.MachO.GetCommandsOfType<SymbolTable>();

        public IEnumerable<IdDylib> IdDylibs => this.MachO.GetCommandsOfType<IdDylib>();

        public IEnumerable<LoadDylib> LoadDylibs => this.MachO.GetCommandsOfType<LoadDylib>();

        public IEnumerable<LoadWeakDylib> LoadWeakDylib => this.MachO.GetCommandsOfType<LoadWeakDylib>();

        public IEnumerable<ReexportDylib> ReexportDylibs => this.MachO.GetCommandsOfType<ReexportDylib>();

        public IEnumerable<EntryPoint> EntryPoint => this.MachO.GetCommandsOfType<EntryPoint>();

        public List<DwarfCompilationUnit> CompilationUnits { get; }

        private List<DwarfLineNumberProgram> lineNumberPrograms;

        public List<DwarfLineNumberProgram> LineNumberPrograms
        {
            get
            {
                if (lineNumberPrograms == null)
                {
                    byte[] debugData = this.LoadSection(SECTIONNAME_DEBUG_LINE);
                    lineNumberPrograms = DwarfSymbolProvider.ParseLineNumberPrograms(debugData, NormalizeAddress);
                }
                return lineNumberPrograms;
            }
        }

        private List<DwarfCommonInformationEntry> commonInformationEntries;

        public List<DwarfCommonInformationEntry> CommonInformationEntries
        {
            get
            {
                if (commonInformationEntries == null)
                {
                    byte[] debugFrame = this.LoadSection(SECTIONNAME_DEBUG_FRAME);
                    byte[] ehFrame = this.LoadSection(SECTIONNAME_EH_FRAME);
                    commonInformationEntries = DwarfSymbolProvider.ParseCommonInformationEntries(debugFrame, ehFrame, new DwarfExceptionHandlingFrameParsingInput(this));
                }
                return commonInformationEntries;
            }
        }

        public List<string> GetSymbolTableFiles()
        {
            return this.LineNumberPrograms.SelectMany(p => p.Files).Select(f => f.Name).ToList();
        }

        #region IDwarfBinary interface

        /// <summary>
        /// The version of Dwarf used.
        /// </summary>
        public int DwarfVersion { get; set; } = -1;

        /// <summary>
        /// Unit type of Dwarf used..
        /// </summary>
        public DwarfUnitType DwarfUnitType { get; set; } = DwarfUnitType.Unknown;

        byte[] IDwarfBinary.DebugData => throw new NotImplementedException();

        byte[] IDwarfBinary.DebugDataDescription => throw new NotImplementedException();

        byte[] IDwarfBinary.DebugDataStrings => throw new NotImplementedException();

        byte[] IDwarfBinary.DebugLine => throw new NotImplementedException();

        byte[] IDwarfBinary.DebugFrame => throw new NotImplementedException();

        byte[] IDwarfBinary.EhFrame => throw new NotImplementedException();

        ulong IDwarfBinary.CodeSegmentOffset => throw new NotImplementedException();

        ulong IDwarfBinary.EhFrameAddress => this.GetSectionAddress(SECTIONNAME_EH_FRAME);

        ulong IDwarfBinary.TextSectionAddress => this.GetSectionAddress(SECTIONNAME_TEXT);

        ulong IDwarfBinary.DataSectionAddress => this.GetSectionAddress(SECTIONNAME_DATA);

        IReadOnlyList<DwarfPublicSymbol> IDwarfBinary.PublicSymbols => throw new NotImplementedException();

        bool IDwarfBinary.Is64bit => this.MachO.Is64;

        private ICompiler[] compilers;

        public ICompiler[] Compilers
        {
            get
            {
                if (compilers == null)
                {
                    compilers = this.GetCompilers();
                }
                return compilers;
            }
        }

        /// <summary>
        /// Gets address offset within module when it is loaded.
        /// </summary>
        /// <param name="address">Virtual address that points where something should be loaded.</param>
        public ulong NormalizeAddress(ulong address)
        {
            ulong codeSegmentOffset = 0;
            Section section = null;
            foreach (Segment seg in this.Segments)
            {
                section = seg.Sections.FirstOrDefault(sec => sec.Address <= address && sec.Address + sec.Size > address);
                if (section != null)
                {
                    codeSegmentOffset = seg.Address - seg.FileOffset;
                    break;
                }
            }

            return section != null ? address - codeSegmentOffset : 0;
        }

        public string GetDwarfCompilerCommand()
        {
            if (CompilationUnits == null || CompilationUnits.Count == 0)
            {
                return string.Empty;
            }
            KeyValuePair<DwarfAttribute, DwarfAttributeValue> producer = CompilationUnits
                .SelectMany(c => c.Symbols)
                .Where(s => s.Tag == DwarfTag.CompileUnit)
                .SelectMany(s => s.Attributes)
                .FirstOrDefault(a => a.Key == DwarfAttribute.Producer);
            return producer.Key == DwarfAttribute.None ? string.Empty : producer.Value.String;
        }

        public DwarfLanguage GetLanguage()
        {
            if (CompilationUnits == null || CompilationUnits.Count == 0)
            {
                return DwarfLanguage.Unknown;
            }
            KeyValuePair<DwarfAttribute, DwarfAttributeValue> language = CompilationUnits
                .SelectMany(c => c.Symbols)
                .Where(s => s.Tag == DwarfTag.CompileUnit)
                .SelectMany(s => s.Attributes)
                .FirstOrDefault(a => a.Key == DwarfAttribute.Language);
            return language.Key == DwarfAttribute.None ? DwarfLanguage.Unknown : ((DwarfLanguage)(language.Value.Constant));
        }

        #endregion IDwarfBinary interface

        private List<DwarfCompilationUnit> LoadCompilationUnits()
        {
            byte[] debugData = this.LoadSection(SECTIONNAME_DEBUG_INFO);
            byte[] debugAbbrev = this.LoadSection(SECTIONNAME_DEBUG_ABBREV);
            byte[] debugStr = this.LoadSection(SECTIONNAME_DEBUG_STR);
            return DwarfSymbolProvider.ParseCompilationUnits(this, debugData, debugAbbrev, debugStr, NormalizeAddress);
        }

        private byte[] LoadSection(string sectionName)
        {
            Section section = this.Segments.SelectMany(seg => seg.Sections.ToList())
                                           .FirstOrDefault(sec => sec.Name == sectionName || sec.Name == sectionName + ".dwo");

            return section != null ? section.GetData() : Array.Empty<byte>();
        }

        private ulong GetSectionAddress(string sectionName)
        {
            foreach (Segment segment in this.Segments)
            {
                Section section = segment.Sections.FirstOrDefault(sec => sec.Name == sectionName);
                if (section != null)
                {
                    ulong CodeSegmentOffset = segment.Address - (ulong)segment.FileOffset;
                    return section.Offset + CodeSegmentOffset;
                }
            }

            return ulong.MaxValue;
        }

        private MachOCompiler[] GetCompilers()
        {
            try
            {
                string compilerString = this.GetDwarfCompilerCommand(); // return empty string if not found
                return new MachOCompiler[] { new MachOCompiler(compilerString) };
            }
            // Catch cases when the compiler string is not formatted the way we expect it to be.
            catch (Exception ex) when (ex is ArgumentException || ex is ArgumentNullException)
            {
                return new MachOCompiler[] { new MachOCompiler(string.Empty) };
            }
        }
    }
}
