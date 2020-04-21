using System;
using System.Collections.Generic;
using System.IO;

namespace Raze.Defs
{
    /// <summary>
    /// This class handles storing and sorting loaded definitions. The actual loading
    /// is handled by an instance of the <see cref="DefinitionLoader"/> class.
    /// </summary>
    public class DefDatabase : IDisposable
    {
        public DefinitionLoader Loader { get; private set; }
        public event Action<string, Exception> OnLoadError
        {
            add
            {
                Loader.OnError += value;
            }
            remove
            {
                Loader.OnError -= value;
            }
        }

        private List<Def> allDefs;
        private Dictionary<string, Def> namedDefs;
        private Dictionary<Def, List<Def>> childrenMap;
        private readonly Def[] emptyDefArray = new Def[0];

        public DefDatabase()
        {
            Loader = new DefinitionLoader();
            Loader.OnError += (msg, e) =>
            {
                Debug.Error("Error loading defs: " + msg, e);
            };

            allDefs = new List<Def>();
            namedDefs = new Dictionary<string, Def>();
            childrenMap = new Dictionary<Def, List<Def>>();
        }

        /// <summary>
        /// Adds a set of json texts ready to be loaded. Using this is generally not recommended because it is hard to tell where the
        /// json has come from, unlike when loading from file.
        /// </summary>
        /// <param name="jsonFiles">The json definitions. Each definition text can contain multiple stubs in an array.</param>
        public void AddRaw(IReadOnlyList<string> jsonFiles)
        {
            if (jsonFiles == null || jsonFiles.Count == 0)
            {
                Debug.Error("Null or empty array passed into AddRaw");
                return;
            }

            foreach (var txt in jsonFiles)
            {
                if (!string.IsNullOrWhiteSpace(txt))
                {
                    Loader.Add(new DefinitionFile("from LoadRaw", txt));
                }
                else
                {
                    Debug.Error("There is a blank or null entry passed into the array into LoadRaw.");
                }
            }
        }

        /// <summary>
        /// Adds a json text ready to be loaded. Using this is generally not recommended because it is hard to tell where the
        /// json has come from, unlike when loading from file.
        /// </summary>
        /// <param name="json">The json definition. This definition text can contain multiple stubs in an array.</param>
        public void AddRaw(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.Error("Null or blank string passed into AddRaw");
                return;
            }

            Loader.Add(new DefinitionFile("from LoadRaw", json));
        }

        /// <summary>
        /// Adds all .json files from a specified directory.
        /// </summary>
        /// <param name="dir">The directory to find the .json files.</param>
        /// <param name="deep">If true (default) includes all sub-directories in search. If false, only includes top directory.</param>
        public void AddAllFromDirectory(string dir, bool deep = true)
        {
            if (!Directory.Exists(dir))
            {
                Debug.Error($"Directory {dir} does not exist.");
                return;
            }

            string[] files = Directory.GetFiles(dir, "*.json", deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            AddFromFiles(files);
        }

        /// <summary>
        /// Adds a json text, read from a file, ready to be loaded.
        /// </summary>
        /// <param name="filePath">The absolute path of the json definition file.</param>
        public void AddFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.Error("Null or blank file path passed into AddFromFile.");
                return;
            }

            AddFromFiles(new string[] { filePath } );
        }

        /// <summary>
        /// Adds a set json texts, read from files, ready to be loaded.
        /// </summary>
        /// <param name="filePaths">The list or array of file paths to load from. May not be null or empty.</param>
        public void AddFromFiles(IReadOnlyList<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                Debug.Error("Null or empty array passed into AddFromFiles.");
                return;
            }

            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    string txt;
                    try
                    {
                        txt = File.ReadAllText(filePath);
                    }
                    catch (Exception e)
                    {
                        Debug.Error($"Failed to load json from file - Exception while reading '{filePath}'", e);
                        continue;
                    }

                    Loader.Add(new DefinitionFile(filePath, txt));
                }
                else
                {
                    Debug.Warn($"Failed to load json from file - file does not exist: '{filePath}'");
                }
            }
        }

        /// <summary>
        /// Gets a definition given it's name. Will return null with no error logged if not found. Case-sensitive and whitespace included.
        /// </summary>
        /// <param name="name">The name of the definition, as specified in file.</param>
        /// <returns>The definition or null.</returns>
        public Def Get(string name)
        {
            return namedDefs.TryGetValue(name, out Def d) ? d : null;
        }

        /// <summary>
        /// Gets a list of the children of any given definition. Will not return null even if the definition has no children.
        /// Only returns children that were loaded using this database.
        /// </summary>
        /// <param name="def">The definition to get the children of.</param>
        /// <returns>A list of children, or an empty list if it has no children.</returns>
        public IReadOnlyList<Def> GetChildren(Def def)
        {
            if (childrenMap.ContainsKey(def))
                return childrenMap[def];
            
            return emptyDefArray;
        }

        /// <summary>
        /// Loads and processes all definitions added using the <c>AddRaw</c> and <c>AddFromFile(s)</c>
        /// methods. This action 'consumes' the added json texts. So calling this method twice in a row has no effect.
        /// </summary>
        public void Load()
        {
            var loaded = Loader.ProcessAll();
            if (loaded == null)
                return;

            foreach (var def in loaded)
            {
                Add(def);
            }

            RebuildChildrenMap();
        }

        private void Add(Def def)
        {
            if (namedDefs.ContainsKey(def.Name))
            {
                Debug.Warn($"Duplicate def loaded: {def.Name}.");
                return;
            }

            allDefs.Add(def);
            namedDefs.Add(def.Name, def);
        }

        private void RebuildChildrenMap()
        {
            childrenMap.Clear();
            foreach (var def in allDefs)
            {
                if (string.IsNullOrWhiteSpace(def.Parent))
                    continue;

                Def parent = Get(def.Parent);
                if (parent != null)
                {
                    AddChild(parent, def);
                }
            }

            void AddChild(Def parent, Def child)
            {
                if (childrenMap.ContainsKey(parent))
                {
                    childrenMap[parent].Add(child);
                }
                else
                {
                    var list = new List<Def>();
                    list.Add(child);
                    childrenMap.Add(parent, list);
                }
            }
        }

        public void Dispose()
        {
            namedDefs?.Clear();
            namedDefs = null;
            Loader?.Dispose();
            Loader = null;
        }
    }
}
