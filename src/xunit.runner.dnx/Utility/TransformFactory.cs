using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Xunit.Runner.Dnx
{
    public class TransformFactory
    {
        static readonly TransformFactory instance = new TransformFactory();

        readonly Dictionary<string, Transform> availableTransforms = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);

        protected TransformFactory()
        {
            availableTransforms.Add("xml", new Transform { CommandLine = "xml", Description = "output results to xUnit.net v2 style XML file", OutputHandler = Handler_DirectWrite });
        }

        public static List<Transform> AvailableTransforms
        {
            get { return instance.availableTransforms.Values.ToList(); }
        }

        public static List<Action<XElement>> GetXmlTransformers(XunitProject project)
        {
            return project.Output.Select(output => new Action<XElement>(xml => instance.availableTransforms[output.Key].OutputHandler(xml, output.Value))).ToList();
        }

        static void Handler_DirectWrite(XElement xml, string outputFileName)
        {
            // Create the output parent directories if they don't exist.
            int lastSlashIdx = outputFileName.LastIndexOf('/');
            if (lastSlashIdx != -1)
                Directory.CreateDirectory(outputFileName.Substring(0, lastSlashIdx));

            using (var stream = File.Open(outputFileName, FileMode.Create))
                xml.Save(stream);
        }
    }
}
