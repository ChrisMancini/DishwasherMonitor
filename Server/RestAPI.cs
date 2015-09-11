using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Shared;

namespace Server
{
    public class RestAPI : HttpServer
    {
        public RestAPI(int serverPort) : base(serverPort)
        {

        }

        protected override async Task WriteResponseAsync(string request, IOutputStream os)
        {
            var response = new StatusResponse();

            if (request.StartsWith("/api/seed"))
            {
                var sql = new SqlHelper();

                sql.SeedDatabase();
            }

            if (request.StartsWith("/api/status"))
            {
                var sql = new SqlHelper();

                var info = sql.Get();

                response.Status = info.CurrentStatus.ToString();

                if (info.CurrentStatus == DishwasherStatus.Clean)
                {
                    response.Details = new StatusResponse.CleanStatusDetails
                    {
                        DishwasherRun = sql.GetDishwasherRun()
                    };
                }
                else if (info.CurrentStatus == DishwasherStatus.Dirty)
                {
                    response.Details = new StatusResponse.DirtyStatusDetails
                    {
                        DirtyTime = info.DirtyDateTime
                    };
                }
                else if (info.CurrentStatus == DishwasherStatus.Running)
                {
                    response.Details = new StatusResponse.RunningStatusDetails
                    {
                        StartTime = info.CurrentRunStart,
                        RunCycle = info.CurrentCycle
                    };
                }
            }
            
            // Show the html 
            using (Stream resp = os.AsStreamForWrite())
            {
                string json;
                using (MemoryStream jsonStream = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(typeof(StatusResponse));
                    serializer.WriteObject(jsonStream, response);
                    jsonStream.Position = 0;
                    StreamReader sr = new StreamReader(jsonStream);
                    json = sr.ReadToEnd();
                }

                byte[] bodyArray = Encoding.UTF8.GetBytes(json);
                using (MemoryStream stream = new MemoryStream(bodyArray))
                {
                    string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                                  "Content-Length: {0}\r\n" +
                                                  "Content-Type: application/json\r\n" +
                                                  "Connection: close\r\n\r\n",
                        stream.Length);
                    byte[] headerArray = Encoding.UTF8.GetBytes(header);
                    await resp.WriteAsync(headerArray, 0, headerArray.Length);
                    await stream.CopyToAsync(resp);
                }
                await resp.FlushAsync();
            }
        }
    }
}