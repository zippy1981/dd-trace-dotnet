// <copyright file="ITRClient.NetCore.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETCOREAPP
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Datadog.Trace.Ci.ITR;

internal partial class ITRClient
{
    private HttpClient _client;

    private void InitializeClient()
    {
        _client = new HttpClient();
    }

    private async Task<RawResponse> SendJsonDataAsync(Uri url, string inputJson)
    {
        using var content = new StringContent(inputJson, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(url, content).ConfigureAwait(false);
        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return new RawResponse((int)response.StatusCode, responseContent);
    }

    private async Task<RawResponse> SendMultipartJsonWithStreamAsync(Uri url, string jsonName, string jsonContent, string streamName, Stream streamContent)
    {
        using var formDataContent = new MultipartFormDataContent();

        // First Content
        formDataContent.Add(new StringContent(jsonContent, Encoding.UTF8, "application/json"), jsonName);

        // Second Content
        var packContent = new StreamContent(streamContent);
        packContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        formDataContent.Add(packContent, streamName);

        // Send payload
        var response = await _client.PostAsync(_packFileUrl, formDataContent).ConfigureAwait(false);
        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return new RawResponse((int)response.StatusCode, responseContent);
    }
}
#endif
