using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Database.Managers.LibraryFiles;

public class MySqlLibraryFileManager: ILibraryFileManager
{
    private MySqlDbManager dbManager;
    public MySqlLibraryFileManager(MySqlDbManager dbManager)
    {
        this.dbManager = dbManager;
    }
    
    public async Task<LibraryFile> Get(Guid uid)
    {
        using var db = await dbManager.GetDb();
        return db.Db.Single<LibraryFile>("where Uid = @0", uid);
    }

    public async Task<IEnumerable<LibraryFile>> GetAll()
    {
        using var db = await dbManager.GetDb();
        return await db.Db.FetchAsync<LibraryFile>();;
    }

    public Task AddMany(LibraryFile[] libraryFiles)
    {
        throw new NotImplementedException();
    }

    public async Task<LibraryFile> Update(LibraryFile file)
    {
        using var db = await dbManager.GetDb();
        await db.Db.UpdateAsync(file);
        return await Get(file.Uid);
    }

    public Task<NextLibraryFileResult> GetNext(NextLibraryFileArgs args)
    {
        throw new NotImplementedException();
    }

    public Task<LibraryFileDatalistModel> ListAll(FileStatus status, int page = 0, int pageSize = 0)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<LibraryFile>> GetAll(FileStatus? status, int skip = 0, int top = 0)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<LibraryFile>> OrderLibraryFiles(IEnumerable<LibraryFile> libraryFiles, FileStatus status)
    {
        throw new NotImplementedException();
    }

    public Task ResetProcessingStatus(Guid nodeUid)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<LibraryStatus>> GetStatus()
    {
        throw new NotImplementedException();
    }

    public Task MoveToTop(ReferenceModel<Guid> model)
    {
        throw new NotImplementedException();
    }
}