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
		SimpleLockThreadPool threadPool = new SimpleLockThreadPool(64, true);

		public Proxy ()
		{
			
		}

		public void handler()
		{
			Console.WriteLine ("accepted");
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

				threadPool.QueueUserWorkItem(Handler.Handle, sock);
			}
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

