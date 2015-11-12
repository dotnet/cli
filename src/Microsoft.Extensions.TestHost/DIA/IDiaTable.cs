// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace dia2
{
    [TypeIdentifier]
    [CompilerGenerated]
    [DefaultMember("Item"), Guid("4A59FB77-ABAC-469B-A30B-9ECC85BFEF14"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IDiaTable : IEnumUnknown
    {
        string name { get; }

        [DispId(2)]
        int count
        {
            get;
        }

        new void RemoteNext([In] uint celt, [MarshalAs(UnmanagedType.IUnknown)] out object rgelt, out uint pceltFetched);

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumUnknown ppenum);

        [return: MarshalAs(UnmanagedType.IUnknown, MarshalType = "System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler")]
        IEnumerator GetEnumerator();


        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Item([In] uint index);
    }
}
