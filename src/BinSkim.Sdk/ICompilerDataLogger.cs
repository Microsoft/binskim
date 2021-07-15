// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.BinaryParsers.ProgramDatabase;

namespace Microsoft.CodeAnalysis.IL.Sdk
{
    public interface ICompilerDataLogger
    {
        void Write(string compilerData, ObjectModuleDetails omDetails);
        void Write(string compilerName, string version, string language, string file);
        void WriteException(string errorMessage);
        void PrintHeader();
    }
}
