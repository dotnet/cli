﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    public static class ToolConfigurationDeserializer
    {
        public static ToolConfiguration Deserialize(string pathToXml)
        {
            var serializer = new XmlSerializer(typeof(DotNetCliTool));

            DotNetCliTool dotNetCliTool;

            using (var fs = new FileStream(pathToXml, FileMode.Open))
            {
                var reader = XmlReader.Create(fs);

                try
                {
                    dotNetCliTool = (DotNetCliTool)serializer.Deserialize(reader);
                }
                catch (InvalidOperationException e) when (e.InnerException is XmlException)
                {
                    throw new ToolConfigurationException(
                        "Failed to retrive tool configuration exception, configuration is malformed xml. " +
                        e.InnerException.Message);
                }
            }

            if (dotNetCliTool.Commands.Length != 1)
            {
                throw new ToolConfigurationException(
                    "Failed to retrive tool configuration exception, one and only one command is supported.");
            }

            if (dotNetCliTool.Commands[0].Runner != "dotnet")
            {
                throw new ToolConfigurationException(
                    "Failed to retrive tool configuration exception, only dotnet as runner is supported.");
            }

            var commandName = dotNetCliTool.Commands[0].Name;
            var toolAssemblyEntryPoint = dotNetCliTool.Commands[0].EntryPoint;

            try
            {
                return new ToolConfiguration(commandName, toolAssemblyEntryPoint);
            }
            catch (ArgumentException e)
            {
                throw new ToolConfigurationException("Configuration content error. " + e.Message);
            }
        }

        [DebuggerStepThrough]
        [XmlRoot(Namespace = "", IsNullable = false)]
        public class DotNetCliTool
        {
            [XmlArrayItem("Command", IsNullable = false)]
            public DotNetCliToolCommand[] Commands { get; set; }
        }

        [Serializable]
        [DebuggerStepThrough]
        [XmlType(AnonymousType = true)]
        public class DotNetCliToolCommand
        {
            [XmlAttribute]
            public string Name { get; set; }

            [XmlAttribute]
            public string EntryPoint { get; set; }

            [XmlAttribute]
            public string Runner { get; set; }
        }
    }
}
