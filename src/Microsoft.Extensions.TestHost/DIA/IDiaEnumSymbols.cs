// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace dia2
{
    [DefaultMember("Item"), Guid("CAB72C48-443B-48F5-9B0B-42F0820AB29A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IDiaEnumSymbols
    {
        [DispId(1)]
        int count
        {

            get;
        }

        IEnumerator GetEnumerator();

        [return: MarshalAs(UnmanagedType.Interface)]
        IDiaSymbol Item([In] uint index);

        void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] out IDiaSymbol rgelt, out uint pceltFetched);

        void Skip([In] uint celt);

        void Reset();

        void Clone([MarshalAs(UnmanagedType.Interface)] out IDiaEnumSymbols ppenum);
    }
}