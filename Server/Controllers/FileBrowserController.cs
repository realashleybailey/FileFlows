namespace FileFlow.Server.Controllers
{
    using System.IO;
    using System.Diagnostics;
    using Microsoft.AspNetCore.Mvc;
    using FileFlow.Server;
    using FileFlow.Shared.Models;
    using FileFlow.Server.Helpers;
    using System.Runtime.InteropServices;

    [Route("/api/file-browser")]
    public class FileBrowserController : Controller
    {
        [HttpGet]
        public IEnumerable<FileBrowserItem> GetItems([FromQuery] string start, [FromQuery] bool includeFiles, [FromQuery] string[] extensions)
        {
            if (start == "ROOT")
            {
                // special case for windows we list the drives
                return System.IO.DriveInfo.GetDrives().Where(x => x.IsReady).Select(x => new FileBrowserItem
                {
                    IsDrive = true,
                    Name = x.Name,
                    FullName = x.RootDirectory.FullName
                });
            }

            if (string.IsNullOrEmpty(start))
                start = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            else if (System.IO.File.Exists(start))
                start = new FileInfo(start).DirectoryName;
            else if (Directory.Exists(start) == false)
                start = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var items = new List<FileBrowserItem>();
            var di = new DirectoryInfo(start);
            if (di.Exists)
            {
                if (di.Parent?.Exists == true)
                {
                    items.Add(new FileBrowserItem
                    {
                        IsParent = true,
                        FullName = di.Parent.FullName,
                        Name = di.FullName
                    });
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    items.Add(new FileBrowserItem
                    {
                        IsParent = true,
                        FullName = "ROOT",
                        Name = di.FullName
                    });
                }
                foreach (var dir in di.GetDirectories())
                {
                    if ((dir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        continue;

                    items.Add(new FileBrowserItem { FullName = dir.FullName, Name = dir.Name, IsPath = true });
                }
                if (includeFiles)
                {
                    foreach (var file in di.GetFiles())
                    {
                        if ((file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;
                        items.Add(new FileBrowserItem { FullName = file.FullName, Name = file.Name });
                    }
                }
            }
            return items;
        }
    }

}