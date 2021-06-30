﻿using System;

using Microsoft.CodeAnalysis.BinaryParsers;

namespace Microsoft.CodeAnalysis.IL.Sdk
{
    internal class BinaryTargetManager
    {
        // We may want to consider changing this to an extension/plugin model rather than a hardcoded list of supported binary parsers.
        // However, for now this will do.
        public static IBinary GetBinaryFromFile(
            Uri uri,
            string symbolPath = null,
            string localSymbolDirectories = null,
            bool tracePdbLoad = false)
        {
            if (PEBinary.CanLoadBinary(uri))
            {
                return new PEBinary(uri, symbolPath, localSymbolDirectories, tracePdbLoad);
            }
            else if (ElfBinary.CanLoadBinary(uri))
            {
                return new ElfBinary(uri);
            }
            else if (MachOBinary.CanLoadBinary(uri))
            {
                return new MachOBinary(uri);
            }
            else
            {
                return null;
            }
        }
    }
}
