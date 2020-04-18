using System;
using System.IO;

namespace Raze.IO
{
    public static class GameIO
    {
        public static readonly string GameDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "GVS");
        public static readonly string LogDirectory = Path.Combine(GameDirectory, "Logs");

        /// <summary>
        /// Creates the directory at the supplied path, if it does not already exist.
        /// </summary>
        /// <param name="directoryPath">The full path to the directory that is to be created.</param>
        /// <returns>Returns true if the directory was created, false if the directory already existed.</returns>
        public static bool EnsureDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
                return false;

            Directory.CreateDirectory(directoryPath);
            return true;
        }

        /// <summary>
        /// Ensures that the directory that contains the file is created if it does not already exist.
        /// </summary>
        /// <param name="filePath">The absolute path to the file who's parent directory is to be created.</param>
        /// <returns>False if the parent directory already exists, true if it was created.</returns>
        public static bool EnsureParentDirectory(string filePath)
        {
            if (File.Exists(filePath))
                return false;

            var f = new FileInfo(filePath);
            return f.Directory != null && EnsureDirectory(f.Directory.FullName);
        }
    }
}
