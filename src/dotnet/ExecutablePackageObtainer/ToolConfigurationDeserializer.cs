// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    public static class ToolConfigurationDeserializer
    {
        public static ToolConfiguration Deserialize(string pathToXml)
        {
            var serializer = new XmlSerializer(typeof(DotnetToolMetadata));

            DotnetToolMetadata dotnetToolMetadata;

            using (var fs = new FileStream(pathToXml, FileMode.Open))
            {
                var reader = XmlReader.Create(fs);

                try
                {
                    dotnetToolMetadata = (DotnetToolMetadata)serializer.Deserialize(reader);
                }
                catch (InvalidOperationException e) when (e.InnerException is XmlException)
                {
                    throw new ToolConfigurationException(
                        "Failed to retrive tool configuration exception, configuration is malformed xml. " +
                        e.InnerException.Message);
                }
            }

            var commandName = dotnetToolMetadata.CommandName;
            var toolAssemblyEntryPoint = dotnetToolMetadata.ToolAssemblyEntryPoint;

            try
            {
                return new ToolConfiguration(commandName, toolAssemblyEntryPoint);
            }
            catch (ArgumentException e)
            {
                throw new ToolConfigurationException("Configuration content error. " + e.Message);
            }
        }
    }
}
