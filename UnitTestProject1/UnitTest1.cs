using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            int testCount = 100000;      // 每个模块测试次数
            // 测试加密解密模块是否有效
            for (int i = 0; i < testCount; i++)
            {
                string str = MadeRandomString(10, 100);
                Assert.AreEqual(str, Ecoder.CSDecode(Ecoder.CSEncode(str)));
                Assert.AreEqual(str, Ecoder.myDecode(Ecoder.myEncode(str)));
            }
            
        }

        // 生成一个包含数字和字母的随机字符串，长度不小于minLen，不大于等于maxLen
        private string MadeRandomString(int minLen, int maxLen)
        {
            Random rd = new Random();
            int len = rd.Next(minLen, maxLen);  // 随机获得长度
            string res = "";
            // 生成字典
            string dict = "";
            for (char i = 'a'; i <= 'z'; i++)
            {
                dict += i;
            }
            for (char i = 'A'; i <= 'Z'; i++)
            {
                dict += i;
            }
            for (char i = '0'; i <= '9'; i++)
            {
                dict += i;
            }
            // 从字典随机抽取字符
            for (int i = 0; i < len; i++)
            {
                res += dict[rd.Next(0, dict.Length)];
            }
            return res;
        }
    }
}
