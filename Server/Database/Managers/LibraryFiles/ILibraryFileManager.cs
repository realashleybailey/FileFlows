using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Database.Managers.LibraryFiles;

public interface ILibraryFileManager
{
    Task<LibraryFile> Get(Guid uid);

    Task<IEnumerable<LibraryFile>> GetAll();

    Task AddMany(LibraryFile[] libraryFiles);
    
    Task<LibraryFile> Update(LibraryFile file);

    Task<NextLibraryFileResult> GetNext(NextLibraryFileArgs args);

    Task<LibraryFileDatalistModel> ListAll(FileStatus status, int page = 0, int pageSize = 0);

    Task<IEnumerable<LibraryFile>> GetAll(FileStatus? status, int skip = 0, int top = 0);

    Task<IEnumerable<LibraryFile>> OrderLibraryFiles(IEnumerable<LibraryFile> libraryFiles, FileStatus status);
    
    Task ResetProcessingStatus(Guid nodeUid);

    Task<IEnumerable<LibraryStatus>> GetStatus();

    Task MoveToTop(ReferenceModel<Guid> model);
}