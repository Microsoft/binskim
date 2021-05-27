﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.BinaryParsers.Dwarf
{
    /// <summary>
    /// Interface that defines image format that contains DWARF data.
    /// </summary>
    public interface IDwarfBinary : IDisposable
    {
        /// <summary>
        /// Gets the debug data.
        /// </summary>
        byte[] DebugData { get; }

        /// <summary>
        /// Gets the debug data description.
        /// </summary>
        byte[] DebugDataDescription { get; }

        /// <summary>
        /// Gets the debug data strings.
        /// </summary>
        byte[] DebugDataStrings { get; }

        /// <summary>
        /// Gets the debug line.
        /// </summary>
        byte[] DebugLine { get; }

        /// <summary>
        /// Gets the debug frame.
        /// </summary>
        byte[] DebugFrame { get; }

        /// <summary>
        /// Gets the exception handling frames used for unwinding (generated by usually GCC compiler).
        /// </summary>
        byte[] EhFrame { get; }

        /// <summary>
        /// Gets the code segment offset.
        /// </summary>
        ulong CodeSegmentOffset { get; }

        /// <summary>
        /// Gets the address of exception handling frames stream after loading into memory.
        /// </summary>
        ulong EhFrameAddress { get; }

        /// <summary>
        /// Gets the address of text section after loading into memory.
        /// </summary>
        ulong TextSectionAddress { get; }

        /// <summary>
        /// Gets the address of data section after loading into memory.
        /// </summary>
        ulong DataSectionAddress { get; }

        /// <summary>
        /// Gets the public symbols.
        /// </summary>
        IReadOnlyList<DwarfPublicSymbol> PublicSymbols { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IDwarfBinary"/> is 64 bit image.
        /// </summary>
        /// <value>
        ///   <c>true</c> if is 64 bit image; otherwise, <c>false</c>.
        /// </value>
        bool Is64bit { get; }

        /// <summary>
        /// Gets address offset within module when it is loaded.
        /// </summary>
        /// <param name="address">Virtual address that points where something should be loaded.</param>
        ulong NormalizeAddress(ulong address);
    }
}
