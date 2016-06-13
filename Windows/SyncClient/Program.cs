using cn.antontech.SyncClient.Properties;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace cn.antontech.SyncClient
{
    class Program
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            logger.Info("Start Sync");
            string baseDir = Settings.Default.BaseDir;
            if (!(Directory.Exists(baseDir)))
            {
                Directory.CreateDirectory(baseDir);
            }

            await DownloadAddon("https://dl.google.com/android/repository/repository-12.xml");
            await DownloadAddon("https://dl.google.com/android/repository/repository2-1.xml");
            await DownloadAddon("https://dl.google.com/android/repository/addon.xml");
            await DownloadAddon("https://dl.google.com/android/repository/addon2-1.xml");
            await DownloadAddon("https://dl.google.com/android/repository/extras/intel/addon.xml");
            await DownloadAddon("https://dl.google.com/android/repository/extras/intel/addon2-1.xml");
            await DownloadAddon("https://dl.google.com/android/repository/sys-img/android-tv/sys-img.xml");
            await DownloadAddon("https://dl.google.com/android/repository/sys-img/android-tv/sys-img2-1.xml");
            await DownloadAddon("https://dl.google.com/android/repository/sys-img/android-wear/sys-img.xml");
            await DownloadAddon("https://dl.google.com/android/repository/sys-img/android-wear/sys-img2-1.xml");
            await DownloadAddon("https://dl.google.com/android/repository/sys-img/android/sys-img.xml");
            await DownloadAddon("https://dl.google.com/android/repository/sys-img/android/sys-img2-1.xml");
            await DownloadAddon("https://dl.google.com/android/repository/sys-img/google_apis/sys-img.xml");
            await DownloadAddon("https://dl.google.com/android/repository/sys-img/google_apis/sys-img2-1.xml");
            await DownloadAddon("https://dl.google.com/android/repository/glass/addon.xml");
            await DownloadAddon("https://dl.google.com/android/repository/glass/addon2-1.xml");


        }
        static async Task DownloadAddon(string remoteAddonUri)
        {
            await DownloadAddon(remoteAddonUri, Settings.Default.BaseDir);
        }
        static async Task DownloadAddon(string remoteAddonUri, string rootDir)
        {
            string remoteBaseUri = URIHelper.GetUriDirectory(remoteAddonUri);
            string localBaseDir = URIHelper.GetLocalDir(remoteBaseUri, rootDir);
            if (!(Directory.Exists(localBaseDir)))
            {
                Directory.CreateDirectory(localBaseDir);
            }
            string localAddonFile = Path.Combine(localBaseDir, URIHelper.GetUriFile(remoteAddonUri));

            logger.Info("Start Download:{0}", remoteAddonUri);
            try
            {
                await DownloadFile(remoteAddonUri, localAddonFile);
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("Download Failed:{0}", remoteAddonUri), ex);
                return;
            }
            logger.Info("Download Complated:{0}", remoteAddonUri);
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(localAddonFile);
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("Parse Failed:{0}", localAddonFile), ex);
                return;
            }
            logger.Info("Parse Complated:{0}", localAddonFile);
            //标记是否需要重新保存xml
            bool needSave = false;
            string ns = doc.DocumentElement.NamespaceURI;
            string tagName = "archive";
            if (ns == "http://schemas.android.com/sdk/android/repo/addon2/01")
            {
                ns = "";
                tagName = "complete";
            }
            foreach (XmlElement node in doc.GetElementsByTagName(tagName, ns))
            {
                long size = Int64.Parse(node.GetElementsByTagName("size", ns)[0].InnerText);
                string checkSum = node.GetElementsByTagName("checksum", ns)[0].InnerText;
                string url = node.GetElementsByTagName("url", ns)[0].InnerText;
                string localFileName = string.Empty;
                string remoteFileUri = string.Empty;
                if (url.StartsWith("https:") || url.StartsWith("http:"))
                {
                    //如果URL不是相对路径
                    localFileName = Path.Combine(localBaseDir, URIHelper.GetUriFile(url));
                    remoteFileUri = url;
                    //更新xml文件
                    node.GetElementsByTagName("url", ns)[0].InnerText = new Uri(url).AbsolutePath;
                    needSave = true;
                }
                else
                {
                    localFileName = Path.Combine(localBaseDir, url);
                    remoteFileUri = URIHelper.CombineUri(remoteBaseUri, url);
                }
                if (File.Exists(localFileName) && IsSame(localFileName, checkSum))
                {
                    logger.Info("Nothing Changed:{0}", localFileName);
                }
                else
                {
                    logger.Info("Start Download:{0}", remoteFileUri);
                    try
                    {
                        await DownloadFile(remoteFileUri, localFileName);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(string.Format("Download Failed:{0}", remoteFileUri), ex);
                    }
                }
            }
            if (needSave)
            {
                doc.Save(localAddonFile);
            }

        }

        static async Task DownloadFile(string uri, string filename)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                client.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                await client.DownloadFileTaskAsync(uri, filename);
            }
        }

        static void WebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Thread.Sleep(500);
            Console.Write("\r {0} {1}% ", ProcessBar(100), 100);
            Console.WriteLine("\nDownload completed");
        }

        static void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.Write("\r {0} {1}% , Total: {2}, Received: {3}", ProcessBar(e.ProgressPercentage), e.ProgressPercentage, SizeSuffix(e.TotalBytesToReceive), SizeSuffix(e.BytesReceived));
        }

        static string ProcessBar(int percentage)
        {
            StringBuilder sb = new StringBuilder();
            int a = percentage / 10;
            for (int i = 1; i <= a; i++)
            {
                sb.Append("■");
            }
            for (int i = a + 1; i <= 10; i++)
            {
                sb.Append("□");
            }
            return sb.ToString();
        }

        private static bool IsSame(string fileName, string checkSum)
        {
            string checkSum2 = string.Empty;
            using (var sha1 = SHA1.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    checkSum2 = BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
            return string.Equals(checkSum, checkSum2, StringComparison.OrdinalIgnoreCase);
        }

        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }
    }
}
