using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    class Ecoder
    {
        public static string ENCODEING = "GB2312";
        //static private int BufSize = 1024;

        // 代码从delphi翻译而来，原理未知，故没有注释
        // 代码中的大量注释为尝试去除buff区域而留
        /*
        static public string Encode6BitBuf(string source)
        {
            string dest = "";
            //char[] dest = new char[BufSize];
            int destlen = dest.Length;
            int restcount = 0;
            //int destpos = 0;
            byte rest = 0;
            byte made;
            byte ch;
            foreach (char c in source)
            {
                //if (destpos >= destlen)
                //    break;
                ch = (byte)c;
                made = (byte)((rest | (ch >> (2 + restcount))) & 0x3F);
                rest = (byte)(((ch << (8 - (2 + restcount))) >> 2) & 0x3F);
                restcount += 2;
                if (restcount < 6)
                {
                    dest += (char)(made + 0x3C);
                    //dest[destpos] = (char)(made + 0x3C);
                    //destpos++;
                }
                else
                {
                    dest += (char)(made + 0x3C);
                    //if (destpos < destlen - 1)
                    //{
                    //    dest[destpos] = (char)(made + 0x3C);
                    //    dest[destpos + 1] = (char)(rest + 0x3C);
                    //    destpos += 2;
                    //}
                    //else
                    //{
                    //    dest[destpos] = (char)(made + 0x3C);
                    //    destpos++;
                    //}
                    restcount = 0;
                    rest = 0;
                }
            }
            if (restcount > 0)
            {
                dest += (char)(rest + 0x3c);
                //dest[destpos] = (char)(rest + 0x3C);
                //destpos++;
            }
            //dest[destpos] = '\0';
            //return new string(dest);
            return dest;
        }
        static public string Decode6BitBuf(string source)
        {
            string buf = "";
            //char[] buf = new char[BufSize];
            byte[] Masks = new byte[] { 0x00, 0x00, 0xFC, 0xF8, 0xF0, 0xE0, 0xC0 };
            byte ch;
            byte tmp = 0;
            byte _byte;
            int bitpos = 2;
            int madebit = 0;
            //int bufpos = 0;

            foreach (char c in source)
            {
                if (c - 0x3C >= 0)
                {
                    ch = (byte)(c - 0x3C);
                }
                else
                {
                    break;
                }
                //if (bufpos >= buf.Length)
                //    break;
                if (madebit + 6 >= 8)
                {
                    _byte = (byte)(tmp | (ch & 0x3F) >> (6 - bitpos));
                    buf += (char)_byte;
                    //buf[bufpos] = (char)_byte;
                    //bufpos++;
                    madebit = 0;
                    if (bitpos < 6)
                    {
                        bitpos += 2;
                    }
                    else
                    {
                        bitpos = 2;
                        continue;
                    }
                }
                tmp = (byte)((ch << bitpos) & Masks[bitpos]);
                madebit += 8 - bitpos;
            }
            //buf[bufpos] = '\0';      // 给结尾添上\0
            //return new string(buf);
            return buf;
        }
        */
        static public string CSEncode(string source)
        {
            byte bcal, bl, bflag1, bflag2;
            int i;
            string res = "";
            bflag2 = 0;
            i = 0;
            foreach (char c in source)
            {
                bl = (byte)(c ^ 0xEB);
                if (i < 2)
                {
                    bcal = bl;
                    bcal = (byte)(bcal >> 2);
                    bflag1 = bcal;
                    bcal = (byte)(bcal & 0x3C);
                    bl = (byte)(bl & 3);
                    bcal = (byte)(bcal | bl);
                    bcal = (byte)(bcal + 0x3B);
                    res += (char)bcal;
                    bflag2 = (byte)((bflag1 & 3) | (bflag2 << 2));
                }
                else
                {
                    bcal = bl;
                    bcal = (byte)(bcal & 0x3F);
                    bcal = (byte)(bcal + 0x3B);
                    res += (char)bcal;
                    bl = (byte)(bl >> 2);
                    bl = (byte)(bl & 0x30);
                    bl = (byte)(bl | bflag2);
                    bl = (byte)(bl + 0x3B);
                    res += (char)bl;
                    bflag2 = 0;
                }
                i++;
                i %= 3;
            }
            if (i != 0)
            {
                res += (char)(bflag2 + 0x3B);
            }
            return res;
        }
        static public string CSDecode(string source)
        {
            byte b1, b2, b3, c1, c2, c3, c4;
            int x, y;
            string res = "";
            x = source.Length / 4;
            if (source.Length > 3)
            {
                for (int i = 0; i < x; i++)
                {
                    c1 = (byte)(source[i * 4 + 0] - 0x3B);
                    c2 = (byte)(source[i * 4 + 1] - 0x3B);
                    c3 = (byte)(source[i * 4 + 2] - 0x3B);
                    c4 = (byte)(source[i * 4 + 3] - 0x3B);
                    b1 = (byte)((c1 & 0xFC) << 2);
                    b2 = (byte)(c1 & 3);
                    b3 = (byte)(c4 & 0xC);
                    res += (char)((b1 | b2 | b3) ^ 0xEB);
                    b1 = (byte)((c2 & 0xFC) << 2);
                    b2 = (byte)(c2 & 3);
                    b3 = (byte)((c4 & 3) << 2);
                    res += (char)((b1 | b2 | b3) ^ 0xEB);
                    b1 = (byte)((c4 & 0x30) << 2);
                    res += (char)((c3 | b1) ^ 0xEB);
                }
            }
            y = source.Length % 4;
            if (y == 2)
            {
                c1 = (byte)(source[x * 4 + 0] - 0x3B);
                c2 = (byte)(source[x * 4 + 1] - 0x3B);
                b1 = (byte)((c1 & 0xFC) << 2);
                b2 = (byte)(c1 & 3);
                b3 = (byte)((c2 & 3) << 2);
                res += (char)((b1 | b2 | b3) ^ 0xEB);
            }
            else if (y == 3)
            {
                c1 = (byte)(source[x * 4 + 0] - 0x3B);
                c2 = (byte)(source[x * 4 + 1] - 0x3B);
                c3 = (byte)(source[x * 4 + 2] - 0x3B);  // 原delphi代码此处使用c4，为美观故改为c3
                b1 = (byte)((c1 & 0xFC) << 2);
                b2 = (byte)(c1 & 3);
                b3 = (byte)(c3 & 0xC);
                res += (char)((b1 | b2 | b3) ^ 0xEB);
                b1 = (byte)((c2 & 0xFC) << 2);
                b2 = (byte)(c2 & 3);
                b3 = (byte)((c3 & 3) << 2);
                res += (char)((b1 | b2 | b3) ^ 0xEB);
            }
            return res;
        }
        // mycode是CScode的变体，将3B改为2B，将EB改为BB，其余相同
        static public string myEncode(string source)
        {
            byte bl, bcal, bflag1, bflag2;
            int i;
            string res = "";
            bflag2 = 0;
            i = 0;
            foreach (char c in source)
            {
                bl = (byte)(c ^ 0xBB);
                if (i < 2)
                {
                    bcal = bl;
                    bcal = (byte)(bcal >> 2);
                    bflag1 = bcal;
                    bcal = (byte)(bcal & 0x3C);
                    bl = (byte)(bl & 3);
                    bcal = (byte)(bcal | bl);
                    bcal = (byte)(bcal + 0x2B);
                    res += (char)bcal;
                    bflag2 = (byte)((bflag1 & 3) | (bflag2 << 2));
                }
                else
                {
                    bcal = bl;
                    bcal = (byte)(bcal & 0x3F);
                    bcal = (byte)(bcal + 0x2B);
                    res += (char)bcal;
                    bl = (byte)(bl >> 2);
                    bl = (byte)(bl & 0x30);
                    bl = (byte)(bl | bflag2);
                    bl = (byte)(bl + 0x2B);
                    res += (char)bl;
                    bflag2 = 0;
                }
                i++;
                i %= 3;
            }
            if (i != 0)
            {
                res += (char)(bflag2 + 0x2B);
            }
            return res;
        }
        static public string myDecode(string source)
        {
            byte b1, b2, b3, c1, c2, c3, c4;
            int x, y;
            string res = "";
            x = source.Length / 4;
            if (source.Length > 3)
            {
                for (int i = 0; i < x; i++)
                {
                    c1 = (byte)(source[i * 4 + 0] - 0x2B);
                    c2 = (byte)(source[i * 4 + 1] - 0x2B);
                    c3 = (byte)(source[i * 4 + 2] - 0x2B);
                    c4 = (byte)(source[i * 4 + 3] - 0x2B);
                    b1 = (byte)((c1 & 0xFC) << 2);
                    b2 = (byte)(c1 & 3);
                    b3 = (byte)(c4 & 0xC);
                    res += (char)((b1 | b2 | b3) ^ 0xBB);
                    b1 = (byte)((c2 & 0xFC) << 2);
                    b2 = (byte)(c2 & 3);
                    b3 = (byte)((c4 & 3) << 2);
                    res += (char)((b1 | b2 | b3) ^ 0xBB);
                    b1 = (byte)((c4 & 0x30) << 2);
                    res += (char)((c3 | b1) ^ 0xBB);
                }
            }
            y = source.Length % 4;
            if (y == 2)
            {
                c1 = (byte)(source[x * 4 + 0] - 0x2B);
                c2 = (byte)(source[x * 4 + 1] - 0x2B);
                b1 = (byte)((c1 & 0xFC) << 2);
                b2 = (byte)(c1 & 3);
                b3 = (byte)((c2 & 3) << 2);
                res += (char)((b1 | b2 | b3) ^ 0xBB);
            }
            else if (y == 3)
            {
                c1 = (byte)(source[x * 4 + 0] - 0x2B);
                c2 = (byte)(source[x * 4 + 1] - 0x2B);
                c3 = (byte)(source[x * 4 + 2] - 0x2B);  // 原delphi代码此处使用c4，为美观故改为c3
                b1 = (byte)((c1 & 0xFC) << 2);
                b2 = (byte)(c1 & 3);
                b3 = (byte)(c3 & 0xC);
                res += (char)((b1 | b2 | b3) ^ 0xBB);
                b1 = (byte)((c2 & 0xFC) << 2);
                b2 = (byte)(c2 & 3);
                b3 = (byte)((c3 & 3) << 2);
                res += (char)((b1 | b2 | b3) ^ 0xBB);
            }
            return res;
        }
    }
}
