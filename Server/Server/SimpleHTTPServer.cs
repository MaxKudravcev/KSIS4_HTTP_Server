using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace Server
{
    class SimpleHTTPServer
    {
        /// <summary>
        /// Standard names for index files
        /// </summary>
        private readonly string[] _indexFiles =
        {
            "index.htm",
            "index.html",
            "default.htm",
            "default.html"
        };

        /// <summary>
        /// Dictionary which maps a file extension to its MIME type 
        /// </summary>
        private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
        #region extension to MIME type list
        {".avi", "video/x-msvideo"},
        {".bin", "application/octet-stream"},
        {".css", "text/css"},
        {".dll", "application/octet-stream"},
        {".exe", "application/octet-stream"},
        {".gif", "image/gif"},
        {".htm", "text/html"},
        {".html", "text/html"},
        {".ico", "image/x-icon"},
        {".img", "application/octet-stream"},
        {".jpeg", "image/jpeg"},
        {".jpg", "image/jpeg"},
        {".js", "application/x-javascript"},
        {".mp3", "audio/mpeg"},
        {".mpeg", "video/mpeg"},
        {".mpg", "video/mpeg"},
        {".pdf", "application/pdf"},
        {".png", "image/png"},
        {".rar", "application/x-rar-compressed"},
        {".txt", "text/plain"},
        {".xml", "text/xml"},
        {".zip", "application/zip"},
        #endregion
        };

        private Thread _serverThread;
        private string _rootDirectory;
        private HttpListener _listener;
        private int _port;

        /// <summary>
        /// Construct server with given port (or default port 8080)
        /// </summary>
        /// <param name="path">Path to the directory to serve</param>
        /// <param name="port">Port of the server. Default value is 8080</param>
        public SimpleHTTPServer(string path, int port = 80)
        {
            this.Initialize(path, port);
        }

        /// <summary>
        /// Abort server thread and stop HTTPListener
        /// </summary>
        public void Stop()
        {
            _serverThread.Abort();
            _listener.Stop();
        }

        /// <summary>
        /// Creates a new instance of HttpListener which will listen to all http requests on specified port.
        /// </summary>
        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
            _listener.Start();

            while (true)
            {
                HttpListenerContext context = _listener.GetContext();
                Process(context);
            }
        }


        /// <summary>
        /// Processess a request.
        /// </summary>
        /// <param name="context"></param>
        private void Process(HttpListenerContext context)
        {
            //Get file name from request
            string filename = context.Request.Url.AbsolutePath;

            #region Request output
            Console.WriteLine("Method: {0}\nRequested URL: {1}\nUser-Agent: {2}\nUser-host: {3}", context.Request.HttpMethod, context.Request.Url.OriginalString, context.Request.UserAgent, context.Request.UserHostAddress);
            Console.Write("Accept:");
            foreach (string str in context.Request.AcceptTypes)
                Console.Write($" {str},");
            Console.WriteLine("\n");
            #endregion

            filename = filename.Substring(1);
            //If file name is not specified, check default index file names
            if(string.IsNullOrEmpty(filename))
            {
                foreach (string indexFile in _indexFiles)
                {
                    if (File.Exists(Path.Combine(_rootDirectory, indexFile)))
                    {
                        filename = indexFile;
                        break;
                    }
                }
            }

            filename = Path.Combine(_rootDirectory, filename);

            if (File.Exists(filename))
            {
                try
                {
                    Stream input = new FileStream(filename, FileMode.Open);

                    //Add permanent http response headers
                    string mime;
                    context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/octet-stream";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));


                    //Write the requested file to Response.OutputStream
                    byte[] buffer = new byte[1024 * 16];
                    int bytes;
                    while ((bytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, bytes);
                    input.Close();

                    //Set status code and flush everything left to a client
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();
                }
                catch
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            #region Response output
            Console.WriteLine("Server response:\nStatus code: {0}", context.Response.StatusCode.ToString());
            if (context.Response.StatusCode == 200)
                Console.WriteLine("Date: {0}\nLast-modified: {1}\nContent-type: {2}\nContent-length: {3}\n", context.Response.Headers.Get("Date"), context.Response.Headers.Get("Last-modified"), context.Response.ContentType, context.Response.ContentLength64.ToString());
            #endregion

            context.Response.OutputStream.Close();
        }

        private void Initialize(string path, int port)
        {
            _rootDirectory = path;
            _port = port;
            _serverThread = new Thread(this.Listen);
            _serverThread.Start();
        }
    }
}
