// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Microsoft.Extensions.Testing.Abstractions
{
    public class PortablePdbReader : IPdbReader
    {
        private MetadataReader _reader;

        public PortablePdbReader(Stream pdbStream)
        {
            pdbStream.Position = 0;
            var buffer = new byte[pdbStream.Length];
            pdbStream.Read(buffer, 0, buffer.Length);

            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    _reader = new MetadataReader(bufferPtr, buffer.Length);
                }
            }
        }

        public SourceInformation GetSourceInformation(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                return null;
            }

            var handle = methodInfo.GetMethodDebugInformationHandle();

            return GetSourceInformation(handle);
        }

        private SourceInformation GetSourceInformation(MethodDebugInformationHandle handle)
        {
            SourceInformation sourceInformation = null;
            try
            {
                var methodDebugDefinition = GetMethodDebugInformation(handle);
                var fileName = GetMethodFileName(methodDebugDefinition);
                var lineNumber = GetMethodStartLineNumber(methodDebugDefinition);

                sourceInformation = new SourceInformation(fileName, lineNumber);
            }
            catch (BadImageFormatException)
            {
            }

            return sourceInformation;
        }

        private MethodDebugInformation GetMethodDebugInformation(MethodDebugInformationHandle handle)
        {
            var methodDebugDefinition = _reader.GetMethodDebugInformation(handle);
            return methodDebugDefinition;
        }

        private static int GetMethodStartLineNumber(MethodDebugInformation methodDebugDefinition)
        {
            var sequencePoint =
                methodDebugDefinition.GetSequencePoints().OrderBy(s => s.StartLine).FirstOrDefault();
            var lineNumber = sequencePoint.StartLine;
            return lineNumber;
        }

        private string GetMethodFileName(MethodDebugInformation methodDebugDefinition)
        {
            var fileName = string.Empty;
            if (!methodDebugDefinition.Document.IsNil)
            {
                var document = _reader.GetDocument(methodDebugDefinition.Document);
                fileName = _reader.GetString(document.Name);
            }

            return fileName;
        }

        public void Dispose()
        {
        }
    }
}
