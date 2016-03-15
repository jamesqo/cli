﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Graph;
using NuGet.RuntimeModel;
using System.IO;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.DotNet.ProjectModel
{
    public class RuntimeGraphManager
    {
        private const string RuntimeJsonFileName = "runtime.json";

        public NuGet.RuntimeModel.RuntimeGraph Collect(IEnumerable<LibraryExport> exports)
        {
            var graph = RuntimeGraph.Empty;
            foreach (var export in exports)
            {
                if (export.Library.Identity.Type == LibraryType.Package)
                {
                    var runtimeJson =  ((PackageDescription) export.Library).Library.Files.FirstOrDefault(f => f == RuntimeJsonFileName);
                    if (runtimeJson != null)
                    {
                        var runtimeJsonFullName = Path.Combine(export.Library.Path, runtimeJson);
                        graph = RuntimeGraph.Merge(graph, JsonRuntimeFormat.ReadRuntimeGraph(runtimeJsonFullName));
                    }
                }
            }
            return graph;
        }

        public IEnumerable<RuntimeFallbacks> Expand(RuntimeGraph runtimeGraph, IEnumerable<string> runtimes)
        {
            foreach (var runtime in runtimes)
            {
                // ExpandRuntime return runtime itself as first item so we are skiping it
                yield return new RuntimeFallbacks(runtime, runtimeGraph.ExpandRuntime(runtime).Skip(1));
            }
        }
    }
}
