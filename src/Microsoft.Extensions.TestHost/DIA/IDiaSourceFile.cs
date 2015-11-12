// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace dia2
{
    [Guid("A2EF5353-F5A8-4EB3-90D2-CB526ACB3CDD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IDiaSourceFile
    {
        [DispId(2)]
        uint uniqueId
        {

            get;
        }
        [DispId(3)]
        string fileName
        {

            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }
        [DispId(4)]
        uint checksumType
        {

            get;
        }
        [DispId(5)]
        IDiaEnumSymbols compilands
        {

            [return: MarshalAs(UnmanagedType.Interface)]
            get;
        }


        void get_checksum([In] uint cbData, out uint pcbData, out byte pbData);
    }
}
