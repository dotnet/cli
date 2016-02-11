using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities.Assertions;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public class AppConfig
    {
        private const string AssemblyBindingNamespace = "urn:schemas-microsoft-com:asm.v1";
        private readonly string _path;
        private readonly XDocument _document;
        private List<BindingRedirect> _bindingRedirects;

        public List<BindingRedirect> BindingRedirects => _bindingRedirects ?? (_bindingRedirects = ParseBindingRedirects());

        public AppConfig(string path)
        {
            _path = path;
            _document = XDocument.Load(path);
        }

        public AppConfigAssertions Should()
        {
            return new AppConfigAssertions(this);
        }

        private List<BindingRedirect> ParseBindingRedirects()
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(_document.CreateReader().NameTable);
            namespaceManager.AddNamespace("b", AssemblyBindingNamespace);

            IEnumerable<XElement> bindings =
                _document.XPathSelectElements(
                    "//configuration/runtime/b:assemblyBinding/b:dependentAssembly", namespaceManager);

            return bindings.Select(GetBindingRedirect).ToList();
        }

        private static BindingRedirect GetBindingRedirect(XElement element)
        {
            XElement identity = element.Element(XName.Get("assemblyIdentity", AssemblyBindingNamespace));
            XElement redirect = element.Element(XName.Get("bindingRedirect", AssemblyBindingNamespace));
            return new BindingRedirect(
                identity.Attribute("name")?.Value,
                identity.Attribute("publicKeyToken")?.Value,
                identity.Attribute("culture")?.Value,
                redirect.Attribute("oldVersion")?.Value,
                redirect.Attribute("newVersion")?.Value);
        }

        public class BindingRedirect
        {
            public string AssemblyName { get; private set; }
            public string PublicKeyToken { get; private set; }
            public string Culture { get; private set; }

            public string FromVersion { get; private set; }
            public string ToVersion { get; private set; }

            public BindingRedirect(string assemblyName, string publicKeyToken, string culture, string fromVersion,
                string toVersion)
            {
                AssemblyName = assemblyName;
                PublicKeyToken = publicKeyToken;
                Culture = culture;
                FromVersion = fromVersion;
                ToVersion = toVersion;
            }
        }
    }
}
