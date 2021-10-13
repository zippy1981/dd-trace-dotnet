// <copyright file="Program.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using CommandLine;
using CommandLine.Text;
using Spectre.Console;
using Spectre.Console.Cli;

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
            RunnerFolder = AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(RunnerFolder))
            {
                RunnerFolder = Path.GetDirectoryName(Environment.GetCommandLineArgs().FirstOrDefault());
            }

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
                AnsiConsole.MarkupLine("[red]The current platform is not supported. Supported platforms are: Windows, Linux and MacOS.[/]");
                Environment.Exit(1);
                return;
            }

            // ***

            Console.CancelKeyPress += Console_CancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_ProcessExit;
            // ***

            Parser parser = new Parser(settings =>
            {
                settings.AutoHelp = true;
                settings.AutoVersion = true;
                settings.EnableDashDash = true;
                settings.HelpWriter = null;
            });

            ParserResult<object> result = parser.ParseArguments<Options, DiagnosticsOptions>(args);
            Environment.ExitCode = result.MapResult<Options, DiagnosticsOptions, int>(ParsedOptions, ParsedDiagnosticsOptions, errors => ParsedErrors(result, errors));
        }

        private static int ParsedOptions(Options options)
        {
            try
            {
                string[] args = options.Value.ToArray();

                // Start logic

                Dictionary<string, string> profilerEnvironmentVariables = Utils.GetProfilerEnvironmentVariables(RunnerFolder, Platform, options);
                if (profilerEnvironmentVariables is null)
                {
                    return 1;
                }

                // We try to autodetect the CI Visibility Mode
                if (!options.EnableCIVisibilityMode)
                {
                    // Support for VSTest.Console.exe and dotcover
                    if (args.Length > 0 && (
                        string.Equals(args[0], "VSTest.Console", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[0], "dotcover", StringComparison.OrdinalIgnoreCase)))
                    {
                        options.EnableCIVisibilityMode = true;
                    }

                    // Support for dotnet test and dotnet vstest command
                    if (args.Length > 1 && string.Equals(args[0], "dotnet", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(args[1], "test", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(args[1], "vstest", StringComparison.OrdinalIgnoreCase))
                        {
                            options.EnableCIVisibilityMode = true;
                        }
                    }
                }

                if (options.EnableCIVisibilityMode)
                {
                    // Enable CI Visibility mode by configuration
                    profilerEnvironmentVariables[Configuration.ConfigurationKeys.CIVisibilityEnabled] = "1";
                }

                if (options.SetEnvironmentVariables)
                {
                    AnsiConsole.WriteLine("Setting up the environment variables.");
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
                        AnsiConsole.MarkupLine($"[green]Running: {cmdLine}[/]");

                        ProcessStartInfo processInfo = Utils.GetProcessStartInfo(args[0], Environment.CurrentDirectory, profilerEnvironmentVariables);
                        if (args.Length > 1)
                        {
                            processInfo.Arguments = string.Join(' ', args.Skip(1).ToArray());
                        }

                        return Utils.RunProcess(processInfo, _tokenSource.Token);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }

        private static int ParsedDiagnosticsOptions(DiagnosticsOptions options)
        {
            return 0;
        }

        private static int ParsedErrors(ParserResult<object> result, IEnumerable<Error> errors)
        {
            if (result.TypeInfo.Current == typeof(NullInstance))
            {
                // .
            }

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
                    },
                    true);
            }

            // AnsiConsole.MarkupLine("[purple3]                                      ``..--:://++ossyo[/]");
            // AnsiConsole.MarkupLine("[purple3]                     ``..--:://++ossyyyyyyyyyyyyyyyyyyy[/]");
            AnsiConsole.MarkupLine("[purple3]         -:://++oosyyyyyyyyyyyyyyyyyyyyy/.-.+yyyyyyyyyy`[/]");
            AnsiConsole.MarkupLine("[purple3]         oyyyyyyyys+:-:oyyyyyyyossyyyyo-  -: `+yyyyyyyy-[/]");
            AnsiConsole.MarkupLine("[purple3]         /yyyyyys-`   `.-+so+:. ``````    `s.  :yyyyyyy/[/]");
            AnsiConsole.MarkupLine("[purple3]         .yyyyy/`      /` ``            `--+s:  /yyyyyy+[/]");
            AnsiConsole.MarkupLine("[purple3]          yyyys`       .+                 .+yys/+yyyyyys[/]");
            AnsiConsole.MarkupLine("[purple3]          oyyys`        s.                .-:yyyyyyyyyyy`[/]");
            AnsiConsole.MarkupLine("[purple3]          /yyyys-      `s/     .:/:       syo/yyyyyyyyyy.[/]");
            AnsiConsole.MarkupLine("[purple3]          -yyyyyyo:.`.-oy:    /yyyy`      .//-yyyyyyyyyy:[/]");
            AnsiConsole.MarkupLine("[purple3]          `yyyyyyyyo:++/-     :so+/           /yyyyyyyyy+[/]");
            AnsiConsole.MarkupLine("[purple3]           syyyyyyy+           `             ` -syyyyyyys[/]");
            AnsiConsole.MarkupLine("[purple3]           +yyyyyyys`                    ./ooo+ -yyyyyyyy[/]");
            AnsiConsole.MarkupLine("[purple3]           -yyyyyyyy:                    -oyys- `yyyyyyyy.[/]");
            AnsiConsole.MarkupLine("[purple3]           `yyyyyyyys+.       .`           `-`  :yyyyyyyys+[/]");
            AnsiConsole.MarkupLine("[purple3]            syyyyyyyyyy+`      -/:.`      .+o-:/sso+/:--.oy[/]");
            AnsiConsole.MarkupLine("[purple3]            +yyyyyyyyyyy+      .-/syo+//+so:-.`        ` /y.[/]");
            AnsiConsole.MarkupLine("[purple3]            :yyyyyyyyyyyy      oo..`.-:::.            `s.-y:[/]");
            AnsiConsole.MarkupLine("[purple3]            `yyyyyyyyyyy:      /s              ``    `sy:.y/[/]");
            AnsiConsole.MarkupLine("[purple3]             yyyyyyyyy+.       .y`            .syo:``oyy/ yo[/]");
            AnsiConsole.MarkupLine("[purple3]             +yyyyyy+.``..`     y-           -yyyyyysyyyo sy[/]");
            AnsiConsole.MarkupLine("[purple3]             :yyyy/`     `:+-   o/    `//:-`:yyyyyyyyyyyy oy.[/]");
            AnsiConsole.MarkupLine("[purple3]             .yyy-         `+o` /o   .syyyyyyyyyyyyyyyyyy`/y:[/]");
            AnsiConsole.MarkupLine("[purple3]              yyy            oo.:y  -yyyyyyyyyyyyyysoo+/:`:y/[/]");
            AnsiConsole.MarkupLine("[purple3]              oyy+.          .ysoy..yyyysso++/::--:://+oosyyo[/]");
            AnsiConsole.MarkupLine("[purple3]              :ysso:          y: s:`::::://++ossssssoo++//:--[/]");
            AnsiConsole.MarkupLine("[purple3]              `.```          :y` /o++++///:--...````[/]");
            // AnsiConsole.MarkupLine("[purple3]                      .....-+s:   `[/]");
            // AnsiConsole.MarkupLine("[purple3]                       .:///-`[/]");
            AnsiConsole.MarkupLine("[purple3][/]");
            AnsiConsole.MarkupLine("[purple3]::::::-`     `::`  .:::::::.  .::`    `::::::-`    `-::::.`    `-::::-[/]");
            AnsiConsole.MarkupLine("[purple3]yy:::/oy+   `oyyo` .::oyo::. `ssyo    .ys:::/sy:  /ys/::+ys. `oyo/::/+[/]");
            AnsiConsole.MarkupLine("[purple3]yy     +y:  +y--y+    /y:    oy.:y+   .yo    `yy`-yo     -ys +y/   ```[/]");
            AnsiConsole.MarkupLine("[purple3]yy     /y: /y/-:sy/   /y:   /y:-:sy:  .yo     sy.:y+     `yy oy-  :oyy[/]");
            AnsiConsole.MarkupLine("[purple3]yy    -yy`-y+ ://sy:  /y:  :y+`///sy- .yo   `/yo `yy-   `oy/ -ys.   yy[/]");
            AnsiConsole.MarkupLine("[purple3]yysssys/`.ys`    `sy. /y: .yo     `sy..yysssyo/   `/syssso:   .+syssys[/]");
            AnsiConsole.MarkupLine("[purple3][/]");
            AnsiConsole.WriteLine(helpText);
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
