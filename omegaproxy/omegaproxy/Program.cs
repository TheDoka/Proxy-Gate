using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace omegaproxy
{
	class Program
	{
		static string remote_port;
		static int v_mod = 0;
		static bool en_thd = false;
		static bool on_error = false;
		static bool accept_blank = false;
		static bool crash_restart = false;
		static List<int> range = new List<int>();
		static List<Thread> threads_list = new List<Thread>();
		static List<Socket> socket_list = new List<Socket>();
		
		//LISTE POUR SERVEUR SERVEUR
		static List<TcpClient> TO_SERVER = new List<TcpClient>();
		static List<NetworkStream> newtw = new List<NetworkStream>();
		
		//static Socket clientSocket;
		
		public static void Main(string[] args)
		{			

			//byte[] bytes = { 0,1 };
			//forward_to(bytes);
			//Console.WriteLine("x");
			//Console.ReadKey();
			
			string ip = "127.0.0.1";
			 
			for (Int32 i = 3700; i < 3720; i++) {
				Console.Title = ("Initialization...");
				try {
				en_thd = true;
				Thread myThread = new Thread(() => clientconnection(ip,i));
				myThread.Name = ("T"+i);
				threads_list.Add(myThread);
				myThread.Start();
			
				} catch (Exception ex) {Console.WriteLine(ex.ToString());}
				System.Threading.Thread.Sleep(50);
			}	
			
			Console.Write("#");
			if (on_error == true) {Console.Write("\n" +"Press Any key to continue."); Console.ReadKey();}
			
			
			LoadUI();
			string[] commands = {"","help","n","new","a","attach","s","send","s_all","r","restart","set", "set lport","set lv","set accept_blank","set crash_restart", "set c_r","i","info","time","c","clear","t","time","p","ping","q","quit"};

			while(true)
			{
				Console.Title = ("Running " + range.Count  + " proxies...");
				try {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("$ ");
			Console.ResetColor();
			
				string choice = Console.ReadLine();
				
				if (choice.Split(' ')[0].Equals("set") && choice.Length <= 3) {Console.WriteLine("'{0}' arguement incomplet",choice);}
				if (!commands.Any(choice.Split(' ')[0].Equals)) {Console.WriteLine("'{0}' n'est pas reconnu en tant que commande interne",choice);}

				    
				
				//							COMMANDS

				if (choice.StartsWith("n ") || choice.StartsWith("new ")) {
					string[] x=choice.Split(' ');
					if (!range.Contains(Int32.Parse(x[2]))) {
						if (x[1].Length<1) {x[1] = "0";}
					Thread myThread = new Thread(() => clientconnection(x[1],Int32.Parse(x[2])));
					myThread.Start();
					Console.WriteLine("[*] New proxy succesfully opened at : {0}:{1}",x[1],Int32.Parse(x[2]));
					LoadUI();
					    } else {Console.WriteLine("=> Proxy {0} is already running.",x[2]);}
					}
				
				if (choice.StartsWith("a ") || choice.StartsWith("attach ")) {
					string[] x=choice.Split(' ');
					clientconnection(x[1],Int32.Parse(x[2]));
					Console.WriteLine("[*] New proxy succesfully opened at : {0}:{1}",x[1],Int32.Parse(x[2]));
					}
				
				
				if (range.Count > 0 && en_thd == true) {
					
				if (choice.StartsWith("send")) { 
					foreach (Socket S_ in socket_list) {
						if (remote_port == ((IPEndPoint)S_.LocalEndPoint).Port.ToString ()) {S_.Send(System.Text.Encoding.UTF8.GetBytes(choice.Remove(0,5)));}
					}
				}
				if (choice.StartsWith("s ")) { 
					foreach (Socket S_ in socket_list) {
							if (remote_port == ((IPEndPoint)S_.LocalEndPoint).Port.ToString () && en_thd == true) {S_.Send(System.Text.Encoding.UTF8.GetBytes(choice.Remove(0,2)));} else {Console.WriteLine("No one at {0}.",remote_port);}
					}
				}
				
					if (choice.StartsWith("s_all ") && choice.Contains("s_all ") && en_thd == true) {
				foreach (Socket S_ in socket_list) {
				S_.Send(System.Text.Encoding.UTF8.GetBytes(choice.Remove(0,6)));
					}
				}	
					
				if (choice=="r" || choice=="restart") {
						en_thd = false;
						
						foreach (Socket S_ in socket_list){
							Console.WriteLine("Shutdown {0}",S_.Connected);
							S_.Close();
						}
					foreach (Thread thread in threads_list) {
						range.Remove(int.Parse(thread.Name.Remove(0,1)));
						thread.Abort();
						Console.WriteLine("Closing {0}",thread.Name);
					}
						
					en_thd = true;	
					LoadUI();
					Console.WriteLine();
				}	
				
					
				}

				if (choice.StartsWith("set crash_restart ") || choice.StartsWith("set c_r ")) {
					string[] x= choice.Split(' ');
					switch (int.Parse(x[2])){
						case 0:
								crash_restart = false;
								Console.WriteLine("=> CR=False");
								break;
							case 1:
								crash_restart = true;
								Console.WriteLine("=> CR=True");
								break;
					}
				}
				
				if (choice.StartsWith("set tport ")) {
					string[] x= choice.Split(' ');
					remote_port = x[2].ToString();
					Console.WriteLine("=> TPORT={0}",x[2]);
				}
				if (choice.StartsWith("set lv ")){string[] x= choice.Split(' '); v_mod = int.Parse(x[2]);}
				if (choice.StartsWith("set accept_blank ")) {string[] x= choice.Split(' '); 
					switch (int.Parse(x[2])){
					case 0:
						accept_blank=false;
						break;
					case 1:
						accept_blank=true;
						break;
					}
				}

				if (choice=="i" || choice=="info") {Console.WriteLine("Threads|State"); foreach (Thread thread in threads_list) {Console.WriteLine("{0}({1})",thread.Name,thread.ThreadState);}}
				if (choice=="t" || choice=="time") {Console.WriteLine("=> " + DateTime.Now.ToLongTimeString());}
				if (choice=="c" || choice=="clear") {Console.Clear();}
				if (choice.StartsWith("p") || choice.StartsWith("ping")) {ping(choice.Split(' ')[1]);}
				if (choice=="q" || choice=="quit")  {Environment.Exit(0);}					
				if (choice.StartsWith("help")) {
					Console.WriteLine(@"
			      Commands			       Usage 
			help			|	Display commands usage.
			n, new    {ip,port}	|	Create new proxy.
			a, attach {ip,port}	|	Create a single thread proxy.
			s, send   {message}	|	Send message to the current talking port.
			s_all	  {message}	|	Send a message to all running proxies.
			set tport {port}	|	Set the current talking port.
			set lv	  {0,1,2,3}	| 	Change the hex format of response.
			set accept_blank {0,1}	|	Display or not \n response.
			set Crash_restart {0,1} |	Automatically restart crashed proxies.
			p, ping	  {ip}		|	Regular ping.
			t, time			|	Show time.
			i, info			|	Display current running proxies.
			c, clear		|	Clear the console output.
			r, restart		|	Stop all proxies, without quitting main.
			q, quit			|	Stop and exit all threads.
					");
				}
				
				} catch (Exception ex) {Console.WriteLine("Err:" + ex.Message + "\n");}
			}
			
	}
		public static void LoadUI()
		{
			Console.Clear();
			Console.WriteLine("Listening on ports : ");
			foreach (var i in range) {Console.Write(i + ", ");}
			Console.WriteLine();
		}
		
		private static void clientconnection(string ip, int port)
			{

			Socket listenr = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint ipEnd = new IPEndPoint(IPAddress.Parse(ip),port);
			try {
				listenr.Bind(ipEnd);
				Console.Write("=");
				range.Add(port);
			} catch (Exception err) {Console.WriteLine("\n" + "[!] Can't open {0}, Err: {1}",port, err.Message); on_error = true; return;}
				
				listenr.Listen(0);
				
				Socket clientSocket;
				clientSocket = listenr.Accept();
				
				socket_list.Add(clientSocket);
				
				if (clientSocket.Connected == true) {
					Console.WriteLine("Connection at port {0} accepted, from port {1}. Talking port set to {0}",port, ((IPEndPoint)clientSocket.RemoteEndPoint).Port);
					remote_port = port.ToString();
				Console.WriteLine();
				
				int readByte;
				byte[] buffer = new byte[clientSocket.SendBufferSize];
				
				
				try {	
					
				do
				{
		
					readByte = clientSocket.Receive(buffer);
					byte[] rData = new Byte[readByte];
					Array.Copy(buffer, rData, readByte);

					if (BitConverter.ToString(rData) != "0D-0A" && accept_blank !=true) {
						Console.WriteLine("[{0}] {1} \n	=> \n {2}", port, ByteArrayToString(rData).ToString().ToUpper(), System.Text.Encoding.UTF8.GetString(rData));
					} else {
						if (accept_blank==true){
							Console.WriteLine("[{0}] {1} \n	=> \n {2}", port, ByteArrayToString(rData).ToString().ToUpper(), System.Text.Encoding.UTF8.GetString(rData));
                        }
				}
					

					
					//FORWARD TO LOGON.WARMANE.COM AND SEND RESPONSE TO GAME; GAME --> LOGON; LOGON --> GAME
					//RDATA --> WARMANE
					//WARMANE RESPONSE --> GAME, CLIENT
					if (TO_SERVER.Count == 0) {
					Thread myThread = new Thread(delegate(){
					                             	
							TcpClient tcpClient = new TcpClient("logon.warmane.com",3724);
							TO_SERVER.Add(tcpClient);
     						NetworkStream networkStream = tcpClient.GetStream();
     						newtw.Add(networkStream);
     						
					});
					                             
								myThread.Name = "TO_SERVER";
								myThread.Start();
								threads_list.Add(myThread);
					    }
					

					
					
					
				} while (readByte > 0 && clientSocket.Connected == true);
						
					} catch (Exception err) {
				
					clientSocket.Close();
					listenr.Close();
					range.Remove(port);
					socket_list.Remove(clientSocket);
					if (crash_restart == true) {Thread myThread = new Thread(() => clientconnection(ip,port)); myThread.Start();}
					LoadUI();
					Console.WriteLine("[*] Crap, {0} crashed, cmon!",port);
					return;
					}
					
					clientSocket.Close();
					listenr.Close();
					range.Remove(port);
					LoadUI(); 
					Console.WriteLine("[*] Crap, we lost {0}, cmon!",port);
					socket_list.Remove(clientSocket);									
					return;
					
			}; // IF NOT CONNECTED
				
				
			}
		
		
		
		
		public static string ByteArrayToString(byte[] ba)
{
  StringBuilder hex = new StringBuilder(ba.Length * 2);
  foreach (byte b in ba)
  	
  	switch (v_mod)
  {
  case 0:
  		hex.AppendFormat("{0:x2} ", b);
  	break;
  case 1:
  		hex.AppendFormat("{0:x2}", b);
  break;
 case 2:
  	hex.AppendFormat("0x" + "{0:x2} ", b);
  break;
 case 3:
  	hex.AppendFormat("x{0:x2} ", b);
  break;	    
  }
  	
   
    
  return hex.ToString();
}
		
		public static void ping(string ip)
		{
			
 	Process process = new Process();
    process.StartInfo.FileName = "cmd";
    process.StartInfo.Arguments = "/c ping " + ip;
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.RedirectStandardError = true;
    var proc = new System.Diagnostics.Process();
            proc.OutputDataReceived += (s,e) => { Console.WriteLine(e.Data);};
            proc.StartInfo = process.StartInfo;
            proc.Start();
            proc.BeginOutputReadLine();
            	proc.WaitForExit();

}

		}
		

}
