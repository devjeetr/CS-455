/*
 * Devjeet Roy
 * Student ID: 11404808
 * 
 * */
using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
namespace CS422
{
    class WebServer
    {
        private const string STUDENT_ID = "11404808";
        private const string CRLF = "\r\n";
        private const string DOUBLE_CRLF = "\r\n\r\n";

        private const string DEFAULT_TEMPLATE =
            "HTTP/1.1 200 OK\r\n" +
            "Content-Type: text/html\r\n" +
            "\r\n\r\n" +
            "<html>ID Number: {0}<br>" +
            "DateTime.Now: {1}<br>" +
            "Requested URL: {2}</html>";

        // Timeout constants
        private const int DEFAULT_NETWORK_READ_TIMEOUT = 1000;
        private const int DOUBLE_CRLF_TIMEOUT = 10;
        private const int FIRST_CRLF_DATA_TIMEOUT = 2048;
        private const int DOUBLE_CRLF_DATA_TIMEOUT = 100 * 1024;


        // Regex, const and format strings for html
        private const string URL_REGEX_PATTERN = @"(\/.*).*";
        private const string HTTP_VERSION = "HTTP/1.1";
        private const string HEADER_REGEX_PATTERN = @"(.*):(.*)";
        private static string[] GET_REQUEST_STRING = new string[] { "GET", "PUT" };


        // Thread stuff
        private static Thread listenerThread;
        private volatile static TcpListener Listener;
        private static List<WebService> services = new List<WebService>();
        private volatile static int processCount = 0;
        private static volatile bool stopped = false;
        private static SimpleLockThreadPool threadPool;

        public static bool Start(int port, int nThreads = 64)
        {
            threadPool = new SimpleLockThreadPool(nThreads);
            
            Listener = new TcpListener(System.Net.IPAddress.Any, port);
            
            listenerThread = new Thread(ListenProc);
        



			Listener.Start();
            listenerThread.Start();

            return true;
        }

