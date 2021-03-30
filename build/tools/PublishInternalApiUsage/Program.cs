using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CsvHelper;
using Mono.Cecil;

namespace PublishInternalApiUsage
{
    public class Program
    {
        private static readonly string _tempDirectory = "tmp";
        private static readonly string[] _targetFrameworks = { "net45", "net461", "netcoreapp3.1", "netstandard2.0" };
        private static readonly string[] _targetAssemblies = { "Datadog.Trace.ClrProfiler.Managed.dll", "Datadog.Trace.AspNet.dll" };

        private static string _csvPath = "Datadog_Trace_InternalApiUsage.csv";

        public static async Task Main(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "--help")
                {
                    Console.WriteLine("Usage: PublishInternalApiUsage [output_csv_path]");
                    return;
                }
                else
                {
                    _csvPath = args[0];
                }
            }

            var versionsToDirectoriesDict = await DownloadAndExtractFromGithubReleases();
            var missingMethodNamesToVersionsDict = ResolveMethodsAndMarkFailures(versionsToDirectoriesDict);

            WriteCsv(_csvPath, versionsToDirectoriesDict.Keys.ToList(), missingMethodNamesToVersionsDict);
            Directory.Delete(_tempDirectory, recursive: true);
        }

        /// <summary>
        /// For each version, load a set of Datadog.Trace.* assemblies, iterate through all Datadog.Trace.dll member references,
        /// attempt to resolve them, and mark all failures in a Dictionary that maps the name<->failing versions
        /// </summary>
        /// <param name="versionsToDirectoriesDict">A dictionary mapping each version to its directory on disk</param>
        /// <returns>A dictionary mapping names of member references to their failing versions</returns>
        private static Dictionary<string, HashSet<Version>> ResolveMethodsAndMarkFailures(Dictionary<Version, string> versionsToDirectoriesDict)
        {
            Dictionary<string, HashSet<Version>> missingMethodNamesToVersionsDict = new Dictionary<string, HashSet<Version>>();

            foreach (Version inputVersion in versionsToDirectoriesDict.Keys)
            {
                foreach (Version bindingVersion in versionsToDirectoriesDict.Keys.Where(bv => bv >= inputVersion))
                {
                    Console.WriteLine("Analyzing input version {0}, against binding version {1}", inputVersion, bindingVersion);

                    foreach (string targetFramework in _targetFrameworks)
                    {
                        foreach (string targetAssembly in _targetAssemblies)
                        {
                            // The input assembly is drawn from the input version directory
                            string inputAssemblyFile = Path.Combine(versionsToDirectoriesDict[inputVersion], targetFramework, targetAssembly);

                            // The directory for dependencies is drawn from the binding version directory
                            string bindingAssemblyDirectory = Path.Combine(versionsToDirectoriesDict[bindingVersion], targetFramework);

                            if (!File.Exists(inputAssemblyFile) || !Directory.Exists(bindingAssemblyDirectory))
                            {
                                continue;
                            }

                            using var resolver = new DefaultAssemblyResolver();
                            resolver.AddSearchDirectory(bindingAssemblyDirectory);

                            using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(inputAssemblyFile, new ReaderParameters { AssemblyResolver = resolver }))
                            {
                                var memberReferences = assembly.MainModule.GetMemberReferences();
                                foreach (MemberReference memberReference in memberReferences)
                                {
                                    // Skip the type if it's not from Datadog.Trace.dll
                                    string scopeName = memberReference.DeclaringType.Scope.Name;
                                    if (scopeName != "Datadog.Trace")
                                    {
                                        continue;
                                    }

                                    // Attempt to resolve each of them and mark failures for the ones that do not resolve on Datadog.Trace.dll
                                    try
                                    {
                                        IMemberDefinition memberDef = memberReference.Resolve();
                                        if (memberDef is null)
                                        {
                                            string key = memberReference.ToString();
                                            if (!missingMethodNamesToVersionsDict.TryGetValue(key, out HashSet<Version> versions))
                                            {
                                                missingMethodNamesToVersionsDict[key] = new HashSet<Version>();
                                            }

                                            missingMethodNamesToVersionsDict[key].Add(bindingVersion);
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine("Error processing {0}", memberReference.FullName);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return missingMethodNamesToVersionsDict;
        }

        private static void WriteCsv(string path, List<Version> allVersions, Dictionary<string, HashSet<Version>> missingMethodsByVersion)
        {
            int headersLength = allVersions.Count + 1;

            using (var writer = new StreamWriter(path))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                // Write headers as the first row of regular fields
                csv.WriteField("Method Name");
                foreach (Version version in allVersions.OrderByDescending(v => v))
                {
                    csv.WriteField(version);
                }

                csv.NextRecord();

                foreach (KeyValuePair<string, HashSet<Version>> kvp in missingMethodsByVersion)
                {
                    csv.WriteField(kvp.Key);

                    foreach (Version version in allVersions.OrderByDescending(v => v))
                    {
                        if (kvp.Value.Contains(version))
                        {
                            csv.WriteField("X");
                        }
                        else
                        {
                            csv.WriteField(string.Empty);
                        }
                    }

                    csv.NextRecord();
                }
            }
        }

        /// <summary>
        /// Download "windows-tracer-home.zip" from dd-trace-dotnet GitHub releases, extract to a temporary directory,
        /// and return Dictionary of version<->directory
        /// </summary>
        /// <returns></returns>
        private static async Task<Dictionary<Version, string>> DownloadAndExtractFromGithubReleases()
        {
            Dictionary<Version, string> versionToPathDict = new Dictionary<Version, string>();

            // Hit the GitHub API to inspect dd-trace-dotnet releases
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; dotnetapp/1.0)");

            using var responseMessage = await client.GetAsync("https://api.github.com/repos/DataDog/dd-trace-dotnet/releases?per_page=100");
            responseMessage.EnsureSuccessStatusCode();

            var content = await responseMessage.Content.ReadAsStringAsync();
            using (JsonDocument document = JsonDocument.Parse(content))
            {
                foreach (JsonElement releaseElement in document.RootElement.EnumerateArray())
                {
                    // Skip pre-release releases
                    bool isPrerelease = releaseElement.GetProperty("prerelease").GetBoolean();
                    if (isPrerelease)
                    {
                        continue;
                    }

                    // Find the asset named "windows-tracer-home.zip"
                    string tracerHomeUrl = string.Empty;
                    foreach (JsonElement assetElement in releaseElement.GetProperty("assets").EnumerateArray())
                    {
                        string name = assetElement.GetProperty("name").GetString();
                        if (name.Equals("windows-tracer-home.zip", StringComparison.OrdinalIgnoreCase))
                        {
                            tracerHomeUrl = assetElement.GetProperty("browser_download_url").GetString();
                        }
                    }

                    // If no "windows-tracer-home.zip", skip this version
                    if (string.IsNullOrEmpty(tracerHomeUrl))
                    {
                        continue;
                    }

                    // Download "windows-tracer-home.zip" file
                    string version = releaseElement.GetProperty("name").GetString();
                    Console.WriteLine("Downloading windows-tracer-home.zip for release: {0}", version);

                    var downloadResponseMessage = await client.GetAsync(tracerHomeUrl);
                    downloadResponseMessage.EnsureSuccessStatusCode();

                    using (var ms = await downloadResponseMessage.Content.ReadAsStreamAsync())
                    using (var fs = File.Create("windows-tracer-home.zip"))
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        ms.CopyTo(fs);
                    }

                    // Extract zip to <temp_path>/<version>
                    string targetDirectory = Path.Combine(_tempDirectory, version);
                    Directory.CreateDirectory(targetDirectory);
                    ZipFile.ExtractToDirectory("windows-tracer-home.zip", targetDirectory, overwriteFiles: true);
                    versionToPathDict.Add(new Version(version), targetDirectory);

                    File.Delete("windows-tracer-home.zip");
                }
            }

            return versionToPathDict;
        }
    }
}
