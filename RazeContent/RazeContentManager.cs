using Microsoft.Xna.Framework.Graphics;
using RazeContent.Loaders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RazeContent
{
    /// <summary>
    /// This content manager serves as a replacement to MonoGame's content manager, and removes
    /// the need for the pipeline. It loads content directly from the file system, generally from uncompressed
    /// files (although some forms of compression are supported).
    ///
    /// The content manager can automatically cache content so that there will not be duplicate content
    /// in memory.
    /// </summary>
    public class RazeContentManager : IDisposable
    {
        public GraphicsDevice GraphicsDevice { get; set; }
        public int CachedAssetCount
        {
            get
            {
                return cache?.Count ?? 0;
            }
        }
        public bool AllowAbsolutePaths { get; set; } = false;

        private Dictionary<string, object> cache = new Dictionary<string, object>();
        private Dictionary<Type, ContentLoader> loaders = new Dictionary<Type, ContentLoader>();
        private HashSet<FileSystemItem> contentFiles = new HashSet<FileSystemItem>();
        private string[] sourceFolders = new string[64];
        private int sourceFolderCount = 0;

        public RazeContentManager(GraphicsDevice gd, params string[] contentSourceFolders)
        {
            // Don't check graphics device for null, allow the loaders that require it to throw exception
            // if it's missing. This way if you font have a GD you can still load audio, for example.
            this.GraphicsDevice = gd;

            foreach (var path in contentSourceFolders)
            {
                AddSourceFolder(path);
            }

            // Add default loaders. These can be overwritten by calling AddLoader.
            AddLoader(new TextureLoader());
            AddLoader(new GameFontLoader());
        }

        public void AddLoader(ContentLoader cl)
        {
            if (cl == null)
                throw new ArgumentNullException(nameof(cl));

            var type = cl.ContentType;
            if (type == null)
                throw new ArgumentException($"The content loader '{cl.GetType().FullName}' has no type associated with it!", nameof(cl));

            if (loaders.ContainsKey(type))
                loaders[type] = cl;
            else
                loaders.Add(type, cl);

            cl.ContentManager = this;
        }

        /// <summary>
        /// Adds a folder to the list of source folders.
        /// Throws exceptions if the path is null or if the path has already been added.
        /// </summary>
        /// <param name="path">The path to the root folder that contains content.</param>
        public void AddSourceFolder(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Could not add source folder, directory was not found: {path}");
            }

            string fullPath = dir.FullName.Trim();
            if (ContainsSourceFolder(fullPath))
                throw new InvalidOperationException($"This RazeContentManager already contains the source folder '{fullPath}'");

            sourceFolders[sourceFolderCount] = fullPath;
            sourceFolderCount++;

            int count = 0;
            foreach (var file in Directory.EnumerateFiles(path, "", SearchOption.AllDirectories))
            {
                var info = new FileInfo(file);
                var item = new FileSystemItem();

                item.AbsolutePath = info.FullName;
                item.LocalPath = info.FullName[fullPath.Length..];

                //Console.WriteLine($"{item.LocalPath}");

                contentFiles.Add(item);

                count++;
            }

            Console.WriteLine($"Added {count} new files to content system, for a total of {contentFiles.Count}.");
        }

        /// <summary>
        /// Attempts to get the absolute path of a content file.
        /// The input can be either:
        /// <list type="bullet">
        /// <item>An absolute path, which will just return the input value.</item>
        /// <item>A local path, relative to a registered source folder (see <see cref="AddSourceFolder"/>).</item>
        /// </list>
        ///
        /// The path, absolute or relative, MUST INCLUDE FILE EXTENSION!
        /// </summary>
        /// <param name="path">The input path, absolute or relative.</param>
        /// <returns>The absolute path, or null if it could not be resolved.</returns>
        public virtual string GetAbsolutePath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (AllowAbsolutePaths)
            {
                FileInfo f = new FileInfo(path);
                if (f.Exists)
                    return f.FullName;
            }

            // Normalize back and forward slashes, necessary for string comparison further down.
            path = path.Replace('/', '\\');

            var tempToCheck = new FileSystemItem();
            for (int i = 0; i < sourceFolderCount; i++)
            {
                string root = sourceFolders[i];
                string combined = Path.Combine(root, path);

                tempToCheck.AbsolutePath = combined;
                if (contentFiles.Contains(tempToCheck))
                {
                    return combined;
                }
            }

            return null;
        }

        /// <summary>
        /// Clears all current items out of the cache.
        /// If dispose is true, the items will be disposed of too. This should only
        /// be done if you are sure that those items will no longer be used.
        /// </summary>
        /// <param name="dispose">If true, cached items will also be disposed before being discarded. Otherwise, the reference to them will simply be removed.</param>
        public void ClearCache(bool dispose = false)
        {
            if (dispose)
            {
                foreach (var obj in cache.Values)
                {
                    if(obj is IDisposable dsp)
                    {
                        dsp.Dispose();
                    }
                }
            }

            cache.Clear();
        }

        public void AddToCache(string absolutePath, object item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrWhiteSpace(absolutePath))
                throw new ArgumentException("Path cannot be null or whitespace", nameof(absolutePath));

            if (!cache.ContainsKey(absolutePath))
                cache.Add(absolutePath, item);
            else
                cache[absolutePath] = item;
        }

        protected virtual T GetFromCache<T>(string absolutePath) where T : class
        {
            return cache[absolutePath] as T;
        }

        /// <summary>
        /// Synchronously loads an asset given an absolute or relative path.
        /// Optionally (and by default) reads and writes to asset cache, to void loading assets twice into memory.
        /// Relative paths start from the source folder (see <see cref="AddSourceFolder(string)"/>) and must include file
        /// extension.
        /// </summary>
        /// <typeparam name="T">The type of asset to load. This determines which loader is used.</typeparam>
        /// <param name="path">The absolute or relative path to load from. Must include file extension, such as .png or .xml.</param>
        /// <param name="fromCache">If <see langword="true"/>, then the asset is taken from the cache if it has already been loaded before. If <see langword="false"/>, the asset will be re-loaded from disk.</param>
        /// <param name="toCache">If <see langword="true"/>, then the asset is put into the cache if it is not already there. If <see langword="false"/>, the asset will not be added into the cache (but it might already be there).</param>
        /// <returns></returns>
        public virtual T Load<T>(string path, bool fromCache = true, bool toCache = true) where T : class
        {
            // Try to read from cache, if enabled.
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or whitespace");

            var loader = GetLoader<T>();
            if (loader == null)
                throw new Exception($"Content loader for type {typeof(T).FullName} not found!");

            FileInfo f = new FileInfo(path);
            if (string.IsNullOrWhiteSpace(f.Extension))
                path += loader.ExpectedFileExtension;

            string absolutePath = GetAbsolutePath(path);
            if (absolutePath == null)
                throw new FileNotFoundException($"Failed to find the corresponding absolute path for '{path}'." +
                                                $" Check spelling and punctuation, and make sure that you included the file extension.");

            // Now get from cache, if enabled.
            if (fromCache)
            {
                if (cache.ContainsKey(absolutePath))
                {
                    T objFromCache = GetFromCache<T>(absolutePath);
                    if(objFromCache == null)
                    {
                        throw new ArgumentException($"The loaded type (from cache), {cache[absolutePath].GetType().FullName} cannot be converted to requested type {typeof(T).FullName}!", nameof(T));
                    }

                    return objFromCache;
                }
            }

            bool canWork = loader.VerifyFile(absolutePath, out string error);
            if (!canWork)
            {
                throw new Exception($"Failed to load content <{typeof(T).Name}> from {absolutePath}:\n{error}");
            }

            Stopwatch s = new Stopwatch();
            s.Start();
            object obj = loader.Load(absolutePath);
            s.Stop();

            if (obj == null)
                return null; // Should an exception be thrown here?

            // Now write to cache, if enabled.
            if (toCache)
            {
                bool alreadyInCache = cache.ContainsKey(absolutePath);
                if (!alreadyInCache)
                    cache.Add(absolutePath, obj);
            }

            T converted = obj as T;
            if(converted == null)
            {
                // Loaded type did not match the requested type!
                throw new ArgumentException($"The loaded type, {obj.GetType().FullName} cannot be converted to requested type {typeof(T).FullName}!", nameof(T));
            }

            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"  -Loaded '{path}' in {s.Elapsed.TotalMilliseconds:F1} ms.");
            Console.ForegroundColor = old;

            return converted;
        }

        public ContentLoader GetLoader<T>() where T : class
        {
            return GetLoader(typeof(T));
        }

        public virtual ContentLoader GetLoader(Type t)
        {
            if (t == null)
                return null;

            if (loaders.ContainsKey(t))
                return loaders[t];

            return null;
        }

        private bool ContainsSourceFolder(string fullPath)
        {
            for (int i = 0; i < sourceFolderCount; i++)
            {
                string path = sourceFolders[i];
                if (path == fullPath)
                    return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (cache != null)
            {
                ClearCache(true);
                cache = null;
            }

            sourceFolders = null;
            contentFiles?.Clear();
            contentFiles = null;
            GraphicsDevice = null;

            if(loaders != null)
            {
                // Remove reference to this content manager. Don't want it sticking around in memory.
                foreach (var loader in loaders.Values)
                {
                    loader.ContentManager = null;
                }
                loaders.Clear();
                loaders = null;
            }
            
        }

        public struct FileSystemItem
        {
            public string AbsolutePath;
            public string LocalPath;

            public override bool Equals(object obj)
            {
                if(obj is FileSystemItem other)
                {
                    return other.AbsolutePath == this.AbsolutePath;
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return AbsolutePath.GetHashCode();
            }
        }
    }
}
