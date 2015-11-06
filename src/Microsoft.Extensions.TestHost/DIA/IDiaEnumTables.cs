// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace dia2
{
    [DefaultMember("Item"), Guid("C65C2B0A-1150-4D7A-AFCC-E05BF3DEE81E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IDiaEnumTables
    {
        [DispId(1)]
        int count
        {

            get;
        }

        IEnumerator GetEnumerator();

        [return: MarshalAs(UnmanagedType.Interface)]
        IDiaTable Item([In] object index);

        void Next(uint celt, [MarshalAs(UnmanagedType.Interface)] out IDiaTable rgelt, ref uint pceltFetched);

        void Skip([In] uint celt);

        void Reset();

        void Clone([MarshalAs(UnmanagedType.Interface)] out IDiaEnumTables ppenum);
    }
}
