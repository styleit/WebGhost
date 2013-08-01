using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Diagnostics;

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
            return false;
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

            List<ContentSem> markList = new List<ContentSem>();
            
            MatchCollection mc = regImage.Matches(result);
            foreach (Match imgItem in mc)
            {
                ContentSem cs = new ContentSem("img", imgItem.Groups[1].Index, imgItem.Groups[1].Length, imgItem.Groups[1].Value);
                markList.Add(cs);
            }

            mc = regCode.Matches(result);
            foreach (Match codeItem in mc)
            {
                ContentSem cs = new ContentSem("code", codeItem.Groups[0].Index, codeItem.Groups[0].Length, codeItem.Groups[0].Value);
                markList.Add(cs);
            }

            markList.OrderBy(c => c.Index);

            foreach (ContentSem semItem in markList)
            {
                if (semItem.Type == "img")
                {

                }
            }
            //articalList.Add(result.Substring(
            //start task to process img and code.
        }

        private void ExecuteCmd(string command)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;

            p.Start();
            p.StandardInput.WriteLine(command);

            p.StandardInput.WriteLine("exit");
            p.WaitForExit();
            p.Close();
        }
        private void processImage(ContentSem sem)
        {
            string filename = string.Empty;

            if (sem.Content.Contains("?"))
            {
                sem.Content = sem.Content.Split('?')[0];
            }

            int fileNameIndex = sem.Content.Split('/').Length;
            filename = sem.Content.Split('/')[fileNameIndex - 1];
            if (sem.Content.Split('/')[0] == "http://img.blog.csdn.net/")
            {
                filename = sem.Content.Split('/')[1] + ".jpg";
            }
            WebRequest wr = WebRequest.Create(sem.Content);
            WebResponse response = wr.GetResponse();
            Stream responseStream = response.GetResponseStream();
            FileStream writer = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
            byte[] buffer = new byte[1024];
            int count = 0;
            while ((count = responseStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                writer.Write(buffer, 0, count);
            }
            writer.Close();
            responseStream.Close();

            //netdisk /e "upload E:\Pictures\psb.jpg \app\PublicFiles"
        }

        private void processCode(ContentSem sem)
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
