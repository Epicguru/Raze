using System;
using System.Collections.Generic;
using System.Reflection;

namespace Raze.Defs
{
    /// <summary>
    /// This static class is in charge of handling instantiating <see cref="Defined{T}"/> objects given
    /// their corresponding <see cref="Def"/>.
    /// </summary>
    /// <typeparam name="TR">The 'real defined' class type to spawn, for example Tile.</typeparam>
    /// <typeparam name="TD">The 'definition class' associated with the real class, for example TileDef.</typeparam>
    public static class DefFactory<TR, TD> where TR : Defined<TD> where TD : Def
    {
        private static Dictionary<Type, Func<TD, TR>> constructors = new Dictionary<Type, Func<TD, TR>>();

        /// <summary>
        /// Sets up all constructors or custom spawning methods for all sub-classes of a definition.
        /// Calling <see cref="Create(string)"/> without calling this first will result in failure.
        /// <para>
        /// As an example, calling <code>Init("Tile")</code> will load and prepare all sub-classes of the Tile definition, ready to be spawned.
        /// Then calling <code>Create("Sand")</code> would return a new instance of the Sand class, which in turn inherits from Tile and is defined by a definition that
        /// inherits from TileDef.
        /// </para>
        /// </summary>
        /// <param name="baseDef">The name of the base definition to load all sub-defs for. May be real or abstract. Note that the actual def itself is not loaded and therefore cannot be Created.</param>
        public static void Init(string baseDef)
        {
            DefStub rootDef = Main.DefDatabase.Get(baseDef) ?? Main.DefDatabase.GetAbstract(baseDef);
            if (rootDef == null)
                throw new Exception($"Failed to find '{baseDef}' root def.");

            var tileDefs = Main.DefDatabase.GetChildren(rootDef, true);

            Debug.Trace($"Found {tileDefs.Count} '{baseDef}' defs loaded.");

            constructors.Clear();
            foreach (var d in tileDefs)
            {
                var def = d as TD;
                Type definedType = def.RealClass;

                if (definedType == null)
                {
                    Debug.Error($"{baseDef} def {d.Name} is missing a 'real class' (RealClass attribute). Could it not be loaded, or was it never specified?");
                    continue;
                }

                var defType = def.GetType();

                if (constructors.ContainsKey(defType))
                {
                    continue;
                }

                MethodInfo[] methods = defType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo customMethod = null;

                foreach (var method in methods)
                {
                    if (method.GetCustomAttribute(typeof(DefinedProviderAttribute)) == null)
                        continue;

                    if (!definedType.IsAssignableFrom(method.ReturnType))
                    {
                        Debug.Error($"The DefinedProvider method '{method.DeclaringType.FullName}.{method.Name}' returns a {method.ReturnType.Name} which is not assignable to {definedType.Name}.");
                        continue;
                    }

                    if (method.ContainsGenericParameters)
                    {
                        Debug.Error($"The DefinedProvider method '{method.DeclaringType.FullName}.{method.Name}' has a generic parameter which is not allowed.");
                        continue;
                    }

                    if (method.GetParameters().Length != 1)
                    {
                        Debug.Error($"The DefinedProvider method '{method.DeclaringType.FullName}.{method.Name}' has {method.GetParameters().Length} parameters, but there should be exactly 1 of a type assignable from TileDef.");
                        continue;
                    }

                    if (!method.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(TD)))
                    {
                        Debug.Error($"The DefinedProvider method '{method.DeclaringType.FullName}.{method.Name}' has a parameter of type {method.GetParameters()[0].ParameterType.Name} which is not assignable from {typeof(TD).Name} and is therefore invalid.");
                        continue;
                    }

                    customMethod = method;
                    break;
                }

                if (customMethod != null)
                {
                    constructors.Add(defType, (inDef) => (TR)customMethod.Invoke(null, new object[] { inDef }));
                }
                else
                {
                    var allInClass = def.RealClass.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    ConstructorInfo found = null;
                    foreach (var con in allInClass)
                    {
                        var param = con.GetParameters();
                        if (param.Length != 1)
                            continue;

                        if (param[0].ParameterType.IsAssignableFrom(typeof(TD)))
                        {
                            found = con;
                            break;
                        }
                    }

                    if (found == null)
                    {

                        Debug.Error($"{baseDef} definition {def.Name} has {def.RealClass.Name} as defined class, but the defined class has no public or private one-argument constructor and the {def.Class.Name} class has no DefinedProvider method.");
                    }
                    else
                    {
                        constructors.Add(defType, (inDef) => (TR)found.Invoke(new object[] { inDef }));
                    }
                }
            }

            Debug.Trace($"Created or found {constructors.Count} of {tileDefs.Count} constructors.");
        }

        /// <summary>
        /// Creates a new instance of a class associated with a definition. The definition is found using the ID.
        /// The method used to create the class is either a public or non-public constructor that takes a single Def argument,
        /// or a custom static method with the <c>[DefinedProvider]</c> attribute.
        /// </summary>
        /// <param name="id">The unique numerical ID of the Def. See <see cref="DefDatabase"/> for more info.</param>
        /// <returns>A new instance of the 'real class' associated with the definition.</returns>
        public static TR Create(ushort id)
        {
            return Create(Main.DefDatabase.Get(id)?.Name);
        }

        /// <summary>
        /// Creates a new instance of a class associated with a definition. The definition is found using it's name.
        /// The method used to create the class is either a public or non-public constructor that takes a single Def argument,
        /// or a custom static method with the <c>[DefinedProvider]</c> attribute.
        /// </summary>
        /// <param name="defName">The unique Name of the Def.</param>
        /// <returns>A new instance of the 'real class' associated with the definition.</returns>
        public static TR Create(string defName)
        {
            if (defName == null)
            {
                Debug.Warn("Null def name passed into Create!");
                return null;
            }

            if (Main.DefDatabase.Get(defName) is TD def)
                return Create(def);

            Debug.Warn($"Failed to find {typeof(TD).Name} def for name {defName}");
            return null;
        }

        /// <summary>
        /// Creates a new instance of a class associated with a definition.
        /// The method used to create the class is either a public or non-public constructor that takes a single Def argument,
        /// or a custom static method with the <c>[DefinedProvider]</c> attribute.
        /// </summary>
        /// <param name="def">The definition to spawn its <c>RealClass</c> from.</param>
        /// <returns>A new instance of the 'real class' associated with the definition.</returns>
        public static TR Create(TD def)
        {
            if (def == null)
            {
                Debug.Warn($"{typeof(TD).Name} passed into Create is null. Perhaps the def is not loaded?");
                return null;
            }

            if (constructors.TryGetValue(def.GetType(), out var f))
            {
                var created = f.Invoke(def);
                created?.ApplyDef(def);
                return created;
            }
            return null;
        }
    }
}
