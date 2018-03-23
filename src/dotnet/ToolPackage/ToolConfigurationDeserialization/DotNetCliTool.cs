﻿using System.Diagnostics;
using System.Xml.Serialization;

namespace Microsoft.DotNet.ToolPackage.ToolConfigurationDeserialization
{
    [DebuggerStepThrough]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class DotNetCliTool
    {
        [XmlArrayItem("Command", IsNullable = false)]
        public DotNetCliToolCommand[] Commands { get; set; }

        [XmlAttribute(AttributeName = "Version")]
        public string Version { get; set; }
    }
}
