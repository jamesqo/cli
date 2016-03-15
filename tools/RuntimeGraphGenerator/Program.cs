// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.DependencyModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;
using Microsoft.DotNet.ProjectModel.Graph;

namespace RuntimeGraphGenerator
{
    public class RuntimeGraphGeneratorCommand
    {
        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            string projectDirectory = null;
            string depsFile = null;
            IReadOnlyList<string> runtimes = null;
            try
            {
                ArgumentSyntax.Parse(args, syntax =>
                {
                    syntax.ApplicationName = "Runtime GraphGenerator";

                    syntax.HandleHelp = false;
                    syntax.HandleErrors = false;

                    syntax.DefineOption("p|project", ref projectDirectory, "Project location");
                    syntax.DefineOption("d|deps", ref depsFile, "Deps file path");

                    syntax.DefineParameterList("runtimes", ref runtimes, "Runtimes");
                });
            }
            catch (ArgumentSyntaxException exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }

            if (runtimes.Count == 0)
            {
                Reporter.Error.WriteLine("No runtimes specified");
                return 1;
            }
            if (!File.Exists(depsFile))
            {
                Reporter.Error.WriteLine($"Deps file not found: {depsFile}");
                return 1;
            }
            if (!Directory.Exists(projectDirectory))
            {
                Reporter.Error.WriteLine($"Project directory not found: {projectDirectory}");
                return 1;
            }

            try
            {
                DependencyContext context;
                using (var depsStream = File.OpenRead(depsFile))
                {
                    context = new DependencyContextJsonReader().Read(depsStream);
                }
                var framework = NuGetFramework.Parse(context.TargetFramework);
                var projectContext = ProjectContext.Create(projectDirectory, framework);

                var exporter = projectContext.CreateExporter(string.Empty);
                var manager = new RuntimeGraphManager();
                var graph = manager.Collect(exporter.GetDependencies(LibraryType.Package));
                var expandedGraph = manager.Expand(graph, runtimes);
                var graphManager = new RuntimeGraphManager();

                context = new DependencyContext(
                    context.TargetFramework,
                    context.Runtime,
                    context.IsPortable,
                    context.CompilationOptions,
                    context.CompileLibraries,
                    context.RuntimeLibraries,
                    expandedGraph
                    );

                using (var depsStream = File.OpenWrite(depsFile))
                {
                    new DependencyContextWriter().Write(context, depsStream);
                }

                return 0;
            }
            catch (Exception ex)
            {
#if DEBUG
                Reporter.Error.WriteLine(ex.ToString());
#else
                Reporter.Error.WriteLine(ex.Message);
#endif
                return 1;
            }
        }

    }
}
