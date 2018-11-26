using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Net;
using System.Collections.Specialized;
using System.Net.Http;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace Obfuskator
{
    class Program
    {


        static void Main(string[] args)
        {

            if (args.Length <= 0)
                return;

            if (String.IsNullOrEmpty(args[0].Trim()))
                return;
            
            string folder = args[0];

            if (!Directory.Exists(String.Format("{0}\\backup", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))))
                Directory.CreateDirectory(String.Format("{0}\\backup", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)));
            Utils.DirectoryCopy(folder, String.Format("{0}\\backup\\{1}", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DateTime.Now.Ticks), true);


            Directory.EnumerateFileSystemEntries(folder, "*.*", SearchOption.TopDirectoryOnly)
             .Where(s => Directory.Exists(s))
             .ToList()
             .ForEach(el =>
             {
                 Console.WriteLine(folder);
                 string newDir = el.Split('\\')[el.Split('\\').Length - 1];
                 string newDirName = Utils.CreateMD5(newDir);
                 Directory.CreateDirectory(folder + "\\" + newDirName);
                 Utils.DirectoryCopy(el, folder + "\\" + newDirName, true);
                 if (Directory.Exists(el))
                     Directory.Delete(el, true);

                 if (Directory.Exists(folder + "\\" + newDirName))
                     Utils.recurseFolder(folder + "\\" + newDirName);
             });


            Directory.EnumerateFileSystemEntries(folder, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".lua") || s.EndsWith(".xml"))
            .ToList()
            .ForEach(el =>
            {
                FileInfo f = new FileInfo(el);
                if (f.Name.EndsWith(".lua"))
                {
                    string localPath = $"{ Path.GetDirectoryName(el) }\\{ Utils.CreateMD5(f.Name.Split('.')[0])}.lua";
                    f.MoveTo(localPath);

                    Process myProcess = new Process();

                    try
                    {
                        Utils.DownloadLuac(localPath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                if (f.Name == "meta.xml")
                {

                    bool notWrite = false;
                    List<string> newMeta = new List<string>();
                    File.ReadAllLines(el)
                    .ToList()
                    .ForEach(str =>
                    {

                        Regex regex = new Regex(@"src=""(\S+)""");
                        MatchCollection matches = regex.Matches(str);
                        if (matches.Count > 0)
                        {
                            //foreach (Match match in matches)
                            string[] src = matches[0].Groups[1].Value.Split('/');
                            string fileName = src[src.Length - 1].Split('.')[0];
                            string fileTemp = src[src.Length - 1].Split('.')[1];
                            if (fileTemp.Equals("lua"))
                                fileName = Utils.CreateMD5(fileName) + ".luac";
                            else
                                fileName += "." + fileTemp;

                            string tmpMD5 = "";
                            for (int i = 0; i < src.Length - 1; i++)
                            {
                                tmpMD5 += Utils.CreateMD5(src[i]) + @"/";
                            }
                            tmpMD5 += fileName;

                            tmpMD5 = str.Replace($"{ String.Join("/", src)}", $"{tmpMD5}");
                            Console.WriteLine(tmpMD5);
                            newMeta.Add(tmpMD5);
                        }
                        else
                        {
                            if (str.IndexOf("<!--") == -1 && str.IndexOf("-->") == -1 && !notWrite)
                                newMeta.Add(str);

                            if (str.IndexOf("<!--") != -1 && str.IndexOf("-->") == -1)
                                notWrite = true;
                            if (str.IndexOf("<!--") == -1 && str.IndexOf("-->") != -1)
                                notWrite = false;
                        }
                    });
                    File.WriteAllLines(f.FullName, newMeta.ToArray());
                }
            });

            const string PRIVATE_KEY = "ThePasswordToDecryptAndEncryptTheFile";
            Directory.EnumerateFileSystemEntries(folder, "*.*", SearchOption.AllDirectories)
           .Where(s => s.EndsWith(".dff") || s.EndsWith(".txd") || s.EndsWith(".col"))
           .ToList()
           .ForEach(item =>
           {
               Console.WriteLine(item);
               Utils.FileEncrypt(item, PRIVATE_KEY,true);
           });

           Console.ReadLine();
        }
    }
}
