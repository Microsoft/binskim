// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis.BinaryParsers.PortableExecutable
{
    public abstract class ImageHeader
    {
        internal SafePointer m_pHeader;

        public ImageHeader(PEHeader parentHeader, SafePointer sp)
        {
            this.ParentHeader = parentHeader;
            this.m_pHeader = sp;
        }

        public PEHeader ParentHeader { get; private set; }

        public abstract ImageHeader Create(PEHeader parentHeader, SafePointer sp);

        public object GetField(int n)
        {
            object res;
            ImageFieldData fi = this.GetFieldInfo(n);
            int count = fi.Count;
            int offset = this.GetFieldOffset(n);
            object o;
            int len;

            SafePointer sp = this.m_pHeader + offset;

            if (fi.VarLen)
            {
                // this assumes we can not have varlen arrays of headers
                count = this.GetFieldSize(n) / fi.GetTypeLen();
            }

            if (count == 1)
            {
                return sp.SafePointerToType(fi);
            }

            // if a negative count is provided we tread it as an offset of
            // the field where to find the real count
            if (count < 0)
            {
                count = Convert.ToInt32(this.GetField(n + count));
            }

            res = new object[count];
            for (int i = 0; i < count; i++)
            {
                o = sp.SafePointerToType(fi);
                ((object[])res)[i] = o;
                len = (o is ImageHeader header)
                    ? header.Size
                    : fi.GetTypeLen();
                sp = sp + len;
            }

            return res;
        }

        public object GetField(object o)
        {
            return this.GetField((int)o);
        }

        public ImageFieldData GetFieldInfo(int n)
        {
            return this.GetFields()[n];
        }

        public int GetFieldSize(int n)
        {
            ImageFieldData fi = this.GetFieldInfo(n);
            int count = fi.Count;
            int padding = fi.PadTo;
            int size;
            int len;

            if (fi.VarLen)
            {
                SafePointer field_start = this.m_pHeader + this.GetFieldOffset(n);
                SafePointer field_end = field_start;
                while (field_end != fi.TrailingByte)
                {
                    field_end++;
                }
                // if we have a VarLen and a PadTo specified we will make sure
                // we only skip up to PadTo trailing bytes
                padding = (padding != 0)
                    ? padding - (field_end.Address - this.m_pHeader.Address) % padding
                    : -1;

                while ((field_end == fi.TrailingByte) && (padding-- != 0))
                {
                    field_end++;
                }

                return field_end - field_start;
            }

            if (fi.Type == Type.HEADER)
            {
                object o = this.GetField(n);
                len = (o is Array)
                    ? ((ImageHeader)((object[])o)[0]).Size
                    : ((ImageHeader)o).Size;
            }
            else
            {
                len = fi.GetTypeLen();
            }

            if (count == 1)
            {
                return len;
            }

            // if a negative count is provided we tread it as an offset of
            // the field where to find the real count
            if (count < 0)
            {
                count = Convert.ToInt32(this.GetField(n + count));
            }

            size = len * count;

            // We don't pad is the size is already a multiple of padding
            if ((padding != 0) && (size % padding != 0))
            {
                size += padding - (size % padding);
            }

            return size;
        }

        public int GetFieldOffset(int n)
        {
            ImageFieldData fi = this.GetFieldInfo(n);
            int offset = fi.Offset;

            if (offset >= 0)
            {
                return offset;
            }

            return this.GetFieldOffset(n + offset) + this.GetFieldSize(n + offset);
        }

        protected abstract ImageFieldData[] GetFields();

        public int NumberOfFields => this.GetFields().Length;

        public static int ShiftOffset(ImageFieldData[] fields, int n)
        {
            if ((fields[n].Offset < 0) || (fields[n].Count < 0))
            {
                return -1;
            }

            return fields[n].Offset + fields[n].Count * fields[n].GetTypeLen();
        }

        public int Size
        {
            get
            {
                int n = this.GetFields().Length - 1;
                return this.GetFieldOffset(n) + this.GetFieldSize(n);
            }
        }
    }
}
