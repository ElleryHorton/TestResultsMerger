using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Merge.Results
{
    public class ResultsMergerNUnit : IMergeResults
    {
        private List<string> _txtResults = new List<string>();
        private List<string> _xmlResults = new List<string>();

        public void AddToMerge(string[] files)
        {
            foreach (var file in files)
            {
                switch (file.Substring(file.Length - 4).ToLower())
                { 
                    case ".txt":
                        _txtResults.Add(file);
                        break;
                    case ".xml":
                        _xmlResults.Add(file);
                        break;
                    default:
                        break;
                }
            }
        }

        public string[] Merge(string outputPath)
        {
            string txtTestResultFile = Path.Combine(outputPath, "TestResult.txt");
            MergeNUnitOutFiles(txtTestResultFile);

            string xmlTestResultFile = Path.Combine(outputPath, "TestResult.xml");
            MergeNUnitXmlFiles(xmlTestResultFile);

            return new string[] { txtTestResultFile, xmlTestResultFile };
        }

        private void MergeNUnitOutFiles(string txtTestResultFile)
        {
            using (var writer = File.CreateText(txtTestResultFile))
            {
                foreach (var file in _txtResults)
                {
                    var contents = File.ReadAllText(file);
                    writer.Write(contents);
                }
            }
        }

        private void MergeNUnitXmlFiles(string xmlTestResultFile)
        {
            if (_xmlResults.Count == 0)
            {
                return;
            }

            XmlDocument master = new XmlDocument();
            master.Load(_xmlResults[0]);

            for (int i = 1; i < _xmlResults.Count; i++) // skip 0 index
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(_xmlResults[i]);
                MergeNUnitXml(master, xml);
            }

            using (var xmlWriter = XmlWriter.Create(xmlTestResultFile))
            {
                master.WriteTo(xmlWriter);
            }
        }

        private void MergeNUnitXml(XmlDocument master, XmlDocument xml)
        {
            const string testResultsTag = "test-results";
            foreach (XmlElement element in xml.GetElementsByTagName(testResultsTag))
            {
                MergeNUnitXml(master, element, string.Empty, string.Empty);
            }
        }

        private void MergeNUnitXml(XmlDocument master, XmlElement element, string parentName, string path)
        {
            var nodePattern = string.Format("{0}/{1}", path, element.Name);
            if (element.HasAttribute("name"))
            {
                var nodes = master.SelectNodes(nodePattern);
                bool found = false;
                foreach (XmlElement child in nodes)
                {
                    if (AreEqualNUnitXmlElements(child, element))
                    {
                        UpdateNUnitXmlElements(child, element);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    bool appendToTestSuiteNode = string.IsNullOrEmpty(path);
                    if (appendToTestSuiteNode)
                    {
                        XmlElement parent = master.SelectSingleNode(nodePattern) as XmlElement;
                        UpdateNUnitXmlElements(parent, element); // merge test-results node despite different names
                        foreach (XmlElement child in element.ChildNodes)
                        {
                            parent.AppendChild(master.ImportNode(child, true));
                        }
                    }
                    else
                    {
                        var parentNodes = master.SelectNodes(path);
                        foreach (XmlElement parent in parentNodes)
                        {
                            if (parent.GetAttribute("name") == parentName)
                            {
                                parent.AppendChild(parent.OwnerDocument.ImportNode(element, true));
                                break;
                            }
                        }
                    }
                }
            }
            foreach (var child in element)
            {
                if (child.GetType() == typeof(XmlElement))
                {
                    MergeNUnitXml(master, child as XmlElement, element.GetAttribute("name"), nodePattern);
                }
            }
        }

        private bool AreEqualNUnitXmlElements(XmlElement destination, XmlElement source)
        {
            switch (destination.Name.ToLower())
            {
                case "test-results":
                case "test-case":
                    return destination.GetAttribute("name") == source.GetAttribute("name");
                case "test-suite":
                    return destination.GetAttribute("name") == source.GetAttribute("name") &&
                           destination.GetAttribute("type") == source.GetAttribute("type");
                default:
                    return true;
            }
        }

        private void UpdateNUnitXmlElements(XmlElement destination, XmlElement source)
        {
            switch (destination.Name.ToLower())
            {
                case "test-results":
                    UpdateAttributeInt("total", destination, source);
                    UpdateAttributeInt("errors", destination, source);
                    UpdateAttributeInt("failures", destination, source);
                    UpdateAttributeInt("not-run", destination, source);
                    UpdateAttributeInt("inconclusive", destination, source);
                    UpdateAttributeInt("ignored", destination, source);
                    UpdateAttributeInt("skipped", destination, source);
                    UpdateAttributeInt("invalid", destination, source);
                    SelectMinimumAttribute("date", destination, source);
                    SelectMinimumAttribute("time", destination, source);
                    break;
                case "test-suite":
                case "test-case":
                    UpdateAttribute("executed", "True", destination, source);
                    UpdateAttribute("result", "Success", destination, source);
                    UpdateAttribute("success", "True", destination, source);
                    UpdateAttributeDouble("time", destination, source);
                    UpdateAttributeInt("asserts", destination, source);
                    break;
                default:
                    break;
            }
        }

        private void UpdateAttributeInt(string attributeName, XmlElement destination, XmlElement source)
        {
            var destinationValue = Convert.ToInt32(destination.GetAttribute(attributeName));
            var sourceValue = Convert.ToInt32(source.GetAttribute(attributeName));
            var sumValue = (destinationValue + sourceValue).ToString();
            destination.SetAttribute(attributeName, sumValue);
        }

        private void UpdateAttributeDouble(string attributeName, XmlElement destination, XmlElement source)
        {
            var destinationValue = Convert.ToDouble(destination.GetAttribute(attributeName));
            var sourceValue = Convert.ToDouble(source.GetAttribute(attributeName));
            var sumValue = (destinationValue + sourceValue).ToString();
            destination.SetAttribute(attributeName, sumValue);
        }

        private void SelectMinimumAttribute(string attributeName, XmlElement destination, XmlElement source)
        {
            var destinationValue = destination.GetAttribute(attributeName);
            var sourceValue = source.GetAttribute(attributeName);
            var minimum = (string.Compare(destinationValue, sourceValue) < 0) ? destinationValue : sourceValue;
            destination.SetAttribute(attributeName, minimum);
        }

        private void UpdateAttribute(string attributeName, string recessiveValue, XmlElement destination, XmlElement source)
        {
            var destinationValue = destination.GetAttribute(attributeName);
            var sourceValue = source.GetAttribute(attributeName);
            if (destinationValue == recessiveValue)
            {
                destination.SetAttribute(attributeName, sourceValue);
            }
        }
    }
}