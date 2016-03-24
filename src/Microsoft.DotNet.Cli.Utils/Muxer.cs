using System;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;
using System.Diagnostics;

namespace Microsoft.DotNet.Cli.Utils
{
    public class Muxer
    {
        public static readonly string MuxerName = "dotnet";
        private static readonly string s_muxerFileName = MuxerName + Constants.ExeSuffix;

        private string _muxerPath;

        public string MuxerPath
        {
            get
            {
                if (_muxerPath == null)
                {
                    throw new InvalidOperationException("Unable to locate dotnet multiplexer");
                }
                return _muxerPath;
            }
        }

        public Muxer()
        {
            if (!TryResolveMuxerFromCurrentProcess())
            {
                if (!TryResolveMuxerFromParentDirectories())
                {
                    TryResolverMuxerFromPath();
                }
            }
        }

        private bool TryResolveMuxerFromCurrentProcess()
        {
            var mainFileName = Process.GetCurrentProcess().MainModule.FileName;
            var fileName = Path.GetFileNameWithoutExtension(mainFileName);
            if (fileName == MuxerName)
            {
                _muxerPath = mainFileName;
                return true;
            }
            return false;
        }

        private bool TryResolveMuxerFromParentDirectories()
        {
            var appBase = new DirectoryInfo(PlatformServices.Default.Application.ApplicationBasePath);
            var muxerDir = appBase.Parent?.Parent;

            if (muxerDir == null)
            {
                return false;
            }

            var muxerCandidate = Path.Combine(muxerDir.FullName, s_muxerFileName);

            if (!File.Exists(muxerCandidate))
            {
                return false;
            }

            _muxerPath = muxerCandidate;
            return true;
        }

        private bool TryResolverMuxerFromPath()
        {
            var muxerPath = Env.GetCommandPath(MuxerName, Constants.ExeSuffix);

            if (muxerPath == null || !File.Exists(muxerPath))
            {
                return false;
            }

            _muxerPath = muxerPath;

            return true;
        }
    }
}
