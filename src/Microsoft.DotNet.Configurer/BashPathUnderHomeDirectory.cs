using System;

namespace Microsoft.DotNet.Configurer
{
    public class BashPathUnderHomeDirectory
    {
        private readonly string _fullHomeDirectoryPath;
        private readonly string _pathRelativeToHome;

        public BashPathUnderHomeDirectory(string fullHomeDirectoryPath, string pathRelativeToHome)
        {
            _fullHomeDirectoryPath =
                fullHomeDirectoryPath ?? throw new ArgumentNullException(nameof(fullHomeDirectoryPath));
            _pathRelativeToHome = pathRelativeToHome ?? throw new ArgumentNullException(nameof(pathRelativeToHome));
        }

        public string PathWithTilde => $"~/{_pathRelativeToHome}";

        public string PathWithDollar => $"$HOME/{_pathRelativeToHome}";

        public string Path => System.IO.Path.Combine(_fullHomeDirectoryPath, _pathRelativeToHome);
    }
}
