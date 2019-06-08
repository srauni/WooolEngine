using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace LoginTool
{
    class LoginTool
    {
        private string host = "127.0.0.1";
        private string port = "7000";
        private string group = "人工智能";
        private string groupNick = "ai";
        private string websitePage = "http://127.0.0.1/";

        public void GameStart()
        {
            // 将服务器写入配置文件
            StreamWriter iniGame = new StreamWriter(new FileStream("Data\\game.ini", FileMode.Create), Encoding.GetEncoding("GB2312"));
            iniGame.WriteLine("[Config]");
            iniGame.WriteLine("Group0=" + group);
            iniGame.WriteLine("GroupNick0=" + groupNick);
            iniGame.WriteLine("GroupNum=" + "1");
            iniGame.WriteLine("ServerIP=" + host);
            iniGame.WriteLine("ServerPort=" + port);
            iniGame.WriteLine("Area=" + "1");
            iniGame.WriteLine("Bind=" + websitePage);
            iniGame.Close();
            // 将配置文件转为GB2312

            // 删除popup.dat，避免每次游戏关闭都弹出东西
            File.Delete("Data\\popup.dat");

            // 运行程序
            if (File.Exists("Data\\woool.dat"))
            {
                // 将游戏改为可执行文件
                File.Move("Data\\woool.dat", "Data\\woool.exe");
            }
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo("Data\\woool.exe");
            process.StartInfo = startInfo;
            process.Start();
        }
        static void Main()
        {
            LoginTool loginTool = new LoginTool();
            loginTool.GameStart();
        }
    }
}
