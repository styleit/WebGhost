using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;

namespace BlogGhost
{
    public class BlogCSDN
    {
        private string baseUrl = "http://blog.csdn.net/newest.html?page={0}";
        private Regex regIndex = new Regex("<div class=\"blog_list\">[\\d\\D]*?<h1>[\\d\\D]*?<a name[\\d\\D]*?href=\"(.*?)\"[\\d\\D]*?>(.*?)</a>");

        private Dictionary<string,string> GetList(string url)
        {
            HttpClient client = new HttpClient();
            Task<string> indexTask = client.GetStringAsync(url);
            string content = indexTask.Result;
            Dictionary<string,string> result = new Dictionary<string,string>();
            MatchCollection mc = regIndex.Matches(content);
            foreach (Match item in mc)
            {
                result.Add(item.Groups[1].Value, item.Groups[2].Value);
            }
            return result;
        }

        public string GetContent(string url)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> Next(int index)
        {
            return this.GetList(string.Format(baseUrl, index));
        }
   } 
}
