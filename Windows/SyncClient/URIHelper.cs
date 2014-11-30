using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cn.antontech.SyncClient
{
    class URIHelper
    {
        public static string GetUriDirectory(string uri){
            if (uri.Contains("."))
            {
                return uri.Substring(0, uri.LastIndexOf('/') + 1);
            }
            else
            {
                return uri;
            }
        }

        public static string GetUriFile(string uri)
        {
            return (new Uri(uri)).Segments.Last<string>();
        }

        public static string GetLocalDir(string uri, string localBaseDir)
        {
            List<string> result = new List<string>();
            result.Add(localBaseDir);
            result.AddRange((new Uri(uri)).AbsolutePath.Split('/'));
            return Path.Combine(result.ToArray<string>());
        }

        public static string CombineUri(string uri, string path)
        {
            return new Uri(new Uri(uri), path).AbsoluteUri;
        }
    }
}
