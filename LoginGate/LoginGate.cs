using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shared;

namespace LoginGate
{
    class LoginGate
    {
        private string ClientHost = "127.0.0.1";   // 监听游戏客户端的地址
        private string ClientPort = "7000";
        private string SrvHost = "127.0.0.1";      // loginSrv的地址
        private string SrvPort = "5500";
        private Socket SrvSocket = null;        // 连接loginSrv的socket
        private ServerSeassion SrvSeassion = null;     // loginSrv的seassion
        //private string SrvMessageBuffer = "";         // 来自loginSrv的数据临时存储
        private bool SrvConnectState = false;       // 与loginSrv的连接状况，用于判断发送心跳包
        private int MaxListenLength = 10000;        // 最高挂起的侦听长度
        private byte[] data = new byte[4096];       // 接收Client数据的缓冲池
        private byte[] dataSrv = new byte[4096];    // 接收loginSrv数据的缓冲池
        private Dictionary<IntPtr, ClientSeassion> sockets = new Dictionary<IntPtr, ClientSeassion>();   // 已连接的用户池
        private LinkedList<ClientSeassion> sendClientQueue = new LinkedList<ClientSeassion>();        // 待发包客户端队列
        // 转发来自loginSrv的包
        private void SendServerPackThread()
        {
            while (true)
            {
                while (SrvSeassion != null)
                {
                    // 取出一条待发送包，并判断是否有效
                    string str = SrvSeassion.GetSendPack();
                    if (str == null)
                        break;
                    // 从中提取出handle
                    int index = str.IndexOf('/');
                    IntPtr handle = (IntPtr)int.Parse(str.Substring(1, index - 1));
                    // 从中提取出数据包
                    string pack = str.Substring(index + 1);
                    pack = pack.Substring(0, pack.Length - 1);      // 去除末尾的$
                    // 发送数据给相应客户端
                    sockets[handle].Send(pack);
                    Console.WriteLine("成功将" + pack + "转发给客户端");
                }
                Thread.Sleep(1);
            }
        }
        // 转发来自客户端的包
        private void SendClientPackThread()
        {
            while (true)
            {
                while (sendClientQueue.Count != 0)
                {
                    ClientSeassion client = sendClientQueue.First();
                    sendClientQueue.RemoveFirst();
                    while (true)
                    {
                        string pack = client.GetSendPack();
                        if (pack == null)
                        {
                            break;
                        }
                        SrvSocket.Send(Encoding.GetEncoding("GB2312").GetBytes(pack));
                        Console.WriteLine("成功将" + pack + "转发给loginSrv");
                    }
                }
                Thread.Sleep(1);
            }
        }
        // 连接loginSrv
        private void ConnectLoginSrv()
        {
            SrvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            int tryCount = 0;
            while (true)
            {
                try
                {
                    SrvSocket.Connect(new IPEndPoint(IPAddress.Parse(SrvHost), int.Parse(SrvPort)));
                    SrvConnectState = true;
                    Console.WriteLine("与loginSrv连接成功");
                    SrvSeassion = new ServerSeassion(SrvSocket);
                    // 连接成功，触发接收监听
                    SrvSocket.BeginReceive(dataSrv, 0, data.Length, SocketFlags.None, ReceiveLoginSrvCallBack, SrvSocket);
                    return;     // 成功建立连接直接返回
                }
                catch (Exception)
                {
                    tryCount++;
                    Console.WriteLine("与loginSrv连接失败，即将进行第" + tryCount + "次尝试");
                }
            }
            
        }
        private void ReceiveLoginSrvCallBack(IAsyncResult result)
        {
            Socket socket = result.AsyncState as Socket;
            int length = socket.EndReceive(result);
            string message = Encoding.GetEncoding("GB2312").GetString(dataSrv, 0, length);
            Console.WriteLine("从loginSrv收到" + message);
            SrvSeassion.ReceiveMessage(message);        // 服务器只有一个，不需要队列
            SrvSocket.BeginReceive(dataSrv, 0, data.Length, SocketFlags.None, ReceiveLoginSrvCallBack, socket);
        }
        // 保持与loginSrv心跳连接
        private void KeepAliveLoginSrv()
        {
            while (true)
            {
                if (SrvConnectState)
                {
                    // 每2s发送一个心跳包
                    byte[] buffer = Encoding.GetEncoding("GB2312").GetBytes("%--$");
                    SrvSocket.Send(buffer);
                    Console.WriteLine("成功发送一次心跳包%--$");
                }
                Thread.Sleep(2000);
            }
        }
        // 监听客户端连接
        private void AcceptThread()
        {
            // 新建服务端的socket，并绑定监听IP和端口，指定最大侦听队列长度
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ClientHost), int.Parse(ClientPort)));
            serverSocket.Listen(MaxListenLength);
            // 开始异步连接
            serverSocket.BeginAccept(AcceptCallBack, serverSocket);
        }
        private void AcceptCallBack(IAsyncResult result)
        {
            Socket serverSocket = result.AsyncState as Socket;
            Socket clientSocket = serverSocket.EndAccept(result);
            sockets.Add(clientSocket.Handle, new ClientSeassion(clientSocket));      // 将socket加入池中，以储存数据
            // 向loginSrv发送一条上线信息
            string remoteAddr = clientSocket.RemoteEndPoint.ToString().Substring(0, clientSocket.RemoteEndPoint.ToString().IndexOf(':'));   // 将端口去掉
            SrvSocket.Send(Encoding.GetEncoding("GB2312").GetBytes("%O" + clientSocket.Handle + "/" + remoteAddr + "/" + SrvHost + "$"));
            Console.WriteLine("发送了一条上线消息" + "%O" + clientSocket.Handle + "/" + remoteAddr + "/" + SrvHost + "$");
            // 开始侦听接收数据
            clientSocket.BeginReceive(data, 0, data.Length, SocketFlags.None, ReceiveCallBack, clientSocket);
        }
        private void ReceiveCallBack(IAsyncResult result)
        {
            Socket clientSocket = null;
            // 当客户端被暴力关掉，会导致服务端报警，使用try避免服务端挂掉
            try
            {
                // 将传入的clientSocket进行数据接收操作
                clientSocket = result.AsyncState as Socket;
                int length = clientSocket.EndReceive(result);
                // 如果客户端正常关闭，会向服务端发送长度为0的数据
                if (length == 0)
                {
                    clientSocket.Close();
                    sockets.Remove(clientSocket.Handle);   // 从字典移除
                    return;
                }
                // 将收到的数据转发给seassion处理
                string message = Encoding.GetEncoding("GB2312").GetString(data, 0, length);
                Console.WriteLine("从Client收到" + message);
                // 根据socket获取对应的UserSeassion，并将接收的数据传入
                if (sockets[clientSocket.Handle].ReceiveMessage(message))
                {
                    // 如果返回了true，说明已经组成了数据包，应加入发送队列
                    sendClientQueue.AddLast(sockets[clientSocket.Handle]);
                }
                // 重新调用开始接收数据
                clientSocket.BeginReceive(data, 0, data.Length, SocketFlags.None, ReceiveCallBack, clientSocket);
            }
            catch (Exception)
            {
                // 客户端暴力关闭
                if (clientSocket != null)
                {
                    clientSocket.Close();
                    sockets.Remove(clientSocket.Handle);
                }
            }
        }

        // 调用此函数以启动服务
        public void ServerStart()
        {
            new Thread(ConnectLoginSrv).Start();
            new Thread(KeepAliveLoginSrv).Start();
            new Thread(AcceptThread).Start();
            new Thread(SendClientPackThread).Start();
            new Thread(SendServerPackThread).Start();

            Console.WriteLine("服务端启动完毕");
        }

        static void Main(string[] args)
        {
            LoginGate loginGate = new LoginGate();
            loginGate.ServerStart();
            Console.ReadLine();
        }
    }
}
