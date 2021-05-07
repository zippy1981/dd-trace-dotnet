using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Agent.Transports;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers.HttpMessageHandlers;
using Moq;
using Xunit;

namespace Datadog.Trace.IntegrationTests
{
    public class SnapshotTests
    {
#if NET452
        private const string FrameworkName = "net452";
#elif NET461
        private const string FrameworkName = "net461";
#elif NETCOREAPP2_1
        private const string FrameworkName = "netcoreapp2.1";
#elif NETCOREAPP3_0
        private const string FrameworkName = "netcoreapp3.0";
#elif NETCOREAPP3_1
        private const string FrameworkName = "netcoreapp3.1";
#elif NET5_0
        private const string FrameworkName = "net5.0";
#else
        // throw a compiler error 
#endif

        private readonly HttpClient _client;
        private readonly Uri _testAgentUri = new Uri("http://localhost:9126");

        public SnapshotTests()
        {
            _client = new HttpClient();
        }

        [Fact]
        [Trait("Category", "Snapshot")]
        [Trait("RunOnWindows", "True")]
        public async Task SingleTrace()
        {
            await ValidateSnapshotAsync(nameof(SingleTrace), action: (tracer) =>
            {
                using (var scope = tracer.StartActive(operationName: "single-operation", serviceName: "my-svc"))
                {
                    Span span = scope.Span;
                    span.SetTag("k", "v");
                    span.SetTag("num", "1234");
                    span.SetMetric("float_metric", 12.34);
                    span.SetMetric("int_metric", 4321);
                }
            });
        }

        [Fact]
        [Trait("Category", "Snapshot")]
        [Trait("RunOnWindows", "True")]
        public async Task MultipleTraces()
        {
            await ValidateSnapshotAsync(nameof(MultipleTraces), action: (tracer) =>
            {
                using (var scope = tracer.StartActive(operationName: "operation1", serviceName: "my-svc"))
                {
                    Span span = scope.Span;
                    span.SetTag("k", "v");
                    span.SetTag("num", "1234");
                    span.SetMetric("float_metric", 12.34);
                    span.SetMetric("int_metric", 4321);
                }

                using (var scope = tracer.StartActive(operationName: "operation2", serviceName: "my-svc"))
                {
                    Span span = scope.Span;
                    span.SetTag("k", "v");
                    span.SetTag("num", "1234");
                    span.SetMetric("float_metric", 12.34);
                    span.SetMetric("int_metric", 4321);
                }
            });
        }

        [Fact]
        [Trait("Category", "Snapshot")]
        [Trait("RunOnWindows", "True")]
        public async Task ParallelChildSpans()
        {
            await ValidateSnapshotAsync(nameof(ParallelChildSpans), asyncAction: async (tracer) =>
            {
                using (var scope = tracer.StartActive(operationName: "parent-operation", serviceName: "my-svc"))
                {
                    Span span = scope.Span;
                    span.SetTag("k", "v");
                    span.SetTag("num", "1234");
                    span.SetMetric("float_metric", 12.34);
                    span.SetMetric("int_metric", 4321);

                    // Child spans should each be a direct child of the first
                    int parallelism = 3;
                    var barrier = new Barrier(parallelism + 1);
                    Task[] tasks = new Task[parallelism];
                    for (int i = 0; i < parallelism; i++)
                    {
                        tasks[i] = Task.Run(
                            async () =>
                            {
                                // Start all tasks at the same time to remove timing issues
                                barrier.SignalAndWait();
                                using (var scope = tracer.StartActive(operationName: "parallel-operation", serviceName: "my-svc"))
                                {
                                    Span span = scope.Span;
                                    span.SetTag("k", "v");
                                    span.SetTag("num", "1234");
                                    span.SetMetric("float_metric", 12.34);
                                    span.SetMetric("int_metric", 4321);
                                }

                                await tracer.FlushAsync();
                            });
                    }

                    barrier.SignalAndWait();
                    await Task.WhenAll(tasks);
                }
            });
        }

        private async Task ValidateSnapshotAsync(string name, Action<Tracer> action = null, Func<Tracer, Task> asyncAction = null)
        {
            var settings = new TracerSettings()
            {
                StartupDiagnosticLogEnabled = false,
                AgentUri = _testAgentUri
            };
            var tracer = new Tracer(settings);

            string testCaseName = $"{GetType().FullName}.{name}_{FrameworkName}";

            var startTestResponse = await _client.GetAsync(new Uri(_testAgentUri, $"/test/start?token={testCaseName}"));
            if (!startTestResponse.IsSuccessStatusCode)
            {
                var responseString = await startTestResponse.Content.ReadAsStringAsync();
                Assert.True(startTestResponse.IsSuccessStatusCode, responseString);
            }

            if (action is not null)
            {
                action(tracer);
            }

            if (asyncAction is not null)
            {
                await asyncAction(tracer);
            }

            await tracer.FlushAsync();

            // I fear that there is a race condition with our serialization loop not sending the completed
            // trace out before issuing this snapshot request, but I don't want to get into that right now
            var resultResponse = await _client.GetAsync(new Uri(_testAgentUri, $"/test/snapshot?token={testCaseName}"));
            if (!resultResponse.IsSuccessStatusCode)
            {
                var responseString = await resultResponse.Content.ReadAsStringAsync();
                Assert.True(resultResponse.IsSuccessStatusCode, responseString);
            }
        }
    }
}
