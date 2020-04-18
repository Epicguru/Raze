using System;
using System.IO;

namespace RazeContent.Loaders
{
    /// <summary>
    /// A content loader loads a particular type of content.
    /// </summary>
    public abstract class ContentLoader
    {
        public Type ContentType { get; }
        public abstract string ExpectedFileExtension { get; }
        public RazeContentManager ContentManager { get; protected internal set; }

        protected ContentLoader(Type type)
        {
            ContentType = type;
        }

        public virtual bool VerifyFile(string path, out string error)
        {
            string ext = null;
            bool worked = CheckFile(path, ext, out int er);
            error = er switch
            {
                0 => $"File {path} does not exist",
                1 => $"Wrong file extension: Expected {ext} but got {new FileInfo(path).Extension}.",
                _ => null
            };

            return worked;
        }

        public abstract object Load(string path);


        /// <summary>
        /// Checks to see if a file exists, and (optionally) that the file has a particular extension (such as .png).
        /// Default implementation only checks these two things, although particular content loaders can override this to
        /// add more checks.
        /// This is a utility method, and does not need to be overriden.
        /// </summary>
        /// <param name="path">The path of the file to check.</param>
        /// <param name="extension">The extension of the file to check, such as .png or .xml, or null to allow any file extension.</param>
        /// <param name="errorCode">The error code. -1: no error, 0: file does not exist, 1: wrong extension</param>
        /// <returns></returns>
        public static bool CheckFile(string path, string extension, out int errorCode)
        {
            errorCode = -1;

            string ext = extension?.Trim().ToLower();
            if (ext != null && ext[0] != '.')
                ext = '.' + ext;

            var info = new FileInfo(path);
            if (!info.Exists)
            {
                errorCode = 0;
                return false;
            }

            if(ext != null && info.Extension != ext)
            {
                errorCode = 1;
                return false;
            }

            return true;
        }
    }
}
