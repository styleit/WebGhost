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
using System.Threading;

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

        /*
        private async Task<string> GetURLContentsAsync(string url)
        {
            // The downloaded resource ends up in the variable named content.
            string content = string.Empty;

            // Initialize an HttpWebRequest for the current URL.
            var webReq = (HttpWebRequest)WebRequest.Create(url);

            // Send the request to the Internet resource and wait for
            // the response.
            Task<WebResponse> responseTask = webReq.GetResponseAsync();

            using (WebResponse response = await responseTask)
            {
                // Get the data stream that is associated with the specified URL.
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream);
                    content = await reader.ReadToEndAsync();
                }
            }
            // Return the result as a byte array.
            return content;
        }
        */

        private string GetURLContentsAsync(string url)
        {
            // The downloaded resource ends up in the variable named content.
            string content = string.Empty;

            // Initialize an HttpWebRequest for the current URL.
            var webReq = (HttpWebRequest)WebRequest.Create(url);

            // Send the request to the Internet resource and wait for
            // the response.
            using (WebResponse response = webReq.GetResponse())
            {
                // Get the data stream that is associated with the specified URL.
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream);
                    content = reader.ReadToEnd();
                }
            }
            // Return the result as a byte array.
            return content;
        }


        private void GetList(string url)
        {
            string content = GetURLContentsAsync(url);
            MatchCollection mc = regIndex.Matches(content);
            foreach (Match item in mc)
            {
                Console.WriteLine("Enqueue one item.");
                indexQueue.Enqueue(new BlogIndexItem(item.Groups[2].Value, item.Groups[1].Value));
            }
            CheckItem();
            Console.WriteLine("GetList done in fun.");
        }

        private bool CheckHistory(BlogIndexItem item)
        {
            return false;
        }

        private void CheckItem()
        {
            Console.WriteLine("CheckItem start in fun.");
            Queue<Task> queue = new Queue<Task>();
            BlogIndexItem indexItem = indexQueue.Dequeue();
            if (!CheckHistory(indexItem))
            {
                queue.Enqueue(Task.Factory.StartNew(() => processContent(indexItem)));
            }

            Task.Factory.ContinueWhenAll(queue.ToArray(), task => { Console.WriteLine("All CheckItem Done"); });
            Console.WriteLine("CheckItem done in fun.");
        }

        private void processContent(BlogIndexItem item)
        {
            Console.WriteLine("Start to processing {0}.", item.URL);
            string content = GetURLContentsAsync(item.URL);
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

            if (markList.Count > 0)
            {
                markList.OrderBy(c => c.Index);

                Queue<Task> taskList = new Queue<Task>();

                foreach (ContentSem semItem in markList)
                {
                    if (semItem.Type == "img")
                    {
                        taskList.Enqueue(Task.Factory.StartNew(() => processImage(semItem)));
                    }
                    if (semItem.Type == "code")
                    {
                        taskList.Enqueue(Task.Factory.StartNew(() => processCode(semItem)));
                    }
                }
                Task.Factory.ContinueWhenAll(taskList.ToArray(), completedTasks => { Console.WriteLine("Processing {0} Done.", item.Title); });
            }

            Console.WriteLine("Processing {0} Done.", item.URL);
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
            string uploadImgCMDPattern = "netdisk /e \"upload {0} \\app\\PublicFiles\\img-51make\\{1}\\{2}\"";
            string filename = string.Empty;

            if (sem.Content.Contains("?") && sem.Content.StartsWith("http://img.blog.csdn.net/"))
            {
                sem.Content = sem.Content.Split('?')[0];
            }

            int fileNameIndex = sem.Content.Split('/').Length;
            filename = sem.Content.Split('/')[fileNameIndex - 1];
            //if (sem.Content.Split('/')[0] == "http://img.blog.csdn.net/")
            if (sem.Content.StartsWith("http://img.blog.csdn.net/"))
            {
                filename = sem.Content.Split('/')[1] + ".jpg";
            }
            WebRequest wr = WebRequest.Create(sem.Content);
            WebResponse response = wr.GetResponse();
            Stream responseStream = response.GetResponseStream();

            if (File.Exists(filename))
            { }
            else
            {
                Console.WriteLine("Will Save File : {0}", filename);
                FileStream writer = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
                byte[] buffer = new byte[1024];
                int count = 0;
                while ((count = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.Write(buffer, 0, count);
                }
                writer.Close();
                responseStream.Close();

                string cmd = string.Format(uploadImgCMDPattern, filename, DateTime.Now.Year, DateTime.Now.Month);
                Console.WriteLine("Will execute command : {0}", cmd);
                ExecuteCmd(cmd);
            }
        }

        private void processCode(ContentSem sem)
        {
            Match code = regCode.Match(sem.Content);
            string codePatern = "[{0}]{1}[/{2}]";
            sem.Content = string.Format(codePatern, code.Groups[1].Value, code.Groups[2].Value, code.Groups[1].Value);
        }

        public string GetContent(string url)
        {
            throw new NotImplementedException();
        }

        public void Next(int index)
        {
            Queue<Task> queue = new Queue<Task>();
            queue.Enqueue(Task.Factory.StartNew(()=>GetList(string.Format(baseUrl, index))));
            Task.Factory.ContinueWhenAll(queue.ToArray(), task => { Console.WriteLine("Next {0} Done",index); });
        }
   } 
}
