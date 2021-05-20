﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;

using FluentAssertions;

using Microsoft.CodeAnalysis.BinaryParsers.Dwarf;

using Xunit;

namespace Microsoft.CodeAnalysis.BinaryParsers.ELF
{
    public class ELFBinaryTests
    {
        internal static string TestData = GetTestDirectory("Test.UnitTests.BinaryParsers" + Path.DirectorySeparatorChar + "TestsData");

        internal static string GetTestDirectory(string relativeDirectory)
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            string codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            string dirPath = Path.GetDirectoryName(codeBasePath);
            dirPath = Path.Combine(dirPath, string.Format("..{0}..{0}..{0}..{0}src{0}", Path.DirectorySeparatorChar));
            dirPath = Path.GetFullPath(dirPath);
            return Path.Combine(dirPath, relativeDirectory);
        }

        [Fact]
        public void ValidateDwarfV4_WithO2()
        {
            // Hello.c compiled using: gcc -Wall -O2 -g -gdwarf-4 hello.c -o hello4
            string fileName = Path.Combine(TestData, "Dwarf/hello-dwarf4-o2");
            using var binary = new ELFBinary(new Uri(fileName));
            binary.GetDwarfVersion().Should().Be(4);
            binary.GetDwarfCompilerCommand().Should().Contain("O2");
            binary.GetLanguage().Should().Be(DwarfLanguage.C99);
        }

        [Fact]
        public void ValidateDwarfV5_WithO2()
        {
            // Hello.c compiled using: gcc -Wall -O2 -g -gdwarf-5 hello.c -o hello5
            string fileName = Path.Combine(TestData, "Dwarf/hello-dwarf5-o2");
            using var binary = new ELFBinary(new Uri(fileName));
            binary.GetDwarfVersion().Should().Be(5);
            binary.GetDwarfCompilerCommand().Should().Contain("O2");
            binary.GetLanguage().Should().Be(DwarfLanguage.C11);
        }

        [Fact]
        public void ValidateDwarfV4_WithO2_Split_DebugFileExists()
        {
            // dwotest.cpp compiled using: gcc -Wall -O2 -g -gdwarf-4 dwotest.cpp -gsplit-dwarf -o dwotest.gcc.4.o
            string fileName = Path.Combine(TestData, "Dwarf/DwarfSplitV4/dwotest.gcc.4.o");
            using var binary = new ELFBinary(new Uri(fileName));
            binary.GetDwarfVersion().Should().Be(4);
            binary.GetLanguage().Should().Be(DwarfLanguage.CPlusPlus);
        }

        [Fact]
        public void ValidateDwarfV5_WithO2_Split_DebugFileExists()
        {
            // dwotest.cpp compiled using: gcc -Wall -O2 -g -gdwarf-5 dwotest.cpp -gsplit-dwarf -o dwotest.gcc.5.o
            string fileName = Path.Combine(TestData, "Dwarf/DwarfSplitV5/dwotest.gcc.5.o");
            using var binary = new ELFBinary(new Uri(fileName));
            binary.GetDwarfVersion().Should().Be(5);
            binary.GetLanguage().Should().Be(DwarfLanguage.CPlusPlus14);
        }

        [Fact]
        public void ValidateDwarfV4_WithO2_Split_DebugFileMissing()
        {
            // dwotest.cpp compiled using: gcc -Wall -O2 -g -gdwarf-4 dwotest.cpp -gsplit-dwarf -o dwotest.gcc.4.o
            string fileName = Path.Combine(TestData, "Dwarf/DwarfSplitV4DebugFileMissing/dwotest.gcc.4.o");
            using var binary = new ELFBinary(new Uri(fileName));
            binary.GetDwarfVersion().Should().Be(4);
            binary.GetLanguage().Should().Be(DwarfLanguage.Unknown); //missing dwo file should not cause exception
        }

        [Fact]
        public void ValidateDwarfV5_WithO2_Split_DebugFileMissing()
        {
            // dwotest.cpp compiled using: gcc -Wall -O2 -g -gdwarf-5 dwotest.cpp -gsplit-dwarf -o dwotest.gcc.5.o
            string fileName = Path.Combine(TestData, "Dwarf/DwarfSplitV5DebugFileMissing/dwotest.gcc.5.o");
            using var binary = new ELFBinary(new Uri(fileName));
            binary.GetDwarfVersion().Should().Be(5);
            binary.GetLanguage().Should().Be(DwarfLanguage.Unknown); //missing dwo file should not cause exception
        }

        [Fact]
        public void ValidateDwarfV5_WithO2_No_Stack_Clash_Protection()
        {
            // helloworld.cpp compiled using: gcc -fno-stack-clash-protection -Wall -O2 -g -gdwarf-5 helloworld.cpp -o gcc.helloworld.5.o.no-stack-clash-protection
            string fileName = Path.Combine(TestData, "Dwarf/gcc.helloworld.5.o.no-stack-clash-protection");
            using var binary = new ELFBinary(new Uri(fileName));
            binary.GetDwarfCompilerCommand().Should().Contain("-fno-stack-clash-protection");
            binary.GetDwarfCompilerCommand().Should().NotContain("-fstack-clash-protection");
        }

        [Fact]
        public void ValidateDwarfV4_WithO2_No_Stack_Clash_Protection()
        {
            // helloworld.cpp compiled using: gcc -fno-stack-clash-protection -Wall -O2 -g -gdwarf-4 helloworld.cpp -o gcc.helloworld.4.o.no-stack-clash-protection
            string fileName = Path.Combine(TestData, "Dwarf/gcc.helloworld.4.o.no-stack-clash-protection");
            using var binary = new ELFBinary(new Uri(fileName));
            binary.GetDwarfCompilerCommand().Should().Contain("-fno-stack-clash-protection");
            binary.GetDwarfCompilerCommand().Should().NotContain("-fstack-clash-protection");
        }

        [Fact]
        public void ValidateDwarfV5_WithO2_With_Stack_Clash_Protection()
        {
            // Hello.c compiled using: gcc -Wall -O2 -g -gdwarf-5 hello.c -o hello5
            string fileName = Path.Combine(TestData, "Dwarf/hello-dwarf5-o2");
            using var binary = new ELFBinary(new Uri(fileName));
            binary.GetDwarfCompilerCommand().Should().NotContain("-fno-stack-clash-protection");
            binary.GetDwarfCompilerCommand().Should().Contain("-fstack-clash-protection");
        }

        [Fact]
        public void ValidateDwarfV4_WithO2_With_Stack_Clash_Protection()
        {
            // Hello.c compiled using: gcc -Wall -O2 -g -gdwarf-4 hello.c -o hello4
            string fileName = Path.Combine(TestData, "Dwarf/hello-dwarf4-o2");
            using var binary = new ELFBinary(new Uri(fileName));
            binary.GetDwarfCompilerCommand().Should().NotContain("-fno-stack-clash-protection");
            binary.GetDwarfCompilerCommand().Should().Contain("-fstack-clash-protection");
        }
    }
}
