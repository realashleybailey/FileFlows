using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Database.Managers.LibraryFiles;

public class SqliteLibraryFileManager : ILibraryFileManager
{
    public Task<LibraryFile> Get(Guid uid)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<LibraryFile>> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task AddMany(LibraryFile[] libraryFiles)
    {
        throw new NotImplementedException();
    }

    public Task<LibraryFile> Update(LibraryFile file)
    {
        throw new NotImplementedException();
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