using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

//This class creates TcpListener
namespace Project_3
{
	public class Proxy
	{
		const int BACKLOG = 100;
		const int portnumber = 8080;
		string header = null;
		public Proxy ()
		{
			
		}
		public void handler()
		{
			Console.WriteLine ("accepted");
			Console.WriteLine (header);

		}
		public void Start()
		{


			Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPAddress ipAddress = IPAddress.Loopback;
			IPEndPoint localEndPoint = new IPEndPoint (ipAddress, portnumber);

			listener.Bind(localEndPoint);
			listener.Listen(BACKLOG);
			loop_for_ever(listener);

		}
		public void loop_for_ever( Socket listener)
		{
			//accept the client
			while (true) {
				Socket sock = listener.Accept();
				//Handler rh = new Handler();

				header = GetHeader(sock);
				Thread requestThread = new Thread(new ThreadStart(handler));

				requestThread.Start ();
			}


		}
		public string GetHeader(Socket socket)
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
		public void SendRequest(Socket socket, string header)
		{
			byte[] bytesSent = System.Text.Encoding.ASCII.GetBytes(header);
			socket.Send(bytesSent, bytesSent.Length, SocketFlags.None);
		}
		public string get_header()
		{
			return header;
		}


	}
}

