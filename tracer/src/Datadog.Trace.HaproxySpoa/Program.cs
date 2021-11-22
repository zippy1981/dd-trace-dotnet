// <copyright file="Program.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Datadog.Trace.AppSec;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Tagging;
using HAProxy.StreamProcessingOffload.Agent;
using HAProxy.StreamProcessingOffload.Agent.Payloads;

namespace Datadog.Trace.HaproxySpoa
{
    internal class Program
    {
        private const string OperationName = "haproxy";

        private static readonly ConcurrentDictionary<string, Holder> UidToSpanMap = new();

        private static Dictionary<string, object> ParseQuery(string query)
        {
            // not so much defensive, as offensive programming
            var dict = query.Split('&').Select(x => x.Split('=')).ToDictionary(x => x[0], x => (object)x[1]);
            return dict;
        }

        private static Dictionary<string, object> ParseHeaders(string headers)
        {
            var dict = headers.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Split(':')).ToDictionary(x => x[0], x => (object)x[1]);
            return dict;
        }

        private static void Main(string[] args)
        {
            IPAddress address = IPAddress.Parse("0.0.0.0");
            int port = 12345;
            var listener = new TcpListener(address, port);
            var frameProcessor = new FrameProcessor()
            {
                EnableLogging = false,
                // LogFunc = (msg) => Console.WriteLine(msg)
            };

            listener.Start();
            Console.WriteLine("Version 0.1");
            Console.WriteLine("Listening on {0}:{1}", address, port);

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                Task.Run(() =>
                {
                    NetworkStream stream = client.GetStream();

                    // Cancel stream when process terminates
                    System.AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                    {
                        frameProcessor.CancelStream(stream);
                    };

                    frameProcessor.HandleStream(stream, (notifyFrame) =>
                    {
                        var messages = ((ListOfMessagesPayload)notifyFrame.Payload).Messages;
                        var responseActions = new List<SpoeAction>();

                        var tracer = Tracer.Instance;

                        foreach (var message in messages)
                        {
                            try
                            {
                                switch (message.Name)
                                {
                                    case "request-message":
                                        {
                                            Console.WriteLine("Received request: " + string.Join(", ", message.Args.Keys));
                                            var hasUid = message.Args.TryGetValue("uid", out var uid);
                                            Console.WriteLine("uid: " + uid.Type + ", " + uid.ToString());
                                            var hasMethod = message.Args.TryGetValue("method", out var method);
                                            var hasPath = message.Args.TryGetValue("path", out var path);
                                            var hasQuery = message.Args.TryGetValue("query", out var query);
                                            var hasHdrs = message.Args.TryGetValue("hdrs", out var hdrs);

                                            var methodValue = method.ToString();

                                            var tags = new WebTags();
                                            var scope = tracer.StartActiveWithTags(OperationName, tags: tags, serviceName: OperationName + "." + path);
                                            scope.Span.DecorateWebServerSpan(resourceName: path.Value.ToString(), methodValue, null, null, tags, Enumerable.Empty<KeyValuePair<string, string>>());

                                            var headerDict = ParseHeaders(hdrs.ToString());
                                            var args = new Dictionary<string, object>
                                            {
                                                { AddressesConstants.RequestMethod, methodValue },
                                                { AddressesConstants.RequestUriRaw, path.ToString() },
                                                { AddressesConstants.RequestQuery, ParseQuery(query.ToString()) },
                                                { AddressesConstants.RequestHeaderNoCookies, headerDict },
                                            };
                                            var transport = new SpoaTransport(false, key =>
                                            {
                                                headerDict.TryGetValue(key, out var value);
                                                return value?.ToString();
                                            });

                                            Security.Instance.InstrumentationGateway.RaiseEvent(args, transport, scope.Span);

                                            var holder = new Holder() { Scope = scope, SpoaTransport = transport };
                                            UidToSpanMap.AddOrUpdate(uid.ToString(), holder, (_, _) => holder);
                                        }

                                        break;
                                    case "response-message":
                                        {
                                            Console.WriteLine("Received response: " + string.Join(", ", message.Args.Keys));
                                            var hasUid = message.Args.TryGetValue("uid", out var uid);
                                            Console.WriteLine("uid: " + uid.Type + ", " + uid.ToString());
                                            var hasStatus = message.Args.TryGetValue("status", out var status);
                                            if (UidToSpanMap.TryRemove(uid.ToString(), out var holder))
                                            {
                                                holder.Scope.Span.SetHttpStatusCode(int.Parse(status.Value.ToString()), isServer: true);
                                                Console.WriteLine("Sending span: " + holder.Scope.Span.SpanId);
                                                holder.Scope.Dispose();
                                                holder.SpoaTransport.DoCompleted();
                                            }
                                        }

                                        break;
                                    default:
                                        Console.WriteLine("Unknown message: " + message.ToString());
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error processing message: " + ex);
                            }
                        }

                        return responseActions;
                    });
                });
            }
        }

        private class Holder
        {
            public Scope Scope { get; set; }

            public SpoaTransport SpoaTransport { get; set; }
        }
    }
}
