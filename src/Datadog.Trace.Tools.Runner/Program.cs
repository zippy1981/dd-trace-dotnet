// <copyright file="Program.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using CommandLine;
using CommandLine.Text;

namespace Datadog.Trace.Tools.Runner
{
    internal class Program
    {
        private static CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private static string RunnerFolder { get; set; }

        private static Platform Platform { get; set; }

        private static void Main(string[] args)
        {
            // Initializing
            string executablePath = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location;
            string location = executablePath;
            if (string.IsNullOrEmpty(location))
            {
                location = Environment.GetCommandLineArgs().FirstOrDefault();
            }

            RunnerFolder = Path.GetDirectoryName(location);

            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                Platform = Platform.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                Platform = Platform.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                Platform = Platform.MacOS;
            }
            else
            {
                Console.Error.WriteLine("The current platform is not supported. Supported platforms are: Windows, Linux and MacOS.");
                Environment.Exit(-1);
                return;
            }

            // ***

            Console.CancelKeyPress += Console_CancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_ProcessExit;

            Parser parser = new Parser(settings =>
            {
                settings.AutoHelp = true;
                settings.AutoVersion = true;
                settings.EnableDashDash = true;
                settings.HelpWriter = null;
            });

            ParserResult<Options> result = parser.ParseArguments<Options>(args);
            Environment.ExitCode = result.MapResult(ParsedOptions, errors => ParsedErrors(result, errors));
        }

        private static int ParsedOptions(Options options)
        {
            string[] args = options.Value.ToArray();

            // Start logic

            Dictionary<string, string> profilerEnvironmentVariables = Utils.GetProfilerEnvironmentVariables(RunnerFolder, Platform, options);
            if (profilerEnvironmentVariables is null)
            {
                return 1;
            }

            if (options.SetEnvironmentVariables)
            {
                Console.WriteLine("Setting up the environment variables.");
                CIConfiguration.SetupCIEnvironmentVariables(profilerEnvironmentVariables);
            }
            else if (!string.IsNullOrEmpty(options.CrankImportFile))
            {
                return Crank.Importer.Process(options.CrankImportFile);
            }
            else
            {
                string cmdLine = string.Join(' ', args);
                if (!string.IsNullOrWhiteSpace(cmdLine))
                {
                    Console.WriteLine("Running: " + cmdLine);

                    // Check if the agent is listening
                    ProcessStartInfo agentProcessInfo = null;
                    if (!Utils.IsAgentRunning())
                    {
                        // Start a new agent instance
                        Console.WriteLine("Starting datadog agent...");

                        string homeFolder = Utils.GetTracerHomeFolder(RunnerFolder, options);
                        string agentPath = null;
                        string agentArgs = null;

                        if (Platform == Platform.Windows)
                        {
                            agentArgs = "-config .\\home\\datadog.yaml";

                            if (RuntimeInformation.OSArchitecture == Architecture.X64)
                            {
                                agentPath = Utils.FileExists(Path.Combine(homeFolder, "win-x64", "trace-agent.exe"));
                            }
                            else
                            {
                                Console.Error.WriteLine($"ERROR: Agent is not available for Windows {RuntimeInformation.OSArchitecture}.");
                                return 1;
                            }
                        }
                        else if (Platform == Platform.Linux)
                        {
                            agentArgs = "run -c ./home/datadog.yaml";

                            if (RuntimeInformation.OSArchitecture == Architecture.X64)
                            {
                                agentPath = Utils.FileExists(Path.Combine(homeFolder, "linux-x64", "agent"));
                            }
                            else if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
                            {
                                agentPath = Utils.FileExists(Path.Combine(homeFolder, "linux-arm64", "agent"));
                            }
                            else
                            {
                                Console.Error.WriteLine($"ERROR: Agent is not available for Linux {RuntimeInformation.OSArchitecture}.");
                                return 1;
                            }
                        }
                        else if (Platform == Platform.MacOS)
                        {
                            agentArgs = "-config ./home/datadog.yaml";

                            if (RuntimeInformation.OSArchitecture == Architecture.X64)
                            {
                                agentPath = Utils.FileExists(Path.Combine(homeFolder, "osx-x64", "trace-agent"));
                            }
                            else
                            {
                                Console.Error.WriteLine($"ERROR: Agent is not available for MacOS {RuntimeInformation.OSArchitecture}.");
                                return 1;
                            }
                        }

                        if (!string.IsNullOrEmpty(agentPath))
                        {
                            agentProcessInfo = new ProcessStartInfo(agentPath, agentArgs)
                            {
                                UseShellExecute = false,
                                WorkingDirectory = Environment.CurrentDirectory,
                                RedirectStandardOutput = false,
                                RedirectStandardInput = true,
                                RedirectStandardError = false,
                            };
                        }
                    }

                    ProcessStartInfo processInfo = Utils.GetProcessStartInfo(args[0], Environment.CurrentDirectory, profilerEnvironmentVariables);
                    if (args.Length > 1)
                    {
                        processInfo.Arguments = string.Join(' ', args.Skip(1).ToArray());
                    }

                    return Utils.RunProcess(processInfo, agentProcessInfo, _tokenSource.Token);
                }
            }

            return 0;
        }

        private static int ParsedErrors(ParserResult<Options> result, IEnumerable<Error> errors)
        {
            HelpText helpText = null;
            int exitCode = 1;
            if (errors.IsVersion())
            {
                helpText = HelpText.AutoBuild(result);
                exitCode = 0;
            }
            else
            {
                helpText = HelpText.AutoBuild(
                    result,
                    h =>
                    {
                        h.Heading = "Datadog APM Auto-instrumentation Runner";
                        h.AddNewLineBetweenHelpSections = true;
                        h.AdditionalNewLineAfterOption = false;
                        return h;
                    },
                    e =>
                    {
                        return e;
                    });
            }

            Console.WriteLine(helpText);
            return exitCode;
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            _tokenSource.Cancel();
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _tokenSource.Cancel();
        }
    }
}
