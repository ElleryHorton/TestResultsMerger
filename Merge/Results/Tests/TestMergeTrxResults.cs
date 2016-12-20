using NUnit.Framework;
using System.IO;
using System.Text;
using TestResultsMerger.Properties;

namespace Merge.Results.Tests
{
    [TestFixture]
    public class TestMergeTrxResults
    {
        [Test]
        public void TrxResultsWithMultipleTestRunsAreMergedCorrectly()
        {
            var merger = new ResultsMergerTrx();
            var files = new string[] {
                CreateTempFile(Encoding.UTF8.GetString(Resources.Part1of2), "trx"),
                CreateTempFile(Encoding.UTF8.GetString(Resources.Part2of2), "trx")
            };
            merger.AddToMerge(files);
            var mergedFilePath = merger.Merge(Path.GetTempPath())[0];
            var actual = File.ReadAllText(mergedFilePath);
            var expected = RemoveByteOrderMarkUTF8(Encoding.UTF8.GetString(Resources.Merged));
            Assert.AreEqual(expected, actual);
        }

        private static string CreateTempFile(string content, string extension)
        {
            var path = Path.ChangeExtension(Path.GetTempFileName(), extension);
            if (!File.Exists(path))
            {
                using (var stream = new StreamWriter(path, false, Encoding.UTF8))
                {
                    stream.Write(RemoveByteOrderMarkUTF8(content));
                    stream.Close();
                }
            }
            return path;
        }

        private static string RemoveByteOrderMarkUTF8(string content)
        {
            string byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (content.StartsWith(byteOrderMarkUtf8))
            {
                content = content.Remove(0, byteOrderMarkUtf8.Length);
            }
            return content;
        }
    }
}
