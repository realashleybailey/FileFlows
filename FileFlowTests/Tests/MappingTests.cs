namespace FileFlowTests.Tests;

[TestClass]
public class MappingTests
{
    const string testFile = "tv.shows/kids.shows/Series Name/Season 1/Series Name - S01E07-E08 - Episode Name WEBRip-1080p.mkv";

    private ProcessingNode GetProcessingNode()
    {
        var node = new ProcessingNode();
        node.DirectorySeperatorChar = '/';
        node.Mappings = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("/usr/local/bin/ffmeg", "/Applications/ffmpeg"),
                new KeyValuePair<string, string>("/media", "/Volumes/media")
            };
        return node;
    }

    [TestMethod]
    public void Mapping_NoMap()
    {
        var node = GetProcessingNode();
        string result = node.Map("/Volumes/media/" + testFile);
        Assert.AreEqual("/Volumes/media/" + testFile, result);
    }

    [TestMethod]
    public void Mapping_Map()
    {
        var node = GetProcessingNode();
        string result = node.Map("/media/" + testFile);
        Assert.AreEqual("/Volumes/media/" + testFile, result);
    }

    [TestMethod]
    public void Mapping_UnMap()
    {
        var node = GetProcessingNode();
        string result = node.UnMap("/Volumes/media/" + testFile);
        Assert.AreEqual("/media/" + testFile, result);
    }

    [TestMethod]
    public void Mapping_NoUnMap()
    {
        var node = GetProcessingNode();
        string result = node.UnMap("/no/Volumes/media/" + testFile);
        Assert.AreEqual("/no/Volumes/media/" + testFile, result);
    }

    [TestMethod]
    public void Mapping_Map_NoDouble()
    {
        var node = GetProcessingNode();
        string result = node.Map("/media/" + testFile);
        Assert.AreEqual("/Volumes/media/" + testFile, result);

        string result2 = node.Map(result);
        Assert.AreEqual(result, result2);
    }

    [TestMethod]
    public void Mapping_UnMap_NoDouble()
    {
        var node = GetProcessingNode();
        string result = node.UnMap("/Volumes/media/" + testFile);
        Assert.AreEqual("/media/" + testFile, result);

        string result2 = node.UnMap(result);
        Assert.AreEqual(result, result2);
    }
}
