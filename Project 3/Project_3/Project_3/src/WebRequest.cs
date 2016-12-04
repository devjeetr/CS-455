using System;
//using System.Net.Sockets;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace CS422
{
    public class WebRequest
    {
        public Stream Body
        {
            get;
            set;
        }
        public String MethodArguments
        {
            get;
            set;
        }

        public ConcurrentDictionary<string, string> Headers
        {
            get;
            set;
        }

        public string Method
        {
            get;
            set;
        }

        public string RequestTarget
        {
            get;
            set;
        }

        public string HTTPVersion
        {
            get;
            set;
        }

        public int bodyOffset
        {
            get;
            set;
        }

		public NetworkStream NetworkStream { 
			get
			{
				return networkStream;
			}

			set {
				networkStream = value;
			}
		}

        private NetworkStream networkStream;
        private const string NOT_FOUND_STATUS = "404 Not Found";
        private const string OK_STATUS = "200 OK";
        private const string CONTENT_LENGTH_HEADER = "Content-Length";
        private const string CONTENT_TYPE_HEADER = "Content-Type";
        private const string CONTENT_TYPE_VALUE = "text/html";
        private const string STATUS_LINE_FORMAT = "{0} {1}\r\n";
        private const string HEADER_FORMAT = "{0}:{1}";
		private const string CRLF = "\r\n";

        private const string RESPONSE_FORMAT = "{0}{1}\r\n\r\n{2}";
		private const string BODYLESS_RESPONSE_FORMAT = "{0}{1}\r\n\r\n";
        private const string HEADLESS_RESPONSE_FORMAT = "{0}\r\n\r\n{1}";


        public WebRequest(NetworkStream stream)
        {
            networkStream = stream;
        }

        public void WriteNotFoundResponse(string pageHTML)
        {
            WriteResponse(NOT_FOUND_STATUS, pageHTML);
        }


        public bool WriteHTMLResponse(string htmlString)
        {
            return WriteResponse(OK_STATUS, htmlString);
        }

		public string getHeaderString(int contentLength)
		{
			string headers = "";

			// Add Content-Type and Content-Length headers
			// if not already present
			if (!Headers.Keys.Contains(CONTENT_TYPE_HEADER))
				Headers[CONTENT_TYPE_HEADER] = CONTENT_TYPE_VALUE;

			if (!Headers.Keys.Contains(CONTENT_LENGTH_HEADER))
				Headers[CONTENT_LENGTH_HEADER] = String.Format("{0}", contentLength);

			var headerKeys = Headers.Keys;

			for (int i = 0; i < headerKeys.Count() - 1; i++)
			{
				if (Headers[headerKeys.ElementAt(i)].Length > 0)
					headers += String.Format(HEADER_FORMAT, headerKeys.ElementAt(i), Headers[headerKeys.ElementAt(i)])
					                 + CRLF;
			}

			if (headerKeys.Count() > 0)
			{
				if (headerKeys.ElementAt(headerKeys.Count() - 1) != "")
					headers += String.Format(HEADER_FORMAT, headerKeys.ElementAt(headerKeys.Count() - 1), Headers[headerKeys.ElementAt(headerKeys.Count() - 1)]);
			}

			return headers;
		}

		public string getResponseString(string status, string headers, string html) { 
			string statusLine = String.Format(STATUS_LINE_FORMAT, HTTPVersion, status);
				
			if (html.Length == 0)
				return String.Format(BODYLESS_RESPONSE_FORMAT, statusLine, headers);
			else
				return String.Format(RESPONSE_FORMAT, statusLine, headers, html);
		}

        public bool WriteResponse(string status, string html)
        {
			var headers = getHeaderString(System.Text.Encoding.Unicode.GetBytes(html).Length);

			var responseString = getResponseString(status, headers, html);
			byte[] response = System.Text.Encoding.ASCII.GetBytes(responseString);

			networkStream.Write(response, 0, response.Length);
            
			return true;
        }


        public bool WriteResponseNoStatus(string html)
        {
            var headers = getHeaderString(System.Text.Encoding.Unicode.GetBytes(html).Length);

			byte[] response = System.Text.Encoding.ASCII.GetBytes(String.Format(HEADLESS_RESPONSE_FORMAT, headers, html));
            networkStream.Write(response, 0, response.Length);

            return true;
        }

        

        public void Print()
        {
            string statusLine = String.Format("{0} {1} {2}", Method, RequestTarget, HTTPVersion);

            var headerKeys = Headers.Keys;
            if (headerKeys.Count() == 0)
                return;
            string headers = "";

            for (int i = 0; i < headerKeys.Count(); i++)
            {
				headers += String.Format(HEADER_FORMAT, headerKeys.ElementAt(i), Headers[headerKeys.ElementAt(i)]) + CRLF;
            }

            //Console.WriteLine(String.Format ("{0}\n{1}", statusLine, headers));
        }

    }
}

