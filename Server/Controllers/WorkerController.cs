namespace FileFlows.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using FileFlows.Shared.Models;
    using FileFlows.Server.Workers;

    [Route("/api/worker")]
    public class WorkerController : Controller
    {
        [HttpGet]
        public IEnumerable<FlowWorkerStatus> GetAll()
        {
            if (Globals.Demo)
                return new[] { DemoWorker() };
            return FlowWorker.RegisteredFlowWorkers.Select(x => x.Status);
        }

        private FlowWorkerStatus DemoWorker()
        {
            return new FlowWorkerStatus
            {
                CurrentFile = "DemoFile.mkv",
                CurrentPart = 1,
                CurrentPartName = "Curren Flow Part",
                CurrentPartPercent = 50,
                CurrentUid = Guid.NewGuid(),
                Library = new ObjectReference
                {
                    Name = "Demo Library",
                    Uid = Guid.NewGuid()
                },
                RelativeFile = "DemoFile.mkv",
                StartedAt = DateTime.Now.AddMinutes(-1),
                TotalParts = 5,
                WorkingFile = "tempfile.mkv",
                Uid = Guid.NewGuid()
            };
        }

        //[HttpGet("{uid}")]
        //public Worker Get(Guid uid)
        //{
        //    if(Globals.Demo)
        //        return new Worker {    }
        //    FlowWorker.RegisteredFlowWorkers.FirstOrDefault(x => x.Status.Uid == uid);
        //}

        [HttpGet("{uid}/log")]
        public string Log(Guid uid)
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
            var worker = FlowWorker.RegisteredFlowWorkers.FirstOrDefault(x => x.Status.Uid == uid);
            if (worker == null || worker.CurrentFlowLogger == null)
                return string.Empty;
            return worker.CurrentFlowLogger.GetPreview();
        }

        [HttpDelete("{uid}")]
        public async Task Abort(Guid uid)
        {
            if (Globals.Demo)
                return;

            var worker = FlowWorker.RegisteredFlowWorkers.FirstOrDefault(x => x.Status.Uid == uid);
            if (worker == null)
                return;

            await worker.Abort();
        }
    }
}