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
        private List<DefStub> allAbsDefs;
        private Def[] idToDef = new Def[ushort.MaxValue + 1];
        private Dictionary<string, Def> namedDefs;
        private Dictionary<string, DefStub> namedAbs;
        private Dictionary<DefStub, (List<Def> real, List<DefStub> abs)> childrenMap;
        private readonly Def[] emptyDefArray = new Def[0];
        private Queue<DefStub> openNodes = new Queue<DefStub>();
        private List<Def> tempDefs = new List<Def>();
        private ushort currentID;

        public DefDatabase()
        {
            Loader = new DefinitionLoader();
            Loader.OnError += (msg, e) =>
            {
                Debug.Error("Error loading defs: " + msg, e);
            };

            allDefs = new List<Def>();
            allAbsDefs = new List<DefStub>();
            namedDefs = new Dictionary<string, Def>();
            namedAbs = new Dictionary<string, DefStub>();
            childrenMap = new Dictionary<DefStub, (List<Def> real, List<DefStub> abs)>();
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
        /// Note that this will NOT return definitions marked as abstract.
        /// </summary>
        /// <param name="name">The name of the definition, as specified in file.</param>
        /// <returns>The definition or null.</returns>
        public Def Get(string name)
        {
            return namedDefs.TryGetValue(name, out Def d) ? d : null;
        }

        /// <summary>
        /// Gets a definition given it's id. Will return null with no error logged if not found.
        /// Note that this will NOT return definitions marked as abstract. See <see cref="GetAbstract(string)"/> for that.
        /// </summary>
        /// <param name="id">The id of the definition, as assigned at runtime.</param>
        /// <returns>The definition or null.</returns>
        public Def Get(ushort id)
        {
            return idToDef[id];
        }

        /// <summary>
        /// Gets an abstract definition given it's id. Will return null with no error logged if not found.
        /// This should only be used for special cases - normally abstract definitions should not used in-game, and they only serve
        /// as a logical link rather than a real definition.
        /// Abstract definitions can only be accessed by name because they are not assigned an ID.
        /// Note that this will NOT return non-abstract definitions. See <see cref="Get(string)"/> for that.
        /// </summary>
        /// <param name="name">The name of the definition, as specified in the json file.</param>
        /// <returns>The abstract definition or null.</returns>
        public DefStub GetAbstract(string name)
        {
            return namedAbs.TryGetValue(name, out DefStub d) ? d : null;
        }

        public IReadOnlyList<Def> GetChildren(string defName, bool recursive)
        {
            var realDef = Get(defName);
            if (realDef != null)
                return GetChildren(realDef, recursive);

            var absDef = GetAbstract(defName);
            if (absDef != null)
                return GetChildren(absDef, recursive);

            Debug.Error($"Failed to find real or abstract def for name {defName}");
            return null;
        }

        /// <summary>
        /// Gets a list of the children of any given definition.
        /// Only returns children that were loaded using this database.
        /// </summary>
        /// <param name="def">The definition to get the children of.</param>
        /// <param name="recursive">If true, all children, and all children's children etc. will be returned. If false, only immediate children are returned.</param>
        /// <returns>A list of children, or an empty list if it has no children.</returns>
        public IReadOnlyList<Def> GetChildren(DefStub def, bool recursive)
        {
            if (def == null)
            {
                Debug.Error("Null def passed into GetChilren");
                return null;
            }

            if (!recursive)
            {
                if (childrenMap.ContainsKey(def))
                    return childrenMap[def].real;
                return emptyDefArray;
            }
            else
            {
                openNodes.Clear();
                tempDefs.Clear();

                openNodes.Enqueue(def);
                while(openNodes.Count > 0)
                {
                    var current = openNodes.Dequeue();

                    if (current != def && !current._IsAbstract)
                        tempDefs.Add(current as Def);

                    if(childrenMap.TryGetValue(current, out var pair))
                    {
                        foreach (var child in pair.real)
                        {
                            openNodes.Enqueue(child);
                        }
                        foreach (var child in pair.abs)
                        {
                            openNodes.Enqueue(child);
                        }
                    }
                    
                }

                return tempDefs;
            }
        }

        /// <summary>
        /// Loads and processes all definitions added using the <c>AddRaw</c> and <c>AddFromFile(s)</c>
        /// methods. This action 'consumes' the added json texts. So calling this method twice in a row has no effect.
        /// </summary>
        public void Load()
        {
            var loaded = Loader.ProcessAll();

            foreach (var def in loaded.defs)
            {
                Console.WriteLine(" DEF > " + def);
                Add(def);
            }

            foreach (var def in loaded.abs)
            {
                Console.WriteLine(" ABS > " + def);
                AddAbs(def);
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

            def.DefID = ++currentID;
            idToDef[def.DefID] = def;
            allDefs.Add(def);
            namedDefs.Add(def.Name, def);
        }

        private void AddAbs(DefStub def)
        {
            if (namedAbs.ContainsKey(def.Name))
            {
                Debug.Warn($"Duplicate abstract def loaded: {def.Name}.");
                return;
            }

            allAbsDefs.Add(def);
            namedAbs.Add(def.Name, def);
        }

        private void RebuildChildrenMap()
        {
            childrenMap.Clear();
            foreach (var def in allDefs)
            {
                if (string.IsNullOrWhiteSpace(def.Parent))
                    continue;

                DefStub parent = Get(def.Parent) ?? GetAbstract(def.Parent);

                if (parent != null)
                {
                    AddRealChild(parent, def);
                }
            }
            foreach (var def in allAbsDefs)
            {
                if (string.IsNullOrWhiteSpace(def.Parent))
                    continue;

                DefStub parent = Get(def.Parent) ?? GetAbstract(def.Parent);

                if (parent != null)
                {
                    AddAbsChild(parent, def);
                }
            }

            void AddRealChild(DefStub parent, Def child)
            {
                if (childrenMap.ContainsKey(parent))
                {
                    childrenMap[parent].real.Add(child);
                }
                else
                {
                    var real = new List<Def>();
                    var abs = new List<DefStub>();
                    real.Add(child);
                    childrenMap.Add(parent, (real, abs));
                }
            }
            void AddAbsChild(DefStub parent, DefStub child)
            {
                if (childrenMap.ContainsKey(parent))
                {
                    childrenMap[parent].abs.Add(child);
                }
                else
                {
                    var real = new List<Def>();
                    var abs = new List<DefStub>();
                    abs.Add(child);

                    childrenMap.Add(parent, (real, abs));
                }
            }
        }

        public void Dispose()
        {
            idToDef = null;
            namedDefs?.Clear();
            namedDefs = null;
            tempDefs?.Clear();
            tempDefs = null;
            openNodes?.Clear();
            openNodes = null;
            childrenMap?.Clear();
            childrenMap = null;
            namedAbs?.Clear();
            namedAbs = null;
            allAbsDefs?.Clear();
            allAbsDefs = null;
            allDefs?.Clear();
            allDefs = null;
            Loader?.Dispose();
            Loader = null;
        }
    }
}
