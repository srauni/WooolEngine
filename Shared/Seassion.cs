using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    // 传入的客户端会话
    abstract class Seassion
    {
        protected Socket socket = null;
        protected string message = "";
        protected long TimeLastReceive = 0;   // 上一次收到客户端封包的时间，用于判断连接是否超时
        protected LinkedList<string> sendBuffer = new LinkedList<string>();       // 待发送的数据包
        enum State
        {
            Connect,
            Disconnect
        }
        public Seassion(Socket socket)
        {
            this.socket = socket;
            TimeLastReceive = DateTime.Now.ToBinary();
        }
        // 接收一次数据包，并判断是否足够传回一次完整的数据，是则打包并返回true
        abstract public bool ReceiveMessage(string str);
        // 使用对象内的socket发送数据
        abstract public void Send(string message);
        // 取出一条待发送的数据包，如果没有就返回null
        public string GetSendPack()
        {
            if (sendBuffer.Count == 0)
            {
                return null;
            }
            string res = sendBuffer.First();
            sendBuffer.RemoveFirst();
            return res;
        }
        // 传入超时时间，判断是否已经超时
        public bool IsTimeout(long Timeout)
        {
            long watseTime = DateTime.Now.ToBinary() - TimeLastReceive;
            return watseTime >= Timeout ? true : false;
        }
        // 截取两字符之间的字符串
        protected string ArrestStringEx(ref string source, char start, char end)
        {
            int indexStart = -1;
            int indexEnd = -1;
            for (int i = 0; i < source.Length; i++)
            {
                if (indexStart == -1 && source[i] == start)
                {
                    indexStart = i;
                }
                else if (indexEnd == -1 && source[i] == end)
                {
                    indexEnd = i;
                    break;
                }
            }
            if (indexStart == -1 || indexEnd == -1)
            {
                return null;
            }
            string sourceBackup = source;
            source = source.Substring(indexEnd + 1);
            return sourceBackup.Substring(indexStart + 1, indexEnd - indexStart - 1);
        }
    }
    // user和server的区别是，server和client接收到的封包格式不同，所以要分割单独判断
    class ClientSeassion : Seassion
    {
        public ClientSeassion(Socket socket) : base(socket)
        {

        }
        public override bool ReceiveMessage(string str)
        {
            Console.WriteLine("ClientReceive收到了" + str);
            bool flag = false;
            message += str;
            TimeLastReceive = DateTime.Now.ToBinary();
            // 判断缓冲区有没有终止符!
            while (message.Contains('!'))
            {
                // 有终止符则将整个包取出并返回（连通分隔符原封不动取出）
                int indexStart = -1;
                int indexEnd = -1;
                for (int i = 0; i < message.Length; i++)
                {
                    // 寻找整个包的首尾
                    if (message[i] == '#')
                    {
                        indexStart = i;
                    }
                    else if (message[i] == '!')
                    {
                        indexEnd = i;
                    }
                }
                // 判断有效性，再将其取出
                if (indexStart != -1 && indexEnd != -1)
                {
                    string temp = message.Substring(indexStart, indexEnd - indexStart + 1);
                    message = message.Substring(indexEnd + 1);
                    Console.WriteLine("取出了" + temp);                        // 加上handle并打包成转发格式
                    temp = "%A" + socket.Handle + "/" + temp + "$";
                    sendBuffer.AddLast(temp);
                    flag = true;
                }
                else
                {
                    // 无效则去掉!以及前面的字符
                    message = message.Substring(message.IndexOf('!') + 1);
                }
            }
            return flag;
        }
        // 使用对象内的socket发送数据
        public override void Send(string message)
        {
            socket.Send(Encoding.GetEncoding("GB2312").GetBytes(message));
        }
    }
    class ServerSeassion : Seassion
    {
        public ServerSeassion(Socket socket) : base(socket)
        {

        }
        public override bool ReceiveMessage(string str)
        {
            bool flag = false;      // 返回是否有可发送的数据包
            message += str;
            TimeLastReceive = DateTime.Now.ToBinary();
            while (message.Contains('$'))
            {
                // 有终止符则将整个包取出并返回（连通分隔符原封不动取出）
                int indexStart = -1;
                int indexEnd = -1;
                for (int i = 0; i < message.Length; i++)
                {
                    // 寻找整个包的首尾
                    if (message[i] == '%')
                    {
                        indexStart = i;
                    }
                    else if (message[i] == '$')
                    {
                        indexEnd = i;
                    }
                }
                // 判断有效性，再将其取出
                if (indexStart != -1 && indexEnd != -1)
                {
                    string temp = message.Substring(indexStart, indexEnd - indexStart + 1);
                    message = message.Substring(indexEnd + 1);
                    // 判断指令的类型，再做下一步判断
                    if (temp[1] == '+' && temp[2] == '+')
                    {
                        Console.WriteLine("收到来自loginSrv的心跳包，没有送入转发队列");
                    }
                    else
                    {
                        // 包含了handle的包，直接取出送给发包器处理
                        sendBuffer.AddLast(temp);
                        flag = true;
                    }
                }
                else
                {
                    // 无效则去掉!以及前面的字符
                    message = message.Substring(message.IndexOf('!') + 1);
                }
            }
            return flag;
        }
        public override void Send(string message)
        {
            socket.Send(Encoding.GetEncoding("GB2312").GetBytes(message));
        }
    }
}
