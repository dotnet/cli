// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace dia2
{
    [DefaultMember("Item"), Guid("08CBB41E-47A6-4F87-92F1-1C9C87CED044"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IDiaEnumDebugStreams
    {
        [DispId(1)]
        int count
        {

            get;
        }

        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumerator GetEnumerator();

        [return: MarshalAs(UnmanagedType.Interface)]
        IDiaEnumDebugStreamData Item([In] object index);

        void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] out IDiaEnumDebugStreamData rgelt, out uint pceltFetched);

        void Skip([In] uint celt);

        void Reset();

        void Clone([MarshalAs(UnmanagedType.Interface)] out IDiaEnumDebugStreams ppenum);
    }
}