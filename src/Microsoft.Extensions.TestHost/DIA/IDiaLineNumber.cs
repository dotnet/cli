// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace dia2
{
    [Guid("B388EB14-BE4D-421D-A8A1-6CF7AB057086"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IDiaLineNumber
    {
        [DispId(1)]
        IDiaSymbol compiland
        {

            [return: MarshalAs(UnmanagedType.Interface)]
            get;
        }
        [DispId(2)]
        IDiaSourceFile sourceFile
        {

            [return: MarshalAs(UnmanagedType.Interface)]
            get;
        }
        [DispId(3)]
        uint lineNumber
        {

            get;
        }
        [DispId(4)]
        uint lineNumberEnd
        {

            get;
        }
        [DispId(5)]
        uint columnNumber
        {

            get;
        }
        [DispId(6)]
        uint columnNumberEnd
        {

            get;
        }
        [DispId(7)]
        uint addressSection
        {

            get;
        }
        [DispId(8)]
        uint addressOffset
        {

            get;
        }
        [DispId(9)]
        uint relativeVirtualAddress
        {

            get;
        }
        [DispId(10)]
        ulong virtualAddress
        {

            get;
        }
        [DispId(11)]
        uint length
        {

            get;
        }
        [DispId(12)]
        uint sourceFileId
        {

            get;
        }
        [DispId(13)]
        int statement
        {

            get;
        }
        [DispId(14)]
        uint compilandId
        {

            get;
        }
    }
}
