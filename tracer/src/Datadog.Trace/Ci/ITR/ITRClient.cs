// <copyright file="ITRClient.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.Ci.ITR;

/// <summary>
/// Intelligent Test Runner Client
/// </summary>
internal partial class ITRClient
{
    private const string BaseUrl = "https://git-api-ci-app-backend.us1.staging.dog";
    private const int MaxRetries = 3;
    private const int MaxPackFileSizeInMb = 3;

    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(ITRClient));

    private readonly GlobalSettings _globalSettings;
    private readonly string _repository;
    private readonly string _workingDirectory;
    private readonly Uri _searchCommitsUrl;
    private readonly Uri _packFileUrl;

    public ITRClient(string repository, string workingDirectory)
    {
        _globalSettings = GlobalSettings.FromDefaultSources();
        _repository = repository;
        _workingDirectory = workingDirectory;

        _searchCommitsUrl = new UriBuilder(BaseUrl)
        {
            Path = $"/repository/{repository}/search_commits"
        }.Uri;

        _packFileUrl = new UriBuilder(BaseUrl)
        {
            Path = $"/repository/{repository}/packfile"
        }.Uri;

        InitializeClient();
    }

    public async Task<long> UploadRepositoryChangesAsync()
    {
        var gitOutput = await ProcessHelpers.RunCommandAsync(new ProcessHelpers.Command("git", "log --format=%H -n 1000 --since=\"1 month ago\"", _workingDirectory)).ConfigureAwait(false);
        var localCommits = gitOutput.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (localCommits.Length == 0)
        {
            return 0;
        }

        var remoteCommitsData = await SearchCommitAsync(localCommits).ConfigureAwait(false);
        if (localCommits.Length == remoteCommitsData.Length && !localCommits.Except(remoteCommitsData).Any())
        {
            return 0;
        }

        return await SendObjectsPackFileAsync(localCommits[0], remoteCommitsData).ConfigureAwait(false);
    }

    public async Task<string[]> SearchCommitAsync(string[] localCommits)
    {
        if (localCommits is null)
        {
            return null;
        }

        var commitRequests = new CommitRequest[localCommits.Length];
        for (var i = 0; i < localCommits.Length; i++)
        {
            commitRequests[i] = new CommitRequest(localCommits[i]);
        }

        var jsonPushedSha = JsonConvert.SerializeObject(new DataArrayEnvelope<CommitRequest>(commitRequests));

        return await WithRetries(InternalSearchCommitAsync, jsonPushedSha, MaxRetries).ConfigureAwait(false);

        async Task<string[]> InternalSearchCommitAsync(string state, bool finalTry)
        {
            var response = await SendJsonDataAsync(_searchCommitsUrl, state).ConfigureAwait(false);
            if (response.StatusCode is < 200 or >= 300)
            {
                if (finalTry)
                {
                    try
                    {
                        Log.Error<int, string>("Failed to submit events with status code {StatusCode} and message: {ResponseContent}", response.StatusCode, response.Content);
                    }
                    catch (Exception ex)
                    {
                        Log.Error<int>(ex, "Unable to read response for failed request with status code {StatusCode}", response.StatusCode);
                    }
                }

                throw new WebException($"Status: {response.StatusCode}, Content: {response.Content}");
            }

            var deserializedResult = JsonConvert.DeserializeObject<DataArrayEnvelope<CommitResponse>>(response.Content);
            if (deserializedResult.Data is null)
            {
                return null;
            }

            var stringArray = new string[deserializedResult.Data.Length];
            for (var i = 0; i < deserializedResult.Data.Length; i++)
            {
                stringArray[i] = deserializedResult.Data[i].Id;
            }

            return stringArray;
        }
    }

    public async Task<long> SendObjectsPackFileAsync(string commitSha, string[] commitsExceptions)
    {
        var jsonPushedSha = JsonConvert.SerializeObject(new DataEnvelope<CommitRequest>(new CommitRequest(commitSha)));
        var packFiles = await GetObjectsPackFileFromWorkingDirectoryAsync(commitsExceptions).ConfigureAwait(false);
        long totalUploadSize = 0;
        foreach (var packFile in packFiles)
        {
            // Send PackFile content
            totalUploadSize += await WithRetries(InternalSendObjectsPackFileAsync, packFile, MaxRetries).ConfigureAwait(false);

            // Delete temporal pack file
            try
            {
                File.Delete(packFile);
            }
            catch
            {
                // .
            }
        }

        return totalUploadSize;

        async Task<long> InternalSendObjectsPackFileAsync(string packFile, bool finalTry)
        {
            using var fileStream = File.Open(packFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var response = await SendMultipartJsonWithStreamAsync(_packFileUrl, "pushedSha", jsonPushedSha, "packfile", fileStream).ConfigureAwait(false);
            if (response.StatusCode is < 200 or >= 300)
            {
                if (finalTry)
                {
                    try
                    {
                        Log.Error<int, string>("Failed to submit events with status code {StatusCode} and message: {ResponseContent}", response.StatusCode, response.Content);
                    }
                    catch (Exception ex)
                    {
                        Log.Error<int>(ex, "Unable to read response for failed request with status code {StatusCode}", response.StatusCode);
                    }
                }

                throw new WebException($"Status: {response.StatusCode}, Content: {response.Content}");
            }

            return fileStream.Length;
        }
    }

    private async Task<string[]> GetObjectsPackFileFromWorkingDirectoryAsync(string[] commitsExceptions)
    {
        commitsExceptions ??= Array.Empty<string>();
        var temporalPath = Path.GetTempFileName();

        var getObjectsArguments = "log --format=format:%H%n%T --since=\"1 month ago\" HEAD " + string.Join(" ", commitsExceptions.Select(c => "^" + c));
        var getObjects = await ProcessHelpers.RunCommandAsync(new ProcessHelpers.Command("git", getObjectsArguments, _workingDirectory)).ConfigureAwait(false);

        var getPacksArguments = $"pack-objects --compression=9 --max-pack-size={MaxPackFileSizeInMb}m  {temporalPath}";
        var packObjectsResult = await ProcessHelpers.RunCommandAsync(new ProcessHelpers.Command("git", getPacksArguments, _workingDirectory), getObjects).ConfigureAwait(false);

        return Directory.GetFiles(Path.GetDirectoryName(temporalPath) ?? string.Empty, $"{Path.GetFileName(temporalPath)}*.pack");
    }

    private async Task<T> WithRetries<T, TState>(Func<TState, bool, Task<T>> sendDelegate, TState state, int numOfRetries)
    {
        var retryCount = 1;
        var sleepDuration = 100; // in milliseconds

        while (true)
        {
            T response = default;
            bool success = false;
            ExceptionDispatchInfo exceptionDispatchInfo = null;
            bool isFinalTry = retryCount >= numOfRetries;

            try
            {
                response = await sendDelegate(state, isFinalTry).ConfigureAwait(false);
                success = true;
            }
            catch (Exception ex)
            {
                exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);

                if (_globalSettings.DebugEnabled)
                {
                    if (ex.InnerException is InvalidOperationException ioe)
                    {
                        Log.Error(ex, "An error occurred while sending data to the Intelligent Test Runner");
                        return default;
                    }
                }
            }

            // Error handling block
            if (!success)
            {
                if (isFinalTry)
                {
                    // stop retrying
                    Log.Error<int>(exceptionDispatchInfo.SourceException, "An error occurred while sending intelligent test runner data after {Retries} retries.", retryCount);
                    exceptionDispatchInfo?.Throw();
                    return default;
                }

                // Before retry delay
                bool isSocketException = false;
                Exception innerException = exceptionDispatchInfo.SourceException;

                while (innerException != null)
                {
                    if (innerException is SocketException)
                    {
                        isSocketException = true;
                        break;
                    }

                    innerException = innerException.InnerException;
                }

                if (isSocketException)
                {
                    Log.Debug(exceptionDispatchInfo.SourceException, "Unable to communicate with the server");
                }

                // Execute retry delay
                await Task.Delay(sleepDuration).ConfigureAwait(false);
                retryCount++;
                sleepDuration *= 2;

                continue;
            }

            Log.Debug("Successfully sent intelligent test runner data");
            return response;
        }
    }

    private readonly struct RawResponse
    {
        public readonly int StatusCode;
        public readonly string Content;

        public RawResponse(int statusCode, string content)
        {
            StatusCode = statusCode;
            Content = content;
        }
    }

    private readonly struct DataEnvelope<T>
    {
        [JsonProperty("data")]
        public readonly T Data;

        public DataEnvelope(T data)
        {
            Data = data;
        }
    }

    private readonly struct DataArrayEnvelope<T>
    {
        [JsonProperty("data")]
        public readonly T[] Data;

        public DataArrayEnvelope(T[] data)
        {
            Data = data;
        }
    }

    private class CommitRequest
    {
        public CommitRequest(string id)
        {
            Id = id;
            Type = "commit";
        }

        [JsonProperty("id")]
        public string Id { get; }

        [JsonProperty("type")]
        public string Type { get; }
    }

    private class CommitResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("attributes")]
        public object Attributes { get; set; }
    }
}
