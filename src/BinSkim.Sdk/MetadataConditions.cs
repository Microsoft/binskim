// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.IL.Sdk
{
    public static class MetadataConditions
    {
        public static readonly string ImageIsNotPE = SdkResources.MetadataCondition_ImageIsNotPE;
        public static readonly string ImageIsNotElf = SdkResources.MetadataCondition_ImageIsNotElf;
        public static readonly string ImageIsNotExe = SdkResources.MetadataCondition_ImageIsNotExe;
        public static readonly string CouldNotLoadPdb = SdkResources.MetadataCondition_CouldNotLoadPdb;
        public static readonly string ImageIsNotSigned = SdkResources.MetadataCondition_ImageIsNotSigned;
        public static readonly string ImageIsWixBinary = SdkResources.MetadataCondition_ImageIsWixBinary;
        public static readonly string ImageIsXBoxBinary = SdkResources.MetadataCondition_ImageIsXboxBinary;
        public static readonly string ImageIsBootBinary = SdkResources.MetadataCondition_ImageIsBootBinary;
        public static readonly string ImageIs64BitBinary = SdkResources.MetadataCondition_ImageIs64BitBinary;
        public static readonly string ElfNotBuiltWithGcc = SdkResources.MetadataCondition_ElfNotBuiltWithGCC;
        public static readonly string ImageIsILOnlyAssembly = SdkResources.MetadataCondition_ImageIsILOnlyAssembly;
        public static readonly string ImageIsNot32BitBinary = SdkResources.MetadataCondition_ImageIsNot32BitBinary;
        public static readonly string ImageIsNot64BitBinary = SdkResources.MetadataCondition_ImageIsNot64BitBinary;
        public static readonly string ElfIsCoreNoneOrObject = SdkResources.MetadataCondition_ElfIsCoreNoneOrObject;
        public static readonly string ImageIsInteropAssembly = SdkResources.MetadataCondition_ImageIsInteropAssembly;
        public static readonly string ImageIsMixedModeBinary = SdkResources.MetadataCondition_ImageIsMixedModeBinary;
        public static readonly string ImageIsKernelModeBinary = SdkResources.MetadataCondition_ImageIsKernelModeBinary;
        public static readonly string ImageIsNotKernelModeBinary = SdkResources.MetadataCondition_ImageIsNotKernelModeBinary;
        public static readonly string ImageIsILLibraryAssembly = SdkResources.MetadataCondition_ImageIsILLibraryAssembly;
        public static readonly string ImageIsResourceOnlyBinary = SdkResources.MetadataCondition_ImageIsResourceOnlyBinary;
        public static readonly string ImageIsDotNetNativeBinary = SdkResources.MetadataCondition_ImageIsDotNetNativeBinary;
        public static readonly string ImageIsResourceOnlyAssembly = SdkResources.MetadataCondition_ImageIsResourceOnlyAssembly;
        public static readonly string ImageIsKernelModeAndNot64Bit = SdkResources.MetadataCondition_ImageIsKernelModeAndNot64Bit;
        public static readonly string ImageIsDotNetCoreBootstrapExe = SdkResources.MetadataCondition_ImageIsDotNetCoreBootstrapExe;
        public static readonly string ImageLikelyLoadsAs32BitProcess = SdkResources.MetadataCondition_ImageLikelyLoads32BitProcess;
        public static readonly string ImageIsDotNetCoreEntryPointDll = SdkResources.MetadataCondition_ImageIsDotNetCoreEntryPointDll;
        public static readonly string ImageCompiledWithOutdatedTools = SdkResources.MetadataCondition_ImageCompiledWithOutdatedTools;
        public static readonly string ImageIsDotNetNativeBootstrapExe = SdkResources.MetadataCondition_ImageIsDotNetNativeBootstrapExe;
        public static readonly string ImageIsPreVersion7WindowsCEBinary = SdkResources.MetadataCondition_ImageIsPreVersion7WindowsCEBinary;
        public static readonly string ImageIsNativeUniversalWindowsPlatformBinary = SdkResources.MetadataCondition_ImageIsNativeUniversalWindowsPlatformBinary;
    }
}
