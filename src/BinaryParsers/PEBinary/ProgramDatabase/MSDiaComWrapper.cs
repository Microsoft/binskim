// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Dia2Lib;

namespace Microsoft.CodeAnalysis.BinaryParsers.ProgramDatabase
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00000001-0000-0000-C000-000000000046")]
    internal interface IClassFactory
    {
        [PreserveSig]
        int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);
    }

    internal static class MsdiaComWrapper
    {
        [DllImport("msdia140.dll", CharSet = System.Runtime.InteropServices.CharSet.Ansi, ExactSpelling = true, BestFitMapping = false)]
        private static extern int DllGetClassObject(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid ClassId,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            out IntPtr ppvObject);

        private static void CoCreateFromMsdia(Guid clsidOfServer, Guid riid, out IntPtr pvObject)
        {
            IntPtr pClassFactory = IntPtr.Zero;
            int hr = DllGetClassObject(clsidOfServer, new Guid("00000001-0000-0000-C000-000000000046"), out pClassFactory);
            if (hr != 0)
            {
                throw new InvalidOperationException("Could not get class object.");
            }
            var classFactory = (IClassFactory)Marshal.GetObjectForIUnknown(pClassFactory);
            classFactory.CreateInstance(IntPtr.Zero, ref riid, out pvObject);
            Marshal.Release(pClassFactory);
            Marshal.ReleaseComObject(classFactory);
        }

        private const string IDiaDataSourceRiid = "79F1BB5F-B66E-48E5-B6A9-1545C323CA3D";
        private const string DiaSourceClsid = "E6756135-1E65-4D17-8576-610761398C3C";

        public static IDiaDataSource GetDiaSource()
        {
            IntPtr diaSourcePtr = IntPtr.Zero;
            CoCreateFromMsdia(new Guid(DiaSourceClsid), new Guid(IDiaDataSourceRiid), out diaSourcePtr);
            object objectForIUnknown = Marshal.GetObjectForIUnknown(diaSourcePtr);
            return objectForIUnknown as IDiaDataSource;
        }
    }
}
