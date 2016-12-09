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
		public Handler() 
		{ 
		
		}


		public static void Handle(object socket) {
			Socket connectedSocket = socket as Socket;

			string header = GetHeader(connectedSocket);

			connect(connectedSocket, header);
		}

		public static string GetHeader(Socket socket)
		{
			string header = "";
			while (true)
			{
				byte[] bytes = new byte[1];
				int bytesRec = socket.Receive(bytes);
				header += System.Text.Encoding.ASCII.GetString(bytes, 0, bytesRec);
				if (header.IndexOf("\r\n\r\n") > -1 || header.IndexOf("\n\n") > -1)
				{
					break;
				}
			}
			return header;
		}

		public static void getTargetUrl(string header) { 
		
		}

		private static string CRLF = "\r\n";
		private const string HEADER_REGEX_PATTERN = @"(.*):(.*)";

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



		public static void connect(Socket serverSocket, string header) {
			// Console.WriteLine("Connecting to remote server");
			//Build C_Socket (client Socket)
			Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			var parsedHeaders = parseHeaders(header);
			//Console.WriteLine(header);
			if (!parsedHeaders.ContainsKey("Host")) {
				//Console.WriteLine("No host header");
				//Console.WriteLine(header);
				clientSocket.Close();
				serverSocket.Close();
				//serverSocket.Shutdown(SocketShutdown.Send);
				return;
			}
			var url = parsedHeaders["Host"].Split('.');

			var domain = String.Format("{0}.{1}", url[url.Length - 2], url[url.Length - 1]);
			IPAddress[] addresslist;

			try
			{
				addresslist = Dns.GetHostAddresses(domain);
			} catch (Exception){
				Console.WriteLine("Exception");
				clientSocket.Close();
				serverSocket.Close();
				return;
			}

			IPEndPoint remoteEP = new IPEndPoint(addresslist[0], 80);
			clientSocket.Connect(remoteEP);

			//Send Client Request to the real server
			SendRequest(clientSocket, header);

			//Receive Response from the real server through client socket
			header = GetHeader(clientSocket);

			var clientHeadersParsed = parseHeaders(header);
			SendRequest(serverSocket, header);

			if (clientHeadersParsed.ContainsKey("Content-Length"))
			{
				var contentLength = int.Parse(clientHeadersParsed["Content-Length"]);
				ProcessWithContentLength(serverSocket, clientSocket, header, contentLength);
			}
			else if (clientHeadersParsed.ContainsKey("Transfer-Encoding"))
			{

				RecieveChunks(serverSocket, clientSocket);
				Console.WriteLine("Receiving chunks");
			} else { 
				//Console.WriteLine("No hostcontent lenght");
				//Console.WriteLine(header);
				clientSocket.Close();
				serverSocket.Close();
				return;
			}

			//serverSocket.Shutdown(SocketShutdown.Both);
			serverSocket.Close();
			clientSocket.Close();
			// clientSocket.Shutdown(SocketShutdown.Both);
			Console.WriteLine("Ending");
		}

		public static void ProcessWithTransferEncoding(Socket serverSocket, Socket clientSocket,
													string clientHeader) { 
			
		
		}

		private static string getNextChunk(Socket socket, int chunkSize) { 
			string header = "";
			chunkSize += "\r\n".Length;
			while (chunkSize > 0)
			{
				byte[] bytes = new byte[1];
				int bytesRec = socket.Receive(bytes);
				header += System.Text.Encoding.UTF8.GetString(bytes, 0, bytesRec);
				if (header.IndexOf("\r\n") > -1 || header.IndexOf("\n\n") > -1)
				{
					break;
				}
				chunkSize--;
			}
			return header;
		}

		private static int GetChunkSize(Socket socket) { 
			string header = "";

			while (true)
			{
				Console.WriteLine("recving chunk size");

				byte[] bytes = new byte[1];
				int bytesRec = socket.Receive(bytes);

				header += System.Text.Encoding.UTF8.GetString(bytes, 0, bytesRec);

				Console.WriteLine(header);

				if (header.IndexOf("\r\n") > -1 || header.IndexOf("\n\n") > -1)
				{
					break;
				}
			}

			int num = Int32.Parse(header, System.Globalization.NumberStyles.HexNumber);
			Console.WriteLine("ChunkSize: {0}", num);


			return num;
		}

		private static byte[] RecieveChunks(Socket serverSocket, Socket clientSocket) {

			var data = "";

			int size = GetChunkSize(clientSocket);
			var chunk = getNextChunk(clientSocket, size);
			data += chunk;

			while (chunk.Length > 0) {
				try
				{
					size = GetChunkSize(clientSocket);
				}
				catch (FormatException e){
					Console.WriteLine("Format exception");
					break;
				}
				chunk = getNextChunk(clientSocket, size);
				serverSocket.Send(System.Text.Encoding.UTF8.GetBytes(chunk), size, SocketFlags.None);
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

			receivedBytes = clientSocket.Receive(buffer);
			contentLength -= receivedBytes;

			while (receivedBytes > 0 && contentLength > 0)
			{
				serverSocket.Send(buffer, receivedBytes, SocketFlags.None);
				receivedBytes = clientSocket.Receive(buffer);
				contentLength -= receivedBytes;
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


