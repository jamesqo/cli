// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.InternalAbstractions;

namespace Microsoft.DotNet.Tools.Restore
{
    public partial class RestoreCommand
    {
        private static readonly string DefaultRid = RuntimeEnvironmentRidExtensions.GetLegacyRestoreRuntimeIdentifier();

        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var app = new CommandLineApplication(false)
            {
                Name = "dotnet restore",
                FullName = ".NET project dependency restorer",
                Description = "Restores dependencies listed in project.json"
            };

            // Parse --quiet, because we have to handle that specially since NuGet3 has a different
            // "--verbosity" switch that goes BEFORE the command
            var quiet = args.Any(s => s.Equals("--quiet", StringComparison.OrdinalIgnoreCase));
            args = args.Where(s => !s.Equals("--quiet", StringComparison.OrdinalIgnoreCase)).ToArray();

            app.OnExecute(() =>
            {
                try
                {
                    return NuGet3.Restore(args, quiet);
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine(e.Message);

                    return -1;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);

                    return -2;
                }
            });

            return app.Execute(args);
        }
    }
}
