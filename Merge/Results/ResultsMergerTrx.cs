using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Merge.Results
{
    public class ResultsMergerTrx : IMergeResults
    {
        private List<string> _trxResults = new List<string>();

        public void AddToMerge(string[] files)
        {
            foreach (var file in files.Where(s => !string.IsNullOrEmpty(s)))
            {
                switch (file.Substring(file.Length - 4).ToLower())
                {
                    case ".trx":
                        _trxResults.Add(file);
                        break;
                    default:
                        break;
                }
            }
        }

        public string[] Merge(string outputPath)
        {
            string trxTestResultFile = Path.Combine(outputPath, "TestResult.trx");
            MergeTrxFiles(trxTestResultFile);

            return new string[] { trxTestResultFile };
        }

        private void MergeTrxFiles(string trxTestResultFile)
        {
            if (_trxResults.Count == 0)
            {
                return;
            }

            XmlDocument master = new XmlDocument();
            master.Load(_trxResults[0]);

            for (int i = 1; i < _trxResults.Count; i++) // skip 0 index
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(_trxResults[i]);
                MergeTrxXml(master, xml, "TestRun");
            }

            using (var xmlWriter = XmlWriter.Create(trxTestResultFile))
            {
                master.WriteTo(xmlWriter);
            }
        }

        private void MergeTrxXml(XmlDocument master, XmlDocument xml, string xmlTagToMergeElements)
        {
            XmlElement masterElement = master.GetElementsByTagName(xmlTagToMergeElements).Item(0) as XmlElement;
            XmlElement element = xml.GetElementsByTagName(xmlTagToMergeElements).Item(0) as XmlElement;
            MergeTrxXml(masterElement, element);
        }

        private void MergeTrxXml(XmlElement master, XmlElement element)
        {
            foreach (var child in element.ChildNodes)
            {
                if (child is XmlText)
                {
                    continue;
                }
                XmlElement childElement = (XmlElement)child;

                bool found = false;
                foreach (XmlElement childMaster in master.ChildNodes)
                {
                    if (AreEqualTrxElements(childMaster, childElement))
                    {
                        MergeTrxXml(childMaster, childElement);
                        UpdateTrxElements(childMaster, childElement);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    master.AppendChild(master.OwnerDocument.ImportNode(childElement, true));
                }
            }
        }

        private bool AreEqualTrxElements(XmlElement destination, XmlElement source)
        {
            if (destination.Name != source.Name)
                return false;
            switch (destination.Name.ToLower())
            {
                case "unittestresult":
                    return destination.GetAttribute("testId") == source.GetAttribute("testId")
                            && destination.GetAttribute("testName") == source.GetAttribute("testName")
                            && destination.GetAttribute("executionId") == source.GetAttribute("executionId");
                case "unittest":
                    return destination.GetAttribute("id") == source.GetAttribute("id");
                case "execution":
                    return destination.GetAttribute("id") == source.GetAttribute("id");
                case "testmethod":
                    return destination.GetAttribute("codeBase") == source.GetAttribute("codeBase")
                            && destination.GetAttribute("className") == source.GetAttribute("className")
                            && destination.GetAttribute("name") == source.GetAttribute("name");
                case "testentry":
                    return destination.GetAttribute("testId") == source.GetAttribute("testId")
                            && destination.GetAttribute("executionId") == source.GetAttribute("executionId");
                case "testlist":
                    return destination.GetAttribute("id") == source.GetAttribute("id");
                default:
                    return true;
            }
        }

        private void UpdateTrxElements(XmlElement destination, XmlElement source)
        {
            switch (destination.Name.ToLower())
            {
                case "times":
                    SelectMinimumAttribute("creation", destination, source);
                    SelectMinimumAttribute("queuing", destination, source);
                    SelectMinimumAttribute("start", destination, source);
                    SelectMaximumAttribute("finish", destination, source);
                    break;
                case "counters":
                    UpdateAttributeInt("total", destination, source);
                    UpdateAttributeInt("executed", destination, source);
                    UpdateAttributeInt("passed", destination, source);
                    UpdateAttributeInt("failed", destination, source);
                    UpdateAttributeInt("error", destination, source);
                    UpdateAttributeInt("timeout", destination, source);
                    UpdateAttributeInt("aborted", destination, source);
                    UpdateAttributeInt("inconclusive", destination, source);
                    UpdateAttributeInt("passedButRunAborted", destination, source);
                    UpdateAttributeInt("notRunnable", destination, source);
                    UpdateAttributeInt("notExecuted", destination, source);
                    UpdateAttributeInt("disconnected", destination, source);
                    UpdateAttributeInt("warning", destination, source);
                    UpdateAttributeInt("completed", destination, source);
                    UpdateAttributeInt("inProgress", destination, source);
                    UpdateAttributeInt("pending", destination, source);
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

        private void SelectMaximumAttribute(string attributeName, XmlElement destination, XmlElement source)
        {
            var destinationValue = destination.GetAttribute(attributeName);
            var sourceValue = source.GetAttribute(attributeName);
            var maximum = (string.Compare(destinationValue, sourceValue) > 0) ? destinationValue : sourceValue;
            destination.SetAttribute(attributeName, maximum);
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