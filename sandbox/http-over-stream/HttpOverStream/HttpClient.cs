using System;
using System.IO;
using System.Text;

namespace HttpOverStream
{
    public class HttpClient
    {
        private const string CrLf = "\r\n";

        public HttpResponse Send(HttpRequest request, Stream stream)
        {
            // TODO: support async and cancellation
            SendRequest(request, stream);
            return stream.CanRead ? ReadResponse(stream) : null;
        }

        private static void SendRequest(HttpRequest request, Stream stream)
        {
            // optimization opportunity: cache the ascii-encoded bytes of commonly-used headers
            using (var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 2048, leaveOpen: true))
            {
                writer.Write($"{request.Verb} {request.Path} HTTP/1.1{CrLf}");

                writer.Write($"Host: {request.Host}{CrLf}");
                writer.Write($"Accept-Encoding: identity{CrLf}");
                writer.Write($"User-Agent: dd-trace-dotnet/1.20{CrLf}");
                //writer.Write($"Connection: close{CrLf}");
                writer.Write($"Content-Length: {request.Content.Length ?? 0}{CrLf}");

                foreach (var header in request.Headers)
                {
                    writer.Write($"{header.Name}: {header.Value}{CrLf}");
                }

                writer.Write(CrLf);
            }

            request.Content.WriteTo(stream);
            stream.Flush();
        }

        private static HttpResponse ReadResponse(Stream stream)
        {
            /*
            const string filname = "C:\\temp\\response.http";
            File.Delete(filname);

            using (var file = File.OpenWrite(filname))
            {
                stream.CopyTo(file);
                file.Flush();
                Console.WriteLine($"Wrote to file {filname}");
                return null;
            }
            */

            var headers = new HttpHeaders();
            int statusCode = 0;
            string responseMessage = null;

            using (var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 2048, leaveOpen: true))
            {
                string line = reader.ReadLine();

                // HTTP/1.1 200 OK
                string statusCodeString = line.Substring(9, 3);
                statusCode = int.Parse(statusCodeString);
                responseMessage = line.Substring(13);

                // read headers
                while (true)
                {
                    line = reader.ReadLine();

                    if (line == "")
                    {
                        // end of headers
                        break;
                    }

                    string[] headerParts = line.Split(':');
                    string name = headerParts[0].Trim();
                    string value = headerParts[1].Trim();
                    headers.Add(name, value);
                }
            }

            return new HttpResponse(statusCode, responseMessage, headers, new StreamContent(stream));
        }
    }
}
