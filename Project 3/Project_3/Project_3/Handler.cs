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
using System.Net;

namespace Project_3
{
	class Handler {

		private static string CRLF = "\r\n";
		private static string DOUBLE_CRLF = "\r\n\r\n";
		private static string DOUBLE_NEWLINE = "\n\n";
		private const string HEADER_REGEX_PATTERN = @"(.*):(.*)";

		public Handler() 
		{ 
		
		}

		public static void Handle(object socket) {
			Socket connectedSocket = socket as Socket;
			byte[] header = GetHttpHeader(connectedSocket);

			connect(connectedSocket, header);
		}

		public static byte[] GetHttpHeader(Socket socket)
		{
			List<byte> buffer = new List<byte>();
			while (true)
			{
				byte[] bytes = new byte[1];
				int bytesRec = socket.Receive(bytes);
				if (bytesRec <= 0)
					continue;

				buffer.AddRange(bytes);
				
				string header = System.Text.Encoding.ASCII.GetString(buffer.ToArray(), 0, buffer.ToArray().Length);

				if (header.IndexOf(DOUBLE_CRLF, StringComparison.Ordinal) > -1
				    || header.IndexOf(DOUBLE_NEWLINE, StringComparison.Ordinal) > -1)
				{
					break;
				}
			}

			Console.WriteLine("Received header: ");
			Console.WriteLine(System.Text.Encoding.ASCII.GetString(buffer.ToArray()));
			return buffer.ToArray();
		}

