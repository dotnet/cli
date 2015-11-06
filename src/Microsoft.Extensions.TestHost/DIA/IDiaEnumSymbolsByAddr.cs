// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace dia2
{
	[Guid("624B7D9C-24EA-4421-9D06-3B577471C1FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface IDiaEnumSymbolsByAddr
	{
		
		[return: MarshalAs(UnmanagedType.Interface)]
		IDiaSymbol symbolByAddr([In] uint isect, [In] uint offset);
		
		[return: MarshalAs(UnmanagedType.Interface)]
		IDiaSymbol symbolByRVA([In] uint relativeVirtualAddress);
		
		[return: MarshalAs(UnmanagedType.Interface)]
		IDiaSymbol symbolByVA([In] ulong virtualAddress);
		
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] out IDiaSymbol rgelt, out uint pceltFetched);
		
		void Prev([In] uint celt, [MarshalAs(UnmanagedType.Interface)] out IDiaSymbol rgelt, out uint pceltFetched);
		
		void Clone([MarshalAs(UnmanagedType.Interface)] out IDiaEnumSymbolsByAddr ppenum);
	}
}
