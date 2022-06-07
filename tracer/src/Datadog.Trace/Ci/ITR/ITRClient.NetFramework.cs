// <copyright file="ITRClient.NetFramework.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETFRAMEWORK || NETSTANDARD
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Datadog.Trace.Ci.ITR;

internal partial class ITRClient
{
    private void InitializeClient()
    {
    }

    private async Task<RawResponse> SendJsonDataAsync(Uri url, string inputJson)
    {
        var client = WebRequest.CreateHttp(url);
        client.Method = "POST";
        client.ContentType = "application/json";

        using (var requestStream = await client.GetRequestStreamAsync().ConfigureAwait(false))
        {
            var jsonBytes = Encoding.UTF8.GetBytes(inputJson);
            await requestStream.WriteAsync(jsonBytes, 0, jsonBytes.Length).ConfigureAwait(false);
        }

        HttpWebResponse httpWebResponse;
        try
        {
            httpWebResponse = (HttpWebResponse)await client.GetResponseAsync().ConfigureAwait(false);
        }
        catch (WebException exception)
            when (exception.Status == WebExceptionStatus.ProtocolError && exception.Response != null)
        {
            // If the exception is caused by an error status code, ignore it and let the caller handle the result
            httpWebResponse = (HttpWebResponse)exception.Response;
        }

        string responseContent;
        using (var responseStream = httpWebResponse.GetResponseStream())
        {
            var streamReader = new StreamReader(responseStream);
            responseContent = await streamReader.ReadToEndAsync().ConfigureAwait(false);
        }

        return new RawResponse((int)httpWebResponse.StatusCode, responseContent);
    }

    private async Task<RawResponse> SendMultipartJsonWithStreamAsync(Uri url, string jsonName, string jsonContent, string streamName, Stream streamContent)
    {
        var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");

        var client = WebRequest.CreateHttp(url);
        client.Method = "POST";
        client.ContentType = "multipart/form-data; boundary=" + boundary;

        using (var requestStream = await client.GetRequestStreamAsync().ConfigureAwait(false))
        {
            var boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            // First Content
            await requestStream.WriteAsync(boundaryBytes, 0, boundaryBytes.Length).ConfigureAwait(false);
            var jsonContentBytes = Encoding.UTF8.GetBytes(
                $"Content-Disposition: form-data; name=\"{jsonName}\"\r\nContent-Type: application/json\r\n\r\n{jsonContent}");
            await requestStream.WriteAsync(jsonContentBytes, 0, jsonContentBytes.Length).ConfigureAwait(false);

            // Second content
            await requestStream.WriteAsync(boundaryBytes, 0, boundaryBytes.Length).ConfigureAwait(false);
            var streamContentBytes =
                Encoding.UTF8.GetBytes(
                    $"Content-Disposition: form-data; name=\"{streamName}\"\r\nContent-Type: application/octet-stream\r\n\r\n");
            await requestStream.WriteAsync(streamContentBytes, 0, streamContentBytes.Length).ConfigureAwait(false);

            await streamContent.CopyToAsync(requestStream).ConfigureAwait(false);

            var trailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            await requestStream.WriteAsync(trailer, 0, trailer.Length).ConfigureAwait(false);
        }

        HttpWebResponse httpWebResponse;
        try
        {
            httpWebResponse = (HttpWebResponse)await client.GetResponseAsync().ConfigureAwait(false);
        }
        catch (WebException exception)
            when (exception.Status == WebExceptionStatus.ProtocolError && exception.Response != null)
        {
            // If the exception is caused by an error status code, ignore it and let the caller handle the result
            httpWebResponse = (HttpWebResponse)exception.Response;
        }

        string responseContent;
        using (var responseStream = httpWebResponse.GetResponseStream())
        {
            var streamReader = new StreamReader(responseStream);
            responseContent = await streamReader.ReadToEndAsync().ConfigureAwait(false);
        }

        return new RawResponse((int)httpWebResponse.StatusCode, responseContent);
    }
}
#endif
