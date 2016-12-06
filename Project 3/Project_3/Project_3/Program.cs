using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Project_3
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			TcpListener server = null;
			Proxy proxy_variable = new Proxy ();

			try{
				Int32 port = 13000;
				IPAddress localAdr = IPAddress.Parse("127.0.0.1");

				proxy_variable.Start();

			}
			catch{
			
			}

		}
	}
}
