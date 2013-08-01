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
        private Regex regContent = new Regex("id=\"article_content\"[\\d\\D]*?>([\\d\\D]*?)<div class=\"share_buttons\"");
        private Regex regImage = new Regex("<img src=\"(.*?)\"");
        private Regex regCode = new Regex ("<pre name=\"code\" class=\"(.*?)\">([\\d\\D]*?)</pre>");
        private Queue<BlogIndexItem> indexQueue = new Queue<BlogIndexItem>();

        private void GetList(string url)
        {
            HttpClient client = new HttpClient();
            Task<string> indexTask = client.GetStringAsync(url);
            string content = indexTask.Result;
            MatchCollection mc = regIndex.Matches(content);
            foreach (Match item in mc)
            {
                indexQueue.Enqueue(new BlogIndexItem(item.Groups[1].Value, item.Groups[2].Value));
            }
        }

        private bool CheckHistory(BlogIndexItem item)
        {
            return true;
        }

        private void CheckItem()
        {
            while (true)
            {
                while (indexQueue.Count == 0)
                {
                    ;
                }
                BlogIndexItem indexItem = indexQueue.Dequeue();
                if (!CheckHistory(indexItem))
                {
                    //Start a new task to process it.
                }
            }
        }

        private void processContent(BlogIndexItem item)
        {
            HttpClient client = new HttpClient();
            Task<string> indexTask = client.GetStringAsync(item.URL);
            string content = indexTask.Result;
            Match artical = regContent.Match(content);
            string result = artical.Groups[1].Value;
            
            List<string> articalList = new List<string>();

            Dictionary<int,int> markList = new Dictionary<int,int> ();
            
            MatchCollection mc = regImage.Matches(result);
            foreach (Match imgItem in mc)
            {
                markList.Add(imgItem.Groups[1].Index, imgItem.Groups[1].Length);
            }

            mc = regCode.Matches(result);
            foreach (Match codeItem in mc)
            {
                markList.Add(-1 * codeItem.Groups[0].Index, codeItem.Groups[0].Length);
            }

            markList.OrderBy(c => c.Key);

            //articalList.Add(result.Substring(
            //start task to process img and code.
        }

        private void processImage()
        {
 
        }

        private void processCode()
        {
 
        }

        public string GetContent(string url)
        {
            throw new NotImplementedException();
        }

        public void Next(int index)
        {
            this.GetList(string.Format(baseUrl, index));
        }
   } 
}