		static ConcurrentDictionary<string, string> parseHeaders(string request)
		{
			String[] requestLines = request.Split(new String[] { CRLF },
				StringSplitOptions.None);

			ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();

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

		public static void connect(Socket serverSocket, byte[] bytes) {

			string header = System.Text.Encoding.UTF8.GetString(bytes);

			Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			//Console.WriteLine("Header recieved: {0}", header);

			var parsedHeaders = parseHeaders(header);
			//Console.WriteLine(header);
			if (!parsedHeaders.ContainsKey("Host")) {
				clientSocket.Close();
				serverSocket.Close();

				return;
			}

			var url = parsedHeaders["Host"].Split('.');

			var domain = parsedHeaders["Host"].Trim();
			IPAddress[] addresslist;

			try
			{

				addresslist = Dns.GetHostAddresses(domain);
				//Console.WriteLine("Host: |{0}|", parsedHeaders["Host"].Trim());
				//addresslist = Dns.GetHostAddresses(parsedHeaders["Host"].Trim());

			} catch (Exception){
				Console.WriteLine(header);
				Console.WriteLine("Exception resolving host address: {0}",
				                 domain);

				clientSocket.Close();
				serverSocket.Close();

				return;
			}

			IPEndPoint remoteEP = new IPEndPoint(addresslist[0], 80);
			clientSocket.Connect(remoteEP);

			// replace proxy keep alive
			if (parsedHeaders.ContainsKey("Proxy-Connection"))
			{
				string existingHeader = System.Text.Encoding.UTF8.GetString(bytes);
				//Console.WriteLine("Replacing proxy-connection, with value: {0}",
				//				  parsedHeaders["Proxy-Connection"]);

				parsedHeaders.TryAdd("\nConnection", parsedHeaders["Proxy-Connection"]);

				string connection = String.Format("Connection: {0}", parsedHeaders["Proxy-Connection"]);
				var newHeader = existingHeader.Substring(0, existingHeader.Length - DOUBLE_CRLF.Length) 
				                              + connection + DOUBLE_CRLF;
				
				//Send Client Request to the real server
				clientSocket.Send(System.Text.Encoding.UTF8.GetBytes(newHeader));
				Console.WriteLine("Replacing");
				Console.WriteLine("Sending:\n{0}", newHeader);
			}
			else { 
				Console.WriteLine("Sending:\n{0}", System.Text.Encoding.UTF8.GetString(bytes));
				clientSocket.Send(bytes);
			}

			//clientSocket.Send(bytes);

			//Receive Response from the real server through client socket
			byte[] clientResponse = GetHttpHeader(clientSocket);
			header = System.Text.Encoding.UTF8.GetString(clientResponse);

			var clientHeadersParsed = parseHeaders(header);
			SendRequest(serverSocket, header);

			if (clientHeadersParsed.ContainsKey("Content-Length"))
			{
				var contentLength = int.Parse(clientHeadersParsed["Content-Length"]);
				ProcessWithContentLength(serverSocket, clientSocket, header, contentLength);
			}
			else if (clientHeadersParsed.ContainsKey("Transfer-Encoding") && 
			        clientHeadersParsed["Transfer-Encoding"].Contains("chunked"))
			{
				//Console.WriteLine("Receiving chunks");
				RecieveChunks(serverSocket, clientSocket);

			} else {
				
				clientSocket.Close();
				serverSocket.Close();
				return;
			}

			serverSocket.Close();
			clientSocket.Close();

		}

		public static void ProcessWithTransferEncoding(Socket serverSocket, Socket clientSocket,
													string clientHeader) { 
			
		
		}

		private static string getAndSendNextChunk(Socket recieveSocket, int chunkSize, Socket sendSocket) { 
			string header = "";
			List<byte> buffer = new List<byte>();
			//Console.WriteLine("Receiving Chunk of size {0}", chunkSize);
			chunkSize += CRLF.Length;

			while (chunkSize > 0)
			{
				byte[] bytes = new byte[1];
				int bytesRec = recieveSocket.Receive(bytes);

				if (bytesRec > 0)
					buffer.AddRange(bytes);
				
				header += System.Text.Encoding.UTF8.GetString(bytes, 0, bytesRec);

				chunkSize--;
			}

			sendSocket.Send(buffer.ToArray());

			return header;
		}

		private static int GetAndSendChunkSize(Socket recieveSocket, Socket sendSocket) { 
			string header = "";
			List<byte> buffer = new List<byte>();
			while (true)
			{
				byte[] bytes = new byte[1];
				int bytesRec = recieveSocket.Receive(bytes);
				header += System.Text.Encoding.UTF8.GetString(bytes, 0, bytesRec);

				if (bytesRec > 0)
				{
					buffer.AddRange(bytes);
				
					
				}
				
				//Console.WriteLine("recving chunk size");

				if (header.IndexOf(CRLF, StringComparison.Ordinal) > -1 
				    || header.IndexOf(DOUBLE_NEWLINE, StringComparison.Ordinal) > -1)
				{
					break;
				}
			}
			//Console.WriteLine("Header: {0}", header);
			int num = Int32.Parse(header, System.Globalization.NumberStyles.HexNumber);

			sendSocket.Send(buffer.ToArray());

			return num;
		}

		private static byte[] RecieveChunks(Socket serverSocket, Socket clientSocket) {

			string data = "";

			int size = GetAndSendChunkSize(clientSocket, serverSocket);
			var chunk = getAndSendNextChunk(clientSocket, size, serverSocket);
			data += chunk;

			while (chunk.Length > 0) {
				try
				{
					size = GetAndSendChunkSize(clientSocket, serverSocket);
					if (size == 0)
						break;
				}catch (FormatException){
					Console.WriteLine("Format exception");
					break;
				}

				chunk = getAndSendNextChunk(clientSocket, size, serverSocket);

				data += chunk;
			}

			return System.Text.Encoding.ASCII.GetBytes(data);
		}

		public static void ProcessWithContentLength(Socket serverSocket, Socket clientSocket, 
		                                            string clientHeader, int contentLength) { 
		
			//Send the real server response to the client through server socket
			int receivedBytes = 0;
			const int BUFSZ = 1024;
			byte[] buffer = new byte[BUFSZ];


			//Console.WriteLine("ClientHeader:\n{0}", clientHeader);

			try
			{
				receivedBytes = clientSocket.Receive(buffer);
				contentLength -= receivedBytes;
				serverSocket.Send(buffer, receivedBytes, SocketFlags.None);

				//Console.WriteLine("Bytes recieved: {0}", receivedBytes);

				while (receivedBytes > 0 && contentLength > 0)
				{
					receivedBytes = clientSocket.Receive(buffer);
					contentLength -= receivedBytes;
					serverSocket.Send(buffer, receivedBytes, SocketFlags.None);
				}

			}catch (SocketException){
				Console.WriteLine("Exception during socket.send");
			}
		}


		public static void SendRequest(Socket socket, string header)
		{
			byte[] bytesSent = System.Text.Encoding.ASCII.GetBytes(header);
			socket.Send(bytesSent, bytesSent.Length, SocketFlags.None);
		}

		private void validateHTTPHeader() { 
			
		}



	}
}


