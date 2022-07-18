namespace FileFlows.Client.Pages;

/// <summary>
/// Page for revisions 
/// </summary>
public partial class Revisions: ListPage<Guid, RevisionedObject>
{
    public override string ApiUrl => "/api/revision";

    public override string FetchUrl => $"{ApiUrl}/list";

    public override Task<bool> Edit(RevisionedObject item) => Task.FromResult(false);
}