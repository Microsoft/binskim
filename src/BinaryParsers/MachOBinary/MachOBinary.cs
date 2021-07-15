// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ELFSharp.MachO;

using Microsoft.CodeAnalysis.BinaryParsers.Dwarf;
using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.BinaryParsers
{
    public class MachOBinary : BinaryBase, IDwarfBinary
    {
        public MachOBinary(Uri uri) : base(uri)
        {
            try
            {
                MachOResult result = MachOReader.TryLoadFat(
                                stream: File.OpenRead(Path.GetFullPath(uri.LocalPath)),
                                shouldOwnStream: true,
                                machOs: out IReadOnlyList<MachO> machOs);

                if (result == MachOResult.NotMachO)
                {
                    throw new Exception($"The binary {uri.LocalPath} is not a valid MachO binary");
                }

                this.MachOs = machOs.Select(m => new SingleMachOBinary(m, uri)).ToList();
                this.Valid = true;
            }
            catch (Exception e)
            {
                this.LoadException = e;
                this.Valid = false;
            }
        }

        public static bool CanLoadBinary(Uri uri)
        {
            try
            {
                return MachOReader.TryLoadFat(
                        stream: File.OpenRead(Path.GetFullPath(uri.LocalPath)),
                        shouldOwnStream: true,
                        machOs: out _) != MachOResult.NotMachO;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        public bool IsFatMachO => this.MachOs?.Count > 1;

        public IReadOnlyList<SingleMachOBinary> MachOs { get; }

        // IDwarfBinary which does not implement in this class but implement in SingleMachO class

        #region IDwarfBinary interface

        /// <summary>
        /// The version of Dwarf used.
        /// </summary>
        public int DwarfVersion { get; set; } = -1;

        /// <summary>
        /// Unit type of Dwarf used..
        /// </summary>
        public DwarfUnitType DwarfUnitType { get; set; }

        /// <summary>
        /// Gets address offset within module when it is loaded.
        /// </summary>
        /// <param name="address">Virtual address that points where something should be loaded.</param>
        public ulong NormalizeAddress(ulong address)
        {
            throw new NotImplementedException();
        }

        public string GetDwarfCompilerCommand()
        {
            throw new NotImplementedException();
        }

        public DwarfLanguage GetLanguage()
        {
            throw new NotImplementedException();
        }

        byte[] IDwarfBinary.DebugData => throw new NotImplementedException();

        byte[] IDwarfBinary.DebugDataDescription => throw new NotImplementedException();

        byte[] IDwarfBinary.DebugDataStrings => throw new NotImplementedException();

        byte[] IDwarfBinary.DebugLine => throw new NotImplementedException();

        byte[] IDwarfBinary.DebugFrame => throw new NotImplementedException();

        byte[] IDwarfBinary.EhFrame => throw new NotImplementedException();

        ulong IDwarfBinary.CodeSegmentOffset => throw new NotImplementedException();

        ulong IDwarfBinary.EhFrameAddress => throw new NotImplementedException();

        ulong IDwarfBinary.TextSectionAddress => throw new NotImplementedException();

        ulong IDwarfBinary.DataSectionAddress => throw new NotImplementedException();

        IReadOnlyList<DwarfPublicSymbol> IDwarfBinary.PublicSymbols => throw new NotImplementedException();

        bool IDwarfBinary.Is64bit => throw new NotImplementedException();

        public ICompiler[] Compilers => throw new NotImplementedException();

        #endregion IDwarfBinary interface
    }
}
