using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace omegaproxy
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		public MainForm()
		{

			InitializeComponent();
			
			
			IPAddress ip = Dns.GetHostEntry("localhost").AddressList[0];
			TcpListener server = new TcpListener(ip, 8080);
			TcpClient client = default(TcpClient);
			
			try {
				
				server.Start();
				Console.WriteLine("Server started...");
				
			} catch (Exception) {
				
				throw;
			}
			
			
		}
	}
}
