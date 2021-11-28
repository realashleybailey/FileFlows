namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;

    [Route("/api/library-file")]
    public class LibraryFileController : Controller
    {
        [HttpGet]
        public IEnumerable<LibraryFile> GetAll([FromQuery] FileStatus? status)
        {
            if (Globals.Demo) {
                if (status == null)
                    status = FileStatus.Unprocessed;
                return Enumerable.Range(1, status == FileStatus.Processing ? 1 : 10).Select(x => new LibraryFile {
                    DateCreated = DateTime.Now,
                    DateModified = DateTime.Now,
                    Flow = new ObjectReference
                    {
                        Name = "Flow",
                        Uid = Guid.NewGuid()
                    },
                    Library = new ObjectReference
                    {
                        Name = "Library",
                        Uid = Guid.NewGuid(),
                    },
                    Name = "File_" + x + ".ext",
                    RelativePath = "File_" + x + ".ext",
                    Uid = Guid.NewGuid(),
                    Status = status.Value,
                    OutputPath = status == FileStatus.Processed ? "output/File_" + x + ".ext" : string.Empty
                });
            }

            // clear the log, its too big to send with this data
            var results = DbHelper.Select<LibraryFile>()
                                   .Where(x => status == null || x.Status == status.Value);

            if (status == FileStatus.Unprocessed)
                return results.OrderBy(x => x.Order > 0 ? x.Order : int.MaxValue)
                              .ThenBy(x => x.DateCreated);

            if (status == FileStatus.Processing)
                return results;

            return results.OrderByDescending(x => x.ProcessingEnded);
        }

        [HttpGet("upcoming")]
        public IEnumerable<LibraryFile> Upcoming([FromQuery] FileStatus? status)
        {
            return GetAll(FileStatus.Unprocessed).Take(10);
        }

        [HttpGet("recently-finished")]
        public IEnumerable<LibraryFile> RecentlyFinished([FromQuery] FileStatus? status)
        {
            if (Globals.Demo)
                return GetAll(FileStatus.Processed);

            return DbHelper.Select<LibraryFile>()
                           .Where(x => x.Status == FileStatus.Processed)
                           .OrderByDescending(x => x.ProcessingEnded)
                           .Take(10);
        }

        [HttpGet("status")]
        public IEnumerable<LibraryStatus> GetStatus()
        {
            if (Globals.Demo)
            {
                return new[]
                {
                    new LibraryStatus { Status = FileStatus.Unprocessed, Count = 10 },
                    new LibraryStatus { Status = FileStatus.Processing, Count = 1 },
                    new LibraryStatus { Status = FileStatus.Processed, Count = 10 },
                    new LibraryStatus { Status = FileStatus.ProcessingFailed, Count = 10 }
                };
            }
            return DbHelper.Select<LibraryFile>()
                           .GroupBy(x => x.Status)
                           .Select(x => new LibraryStatus { Status = x.Key, Count = x.Count() });

        }

        [HttpGet("{uid}")]
        public LibraryFile Get(Guid uid)
        {
            if (Globals.Demo)
            {
                return new LibraryFile
                {
                    Uid = uid,
                    Status = FileStatus.Processed,
                    Name = "A Demo File.ext"
                };
            }
            return DbHelper.Single<LibraryFile>(uid);
        }

        [HttpGet("{uid}/log")]
        public string GetLog(Guid uid)
        {
            if (Globals.Demo)
            {
                return @"2021-11-27 11:46:15.0658 - Debug -> Executing part:
2021-11-27 11:46:15.1414 - Debug -> node: VideoFile
2021-11-27 11:46:15.8442 - Info -> Video Information:
ffmpeg version 4.1.8 Copyright (c) 2000-2021 the FFmpeg developers
  built with gcc 9 (Ubuntu 9.3.0-17ubuntu1~20.04)
  configuration: --disable-debug --disable-doc --disable-ffplay --enable-shared --enable-avresample --enable-libopencore-amrnb --enable-libopencore-amrwb --enable-gpl --enable-libass --enable-fontconfig --enable-libfreetype --enable-libvidstab --enable-libmp3lame --enable-libopus --enable-libtheora --enable-libvorbis --enable-libvpx --enable-libwebp --enable-libxcb --enable-libx265 --enable-libxvid --enable-libx264 --enable-nonfree --enable-openssl --enable-libfdk_aac --enable-postproc --enable-small --enable-version3 --enable-libbluray --enable-libzmq --extra-libs=-ldl --prefix=/opt/ffmpeg --enable-libopenjpeg --enable-libkvazaar --enable-libaom --extra-libs=-lpthread --enable-libsrt --enable-nvenc --enable-cuda --enable-cuvid --enable-libnpp --extra-cflags='-I/opt/ffmpeg/include -I/opt/ffmpeg/include/ffnvcodec -I/usr/local/cuda/include/' --extra-ldflags='-L/opt/ffmpeg/lib -L/usr/local/cuda/lib64 -L/usr/local/cuda/lib32/'
  libavutil      56. 22.100 / 56. 22.100
  libavcodec     58. 35.100 / 58. 35.100
  libavformat    58. 20.100 / 58. 20.100
  libavdevice    58.  5.100 / 58.  5.100
  libavfilter     7. 40.101 /  7. 40.101
  libavresample   4.  0.  0 /  4.  0.  0
  libswscale      5.  3.100 /  5.  3.100
  libswresample   3.  3.100 /  3.  3.100
  libpostproc    55.  3.100 / 55.  3.100";
            }
            var logFile = DbHelper.Single<Settings>()?.GetLogFile(uid);
            if (string.IsNullOrEmpty(logFile))
                return string.Empty;
            if (System.IO.File.Exists(logFile) == false)
                return string.Empty;


            try
            {
                Stream stream = System.IO.File.Open(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader streamReader = new StreamReader(stream);
                return streamReader.ReadToEnd();
            }
            catch (System.Exception ex)
            {
                return "Error opening log: " + ex.Message;
            }
        }

        [HttpPost("move-to-top")]
        public void MoveToTop([FromBody] ReferenceModel model)
        {
            if (Globals.Demo)
                return;

            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete

            var list = model.Uids.ToList();

            var libFiles = DbHelper.Select<LibraryFile>()
                                   .Where(x => x.Status == FileStatus.Unprocessed)
                                   .OrderBy(x =>
                                    {
                                        int index = list.IndexOf(x.Uid);
                                        if (index >= 0)
                                        {
                                            x.Order = index + 1;
                                            return index;
                                        }
                                        else if (x.Order > 0)
                                        {
                                            x.Order = list.Count + x.Order - 1;
                                            return x.Order;
                                        }
                                        return int.MaxValue;
                                    })
                                    .Where(x => x.Order > 0)
                                    .ToList();
            int order = 0;
            foreach (var libFile in libFiles)
            {
                libFile.Order = ++order;
                DbHelper.Update(libFile);
            }
        }

        [HttpDelete]
        public void Delete([FromBody] ReferenceModel model)
        {
            if (Globals.Demo)
                return;
            if (model == null || model.Uids?.Any() != true)
                return; // nothing to delete
            DbHelper.Delete<LibraryFile>(model.Uids);
        }
    }
}