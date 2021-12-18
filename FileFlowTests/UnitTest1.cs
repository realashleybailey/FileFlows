using FileFlows.Shared.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FileFlowTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var node = new ProcessingNode();
            node.Mappings = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("/media/downloads", @"\\tower\downloads\sabnzbd"),
                new KeyValuePair<string, string>("/media/movies", @"\\tower\movies"),
                new KeyValuePair<string, string>("/media/tv", @"\\tower\tv"),
            };
            string path = node.Map("/media/downloads/converted/tv/SomeFolder/SomeFile.mkv");
            Assert.AreEqual(@"\\tower\downloads\sabnzbd\converted\tv\SomeFolder\SomeFile.mkv", path);
        }
    }
}