        private static void ListenProc()
        {
            try
            {

                while (true)
                {
                    if (Listener == null)
                    {
                        Console.WriteLine("NULLLLLL");
                        break;
                    }
                    TcpClient client = Listener.AcceptTcpClient();
                    client.NoDelay = true;
                    client.Client.NoDelay = true;

                    lock (threadPool)
                    {
                        threadPool.QueueUserWorkItem(ThreadWork, client);
                    }

                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }

        public static void Stop()
        {
            Terminate();
        }


        private static WebRequest BuildRequest(TcpClient client)
        {
            WebRequest newWebRequest = new WebRequest(client.GetStream());
            ConcatStream bodyStream = null;
            byte[] bufferedRequest = new byte[5000];
            int offset = 0;
            bool done = false;
            int length = -1;
            int bytesRead = 0;

            while (!done)
            {

                NetworkStream networkStream = client.GetStream();
                lock (networkStream)
                {

                    networkStream.ReadTimeout = DEFAULT_NETWORK_READ_TIMEOUT;
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    while (true)
                    {
                        // wait till client is available
                        try
                        {

                            if (networkStream.DataAvailable)
                            {
                                // Console.WriteLine("client avail");
                                int i = networkStream.Read(bufferedRequest, offset, bufferedRequest.Length - offset);
                                bytesRead += i;
                                offset += i;
                            }
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine("TimeoutException");

                            client.Close();
                            watch.Stop();

                            return null;
                        }

                        if (bytesRead > 0)
                        {
                            // bufferedRequest.AddRange(buf);
                            string request = System.Text.UnicodeEncoding.UTF8.GetString(bufferedRequest);

                            // check single crlf timeout
                            if (bufferedRequest.Count() > FIRST_CRLF_DATA_TIMEOUT
                                && !request.Contains(CRLF))
                            {
                                //time out!!
                                // close socket and return
                                client.Close();
                                return null;
                            }

                            //check timeout #2
                            var elapsedSeconds = watch.ElapsedMilliseconds / 1000.0;

                            // check for double crlf timeouts
                            if (elapsedSeconds >= DOUBLE_CRLF_TIMEOUT
                                || bufferedRequest.Count() > DOUBLE_CRLF_DATA_TIMEOUT)
                            {
                                if (!request.Contains(DOUBLE_CRLF))
                                {
                                    if (elapsedSeconds >= DOUBLE_CRLF_TIMEOUT)
                                        Console.WriteLine("Timing out: Double CRLF not found in {0} seconds",
                                            elapsedSeconds);
                                    else
                                        Console.WriteLine("Timing out: Double CRLF not found in first {0} bytes",
                                            bufferedRequest.Count());

                                    return null;
                                }

                            }

                            if (bufferedRequest.Count() != 0)
                            {
                                if (!isValidRequest(bufferedRequest))
                                {
                                    client.Close();
                                    //listener.Stop ();
                                    watch.Stop();
                                    return null;
                                }

                                if (request.Length >= 4 && request.Contains(DOUBLE_CRLF))
                                {
                                    done = true;
                                    break;
                                }
                            }
                        }
                    }

                    var reqString = System.Text.ASCIIEncoding.UTF8.GetString(bufferedRequest);

                    if (length == -1 && reqString.Split(new String[] { CRLF },
                        StringSplitOptions.None).Length > 2)
                    {
                        //check for content length
                        var headers = parseHeaders(reqString);
                        if (headers.ContainsKey("Content-Length"))
                        {
                            length = int.Parse(headers["Content-Length"]);
                        }
                    }

                    if (done)
                    {
                        if (bufferedRequest.Count() <= 0)
                            return null;
                        // Array.Re
                        // Copy to new buffer 
                        byte[] requestBytes = new byte[bytesRead];
                        // Buffer.BlockCopy(bufferedRequest, 0, requestBytes, 0, bytesRead);
                        Array.Resize(ref bufferedRequest, bytesRead);

                        // find part of body that has been read already
                        string reqStr = System.Text.ASCIIEncoding.UTF8.GetString(bufferedRequest);
                        int index = reqStr.IndexOf(DOUBLE_CRLF);
                        var httpHeaderString = reqStr.Substring(0, index + DOUBLE_CRLF.Count());
                        var httpHeaderBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(httpHeaderString);
                        var alreadyBytes = new byte[bufferedRequest.ToArray().Length - httpHeaderBytes.Length];

                        Buffer.BlockCopy(bufferedRequest, httpHeaderBytes.Length, alreadyBytes, 0, alreadyBytes.Length);

                        MemoryStream already = new MemoryStream(alreadyBytes);
                        if (length != -1)
                            bodyStream = new ConcatStream(already, networkStream, length);
                        else
                            bodyStream = new ConcatStream(already, networkStream);

                    }
                }
            }

            string requestString = System.Text.ASCIIEncoding.UTF8.GetString(bufferedRequest);
            // request has been buffered, now build it
            newWebRequest.Body = bodyStream;

            string[] firstLine = requestString.Split(CRLF.ToCharArray())[0].Split(' ');

            newWebRequest.Method = firstLine[0];
            newWebRequest.MethodArguments = firstLine[1];
            newWebRequest.HTTPVersion = firstLine[2];
            newWebRequest.RequestTarget = System.Uri.UnescapeDataString(firstLine[1]); ;
            newWebRequest.Headers = parseHeaders(requestString);

            return newWebRequest;
        }

        static ConcurrentDictionary<string, string> parseHeaders(string request)
        {
            String[] requestLines = request.Split(new String[] { CRLF },
                StringSplitOptions.None);

            ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();

            //Console.WriteLine ("Searching for headers");
            for (int i = 1; i < requestLines.Length; i++)
            {
                if (requestLines[i] != "")
                {
                    Regex r = new Regex(HEADER_REGEX_PATTERN);
                    Match match = r.Match(requestLines[i]);
                    string header = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
					headers[header] = value;
                }
            }

            return headers;
        }

        static void Terminate()
        {
            listenerThread.Abort();

            if (Listener != null)
                Listener.Stop();

            Listener = null;
            threadPool.Dispose();
            Thread.CurrentThread.Abort();
        }

        static void ThreadWork(object clientObj)
        {
            TcpClient client = clientObj as TcpClient;
            while (client.Connected)
            {
                WebRequest request = BuildRequest(client);
                processCount++;

                if (request == null)
                {
                    client.Close();
                }
                else
                {
                    var handlerService = fetchHandler(request);

                    if (handlerService == null)
                        request.WriteNotFoundResponse("not found");
                    else
                        handlerService.Handler(request);
                }
            }

            client.Close();
        }


        public static void AddService(WebService service)
        {
            services.Add(service);
        }

        private static WebService fetchHandler(WebRequest request)
        {
            foreach (var service in services)
            {
                if (request.RequestTarget != null && request.RequestTarget.StartsWith(service.ServiceURI))
                    return service;
            }
            return null;
        }

        private static bool isValidRequest(byte[] requestBytes)
        {
            String request = System.Text.ASCIIEncoding.ASCII.GetString(requestBytes);
            request = request.Trim();
            String[] requestLines = request.Split(new String[] { CRLF },
                                                StringSplitOptions.None);

            // process first line for request
            if (requestLines.Length >= 1)
            {
                if (!processFirstLine(requestLines[0]))
                    return false;
            }

            // process headers
            if (requestLines.Length >= 2)
            {
                for (int i = 1; i < requestLines.Length; i++)
                {
                    if (requestLines[i] != "")
                    {
                        Regex r = new Regex(HEADER_REGEX_PATTERN);
                        if ((!r.IsMatch(requestLines[i])) &&
                            (i < requestLines.Length - 1
                                && requestLines[i + 1] == ""))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private static bool isValidMethod(String str)
        {
            foreach (string method in GET_REQUEST_STRING)
            {
                if (method.Contains(str))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool processFirstLine(String line)
        {

            String[] tokens = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.None);
            // Console.WriteLine(tokens[0]);
            // Console.WriteLine("token length: {0}", tokens.Length);
            if (tokens.Length > 3)
            {
                return false;
            }
            if (isValidMethod(tokens[0]))
            {
                // Console.WriteLine("tokens[0].Length: {0}", tokens[0].Length);
                // Console.WriteLine(tokens[0]);
                // Console.WriteLine("!GET_REQUEST_STRING.Contains (tokens [0])");
                return false;
            }
            if (tokens.Length == 2)
            {
                Regex r = new Regex(URL_REGEX_PATTERN);

                if (!r.IsMatch(tokens[1]))
                {
                    // Console.WriteLine("tokens.Length == 2 && !r.IsMatch (tokens [1])");
                    return false;
                }
            }
            if (tokens.Length == 3)
            {
                if (!HTTP_VERSION.Contains(tokens[2]) && !tokens[2].Contains(HTTP_VERSION))
                {
                    return false;
                }

            }
            return true;
        }
    }
}


