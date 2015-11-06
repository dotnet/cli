// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace dia2
{
    [DefaultMember("Item"), Guid("FE30E878-54AC-44F1-81BA-39DE940F6052"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IDiaEnumLineNumbers
    {
        [DispId(1)]
        int count
        {

            get;
        }

        IEnumerator GetEnumerator();

        [return: MarshalAs(UnmanagedType.Interface)]
        IDiaLineNumber Item([In] uint index);

        void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] out IDiaLineNumber rgelt, out uint pceltFetched);

        void Skip([In] uint celt);

        void Reset();

        void Clone([MarshalAs(UnmanagedType.Interface)] out IDiaEnumLineNumbers ppenum);
    }
}