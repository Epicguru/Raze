using Newtonsoft.Json;
using Raze.Defs.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Raze.Defs
{
    public class DefinitionLoader : IDisposable
    {
        private static List<Assembly> currentAssemblies = new List<Assembly>();

        /// <summary>
        /// Refreshes the list of assemblies to load from. Uses all assemblies in the current AppDomain.
        /// </summary>
        public static void RefreshAssemblies()
        {
            currentAssemblies.Clear();
            currentAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies().Reverse());
        }

        public static Type FindType(string className)
        {
            foreach (var ass in currentAssemblies)
            {
                var type = ass.GetType(className, false, false);
                if (type != null)
                    return type;
            }

            throw new Exception($"Failed to find type '{className}' in any of the {currentAssemblies.Count} loaded assemblies. The class name is case-senstive, so check spelling!", null);
        }

        /// <summary>
        /// Invoked whenever there is an error with the deserialization of a definition.
        /// </summary>
        public event Action<string, Exception> OnError;
        /// <summary>
        /// If true, definitions that don't specify a <c>Parent</c> are assumed to have parent 'BaseDef'. If false, definitions that don't specify <c>Parent</c> will cause an error.
        /// Default is true.
        /// </summary>
        public bool AssumeBaseDef { get; set; } = true;

        private List<DefinitionFile> rawDefinitions = new List<DefinitionFile>();
        private List<(DefinitionFile file, DefStub stub)> stubs = new List<(DefinitionFile, DefStub)>();
        private List<(DefinitionFile file, DefStub stub)> tempStubs = new List<(DefinitionFile, DefStub)>();
        private Dictionary<string, DefStub> namedStubs = new Dictionary<string, DefStub>();
        private Dictionary<Type, ConstructorInfo> cachedConstructors = new Dictionary<Type, ConstructorInfo>();
        private List<DefStub> tempDefs = new List<DefStub>();
        private List<Def> returnDefs = new List<Def>();
        private List<DefStub> returnAbs = new List<DefStub>();

        private JsonSerializer json;
        private object[] noArgs = new object[0];
        private Type[] noParams = new Type[0];
        private HashSet<string> exploredItems = new HashSet<string>();

        public DefinitionLoader()
        {
            json = new JsonSerializer();
            json.Formatting = Formatting.Indented;
            json.DefaultValueHandling = DefaultValueHandling.Ignore;
            json.NullValueHandling = NullValueHandling.Ignore;
            json.ContractResolver = RazeContractResolver.Instance;

            json.Converters.Add(new ColorConverter());
            json.Converters.Add(new SpriteConverter());

            json.Converters.Add(new PointConverter());
            json.Converters.Add(new Point3DConverter());

            json.Converters.Add(new Vector2Converter());
            json.Converters.Add(new Vector3Converter());

            RefreshAssemblies();
        }

        /// <summary>
        /// Adds a new raw json definition to be processed.
        /// </summary>
        public void Add(DefinitionFile file)
        {
            if (string.IsNullOrWhiteSpace(file.Json))
            {
                Debug.Error("Null or whitespace json input in DefinitionLoader.Add.");
                return;
            }

            rawDefinitions.Add(file);
        }

        /// <summary>
        /// Creates definitions based on all loaded definition files that were added using <see cref="Add(DefinitionFile)"/>.
        /// Calling this 'consumes' the added definitions, so calling this method twice in a row will yield no definitions on the second call.
        /// Note that there are almost always less actual definitions than definition files due to
        /// definition inheritance. Any errors that occur when loading will go through <see cref="OnError"/>.
        /// </summary>
        /// <returns>The list of definitions.</returns>
        public (Def[] defs, DefStub[] abs) ProcessAll()
        {
            if (rawDefinitions.Count == 0)
                return (new Def[0], new DefStub[0]);
            
            ProcessRawToStub();
            ProcessStubs(out var defs, out var abs);

            return (defs, abs);
        }

        private void ProcessRawToStub()
        {
            stubs.Clear();
            foreach (var txt in rawDefinitions)
            {
                tempStubs.Clear();
                try
                {
                    using var tr = new StringReader(txt.Json);
                    using var jr = new JsonTextReader(tr);

                    GetJsonType(txt.Json, out JArray arr, out JObject obj);

                    if (arr != null)
                    {
                        var list = json.Deserialize<List<DefStub>>(jr);
                        for(int i = 0; i < list.Count; i++)
                        {
                            string itemJson = arr[i].ToString(Formatting.Indented);
                            tempStubs.Add((new DefinitionFile(txt.FilePath, itemJson), list[i]));
                        }
                    }
                    else if(obj != null)
                    {
                        tempStubs.Add((txt, json.Deserialize<DefStub>(jr)));
                    }
                    else
                    {
                        OnError?.Invoke($"Failed to parse json def file '{txt.FilePath}', expected array of Defs or single Def:\n{txt.Json}", null);
                    }
                }
                catch (Exception e)
                {
                    OnError?.Invoke($"Failed to deserialize basic def stub(s) for {txt.FilePath}:\n{txt.Json}", e);
                }

                if (tempStubs == null)
                    continue;

                foreach (var pair in tempStubs)
                {
                    var ds = pair.stub;

                    if (string.IsNullOrWhiteSpace(ds.Name))
                    {
                        OnError?.Invoke($"Definition {txt.FilePath} has no name!", null);
                    }
                    else if (!AssumeBaseDef && string.IsNullOrWhiteSpace(ds.Parent) && ds.Class == null)
                    {
                        // If it doesn't have a parent then it must explicitly state the def class.
                        // Otherwise, the type of this definition is unknown.
                        OnError?.Invoke($"Definition {ds.Name} ({txt.FilePath}) has no parent. Set DefinitionLoader.AsumeBaseDef to true to assume that ommiting parent is equivalent to inheriting from BaseDef.", null);
                    }
                    else if (ds.Parent == ds.Name)
                    {
                        // You can't be your own parent. That's not how biology works.
                        OnError?.Invoke($"Definition {ds.Name} ({txt.FilePath}) is it's own parent! Makes no sense.", null);
                    }
                    else
                    {
                        if (!namedStubs.ContainsKey(ds.Name))
                        {
                            if (string.IsNullOrWhiteSpace(ds.Parent) && ds.Name != "BaseDef")
                                ds.Parent = "BaseDef";

                            ds._Json = pair.file.Json;
                            namedStubs.Add(ds.Name, ds);
                            stubs.Add((pair.file, ds));
                        }
                        else
                        {
                            OnError?.Invoke($"Duplicate definition stub name: {ds.Name}. Duplicate: {txt.FilePath}", null);
                        }
                    }
                }
            }

            rawDefinitions.Clear();
        }

        private JsonLoadSettings parseSettings = new JsonLoadSettings() { CommentHandling = CommentHandling.Load, LineInfoHandling = LineInfoHandling.Ignore };
        private void GetJsonType(string txt, out JArray array, out JObject obj)
        {
            JToken t = JToken.Parse(txt, parseSettings);
            switch (t)
            {
                case JArray arr:
                    array = arr;
                    obj = null;
                    return;

                case JObject o:
                    obj = o;
                    array = null;
                    return;

                default:
                    Console.WriteLine("Unexpected type: " + t.Type);
                    array = null;
                    obj = null;
                    return;

            }
        }

        private void ProcessStubs(out Def[] defs, out DefStub[] abstracts)
        {
            returnDefs.Clear();
            returnAbs.Clear();
            foreach (var pair in stubs)
            {
                var stub = pair.stub;

                // Abstract stubs get no processing - they are not meant to be used at all from real code.
                // They just serve as a logical link and a design tool.
                if (stub._IsAbstract)
                {
                    returnAbs.Add(stub);
                    continue;
                }

                bool worked = FindAndSetParent(stub);
                if (!worked)
                    continue; // Error has already been reported.

                Type type = null;
                DefStub current = stub;
                while (current != null)
                {
                    if (current.Class != null)
                    {
                        type = current.Class;
                        break;
                    }

                    current = current._InternalParent;
                }

                if (type.IsAbstract)
                {
                    if(type == typeof(Def))
                    {
                        OnError?.Invoke($"{stub.Name}'s class is Def. This is a base abstract class and a non-abstract class should be specified using \"Class\" = \"MyClassName\"", null);
                        continue;
                    }
                    OnError?.Invoke($"{stub.Name}'s class {type.Name} is abstract: this is not valid. Either change the C# class or review your json structure.", null);
                    continue;
                }

                Def instance = CreateInstance(type, stub.Name);
                if (instance == null)
                    continue; // Error has already been reported.

                instance.FilePath = pair.file.FilePath;

                // Actually read all values from all json stubs associated with this definition.
                instance._Json = pair.file.Json;
                instance._InternalParent = stub._InternalParent;
                instance.Name = pair.stub.Name;
                worked = ConstructFromJson(instance);
                if (AssumeBaseDef)
                    instance.Parent = stub.Parent; // Necessary because of default-def stuff.
                instance._IsAbstract = false;
                instance._InternalParent = null;
                instance._Json = null;

                if (worked)
                {
                    bool hasAdditionalData = instance.AdditionalData != null && instance.AdditionalData.Count > 0;
                    bool allowAdditionalData = instance.AllowAdditionalData;

                    if (hasAdditionalData && !allowAdditionalData)
                    {
                        OnError?.Invoke($"Def {instance.Name} has {instance.AdditionalData.Count} items of additional data, but it does not have \"AllowAdditionalData\" = true." +
                                        $" If this is not a mistake, it is most likely cause by incorrect stub/class inheritance.\n"+
                                        $"Additional data is:\n{string.Join(",\n", Array.ConvertAll(instance.AdditionalData.Values.ToArray(), (jt) => jt.ToString()))}", null);
                    }
                    else
                    {
                        returnDefs.Add(instance);
                    }
                }
            }
            cachedConstructors.Clear();

            // Remove internal parent linking and raw json. Will not be used again.
            foreach (var pair in stubs)
            {
                pair.stub._InternalParent = null;
                pair.stub._Json = null;
            }
            stubs.Clear();
            namedStubs.Clear();

            defs = returnDefs.ToArray();
            returnDefs.Clear();
            abstracts = returnAbs.ToArray();
            returnAbs.Clear();
        }

        private bool ConstructFromJson(DefStub def)
        {
            GetTree(def, tempDefs);

            //Console.WriteLine($"Populating: {def.Name}");

            // Apply json from the root up.
            foreach (var ancestor in tempDefs)
            {
                using StringReader tr = new StringReader(ancestor._Json);
                using JsonTextReader r = new JsonTextReader(tr);

                try
                {
                    json.Populate(r, def);
                }
                catch(Exception e)
                {
                    OnError?.Invoke($"Error while populating data for [{def.GetType().Name}]{def.Name}: parent stub {ancestor.Name} failed to be deserialized onto the object. See exception.", e);
                    return false;
                }
                //Console.WriteLine($"  -Writing: {ancestor.Name ?? "[Def]"}");
            }

            tempDefs.Clear();
            return true;
        }

        /// <summary>
        /// Gets the inheritance tree for this def, starting from the root and leading to (and including) the actual def itself.
        /// </summary>
        /// <param name="def"></param>
        /// <param name="temp"></param>
        private void GetTree(DefStub def, List<DefStub> temp)
        {
            temp.Clear();
            DefStub current = def;
            while (current != null)
            {
                temp.Add(current);
                current = current._InternalParent;
            }
            temp.Reverse();
        }

        private Def CreateInstance(Type t, string name)
        {
            if (!typeof(Def).IsAssignableFrom(t))
            {
                OnError?.Invoke($"Def '{name}': Type {t.FullName} is not assignable to Def, so it is not a valid type for definition.", null);
                return null;
            }

            var constructor = cachedConstructors.ContainsKey(t) ? cachedConstructors[t] : null;
            if (constructor == null)
            {
                try
                {
                    constructor = t.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, noParams, null);
                    if (constructor == null)
                        throw new Exception("Constructor could not be found (returned null)");

                    cachedConstructors.Add(t, constructor);
                }
                catch (Exception e)
                {
                    OnError?.Invoke($"Failed to find zero-argument constructor for type {t.FullName}.", e);
                    return null;
                }
            }

            try
            {
                var instance = constructor.Invoke(noArgs);
                return instance as Def;
            }
            catch(Exception e)
            {
                OnError?.Invoke($"Exception in constructor for {t.FullName}.", e);
                return null;
            }
        }

        private bool FindAndSetParent(DefStub stub, string originalName = null, bool original = true, Type lastType = null)
        {
            string parentName = stub.Parent;
            if (string.IsNullOrWhiteSpace(parentName))
            {
                // Once there is no parent name, we have reached the end.
                return true;
            }

            if (original)
            {
                exploredItems.Clear();
                originalName = stub.Name;
            }
            if (exploredItems.Contains(stub.Name))
            {
                OnError?.Invoke($"Detected cyclic parenting for def stub: {originalName}.", null);
                return false;
            }
            exploredItems.Add(stub.Name);

            if(stub.Class != null)
            {
                if (lastType != null)
                {
                    if (!stub.Class.IsAssignableFrom(lastType))
                    {
                        OnError?.Invoke($"Incorrect C# class inheritance defined by {originalName}: {lastType.Name} does not inherit from {stub.Class.Name}.", null);
                        return false;
                    }
                }
                lastType = stub.Class;
            }

            if (namedStubs.ContainsKey(parentName))
            {
                stub._InternalParent = namedStubs[parentName];
                return FindAndSetParent(stub._InternalParent, originalName, false, lastType);
            }
            else
            {
                OnError?.Invoke($"Failed to find parent named '{parentName}' for definition stub {stub.Name}.", null);
                return false;
            }
        }

        public void Dispose()
        {
            rawDefinitions.Clear();
            stubs.Clear();
            namedStubs.Clear();
            cachedConstructors.Clear();
            tempDefs.Clear();
            currentAssemblies.Clear();
            json = null;
        }
    }
}
