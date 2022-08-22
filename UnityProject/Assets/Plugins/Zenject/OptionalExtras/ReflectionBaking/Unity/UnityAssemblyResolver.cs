﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Zenject.ReflectionBaking.Mono.Cecil;

namespace Zenject.ReflectionBaking
{
    public class UnityAssemblyResolver : BaseAssemblyResolver
    {
        readonly IDictionary<string, string> _appDomainAssemblyLocations;
        readonly IDictionary<string, AssemblyDefinition> _cache;

        public UnityAssemblyResolver()
        {
            _appDomainAssemblyLocations = new Dictionary<string, string>();
            _cache = new Dictionary<string, AssemblyDefinition>();

            AppDomain domain = AppDomain.CurrentDomain;

            Assembly[] assemblies = domain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                if (assemblies[i].IsDynamic)
                {
                    continue;
                }

                try
                {
                    var location = assemblies[i].Location;
                    if (string.IsNullOrEmpty(location))
                    {
                        continue;
                    }

                    var directoryName = Path.GetDirectoryName(location);
                    AddSearchDirectory(directoryName);
                    _appDomainAssemblyLocations[assemblies[i].FullName] = location;
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            AssemblyDefinition assemblyDef = FindAssemblyDefinition(name.FullName, null);

            if (assemblyDef == null)
            {
                assemblyDef = base.Resolve(name);
                _cache[name.FullName] = assemblyDef;
            }

            return assemblyDef;
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            AssemblyDefinition assemblyDef = FindAssemblyDefinition(name.FullName, parameters);

            if (assemblyDef == null)
            {
                assemblyDef = base.Resolve(name, parameters);
                _cache[name.FullName] = assemblyDef;
            }

            return assemblyDef;
        }

        /// Searches for AssemblyDefinition in our cache, and failing that,
        /// looks for a known location.  Returns null if both attempts fail.
        AssemblyDefinition FindAssemblyDefinition(string fullName, ReaderParameters parameters)
        {
            if (fullName == null)
            {
                throw new ArgumentNullException("fullName");
            }

            AssemblyDefinition assemblyDefinition;

            // Look in cache first
            if (_cache.TryGetValue(fullName, out assemblyDefinition))
            {
                return assemblyDefinition;
            }

            // Try to use known location

            string location;

            if (_appDomainAssemblyLocations.TryGetValue(fullName, out location))
            {
                if (parameters != null)
                {
                    assemblyDefinition = AssemblyDefinition.ReadAssembly(location, parameters);
                }
                else
                {
                    assemblyDefinition = AssemblyDefinition.ReadAssembly(location);
                }

                _cache[fullName] = assemblyDefinition;

                return assemblyDefinition;
            }

            return null;
        }
    }
}