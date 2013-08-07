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
        private Regex regImage = new Regex("<img.*?src=\"(.*?)\"");
        private Regex regCode = new Regex ("<pre name=\"code\" class=\"(.*?)\">([\\d\\D]*?)</pre>");
        private Queue<BlogIndexItem> indexQueue = new Queue<BlogIndexItem>();
        private ProducerConsumerQueue<BlogIndexItem> CSDNBlogPosts = new ProducerConsumerQueue<BlogIndexItem>();

        private string GetURLContentsAsync(string url)
        {
            string content = string.Empty;

            var webReq = (HttpWebRequest)WebRequest.Create(url);

            using (WebResponse response = webReq.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream);
                    content = reader.ReadToEnd();
                }
            }
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
            Task checkItemTask = CheckItem();
            checkItemTask.Start();
            checkItemTask.Wait();

            Console.WriteLine("GetList done in fun.");
        }

        private void GetPostList(string url)
        {
 
        }
        private void PostProducter(Queue<BlogIndexItem> queue)
        {
            while (true)
            {
                
            }
        }

        private bool CheckHistory(BlogIndexItem item)
        {
            return false;
        }

        private Task CheckItem()
        {
            Console.WriteLine("CheckItem start in fun.");
            Task checkItemTask = new Task(() => {
                Queue<Task> queue = new Queue<Task>();
                for (int i = 0; i < indexQueue.Count; i++)
                {
                    BlogIndexItem indexItem = indexQueue.Dequeue();
                    if (!CheckHistory(indexItem))
                    {
                        queue.Enqueue(Task.Factory.StartNew(() => processContent(indexItem), TaskCreationOptions.AttachedToParent));
                    }
                }
                Task.Factory.ContinueWhenAll(queue.ToArray(), task => { Console.WriteLine("All CheckItem Done"); });
            });

            checkItemTask.ContinueWith(task => { Console.WriteLine("CheckItem done in fun."); });
            return checkItemTask;
        }

        private void processContent(BlogIndexItem item)
        {
            string content = string.Empty;
            Console.WriteLine("Start to processing {0}.", item.URL);
            Task<string> getContentTask = new Task<string>(() =>
            {
                string _content = string.Empty;
                _content = GetURLContentsAsync(item.URL);
                return _content;
            });
            getContentTask.Start();
            getContentTask.Wait();
            content = getContentTask.Result;

            Match artical = regContent.Match(content);
            string result = artical.Groups[1].Value;

            List<ContentSem> markList = new List<ContentSem>();

            MatchCollection mc = regImage.Matches(result);
            foreach (Match imgItem in mc)
            {
                if (imgItem.Groups[1].Value.StartsWith("http:"))
                {
                    ContentSem cs = new ContentSem("img", imgItem.Groups[1].Index, imgItem.Groups[1].Length, imgItem.Groups[1].Value);
                    markList.Add(cs);
                }
            }

            mc = regCode.Matches(result);
            foreach (Match codeItem in mc)
            {
                ContentSem cs = new ContentSem("code", codeItem.Groups[0].Index, codeItem.Groups[0].Length, codeItem.Groups[0].Value);
                markList.Add(cs);
            }

            StringBuilder buffer = new StringBuilder();
            if (markList.Count > 0)
            {
                IEnumerable<ContentSem> orderList = markList.OrderBy(c => c.Index);
                Task processImgCodeTask = ProcessImageCode(orderList);
                processImgCodeTask.Start();
                processImgCodeTask.Wait();
                
                int index = 0;
                foreach (var listItem in orderList)
                {
                    buffer.Append(result.Substring(index, listItem.Index - index));
                    buffer.Append(listItem.Content);
                    index = listItem.Index + listItem.Length;
                }
                buffer.Append(result.Substring(index, result.Length - index));
            }
            else
            {
                buffer.Append(result);
            }

            string PostContent = buffer.ToString();
            SavePost(item.Title, PostContent);
            Console.WriteLine("Processing {0} Done.", item.URL);
            
        }

        private string filterTitle(string title)
        {
            char[] filter = new char[] { '.','*','?','/','\\','|',':','\"','>','<','\''};
            foreach (char item in filter)
            {
                title = title.Replace(item.ToString(),"");
            }
            return title;
        }

        private void SavePost(string title, string content)
        {
            title = filterTitle(title);
            StreamWriter sw = new StreamWriter(title+".html");
            sw.Write(content);
            sw.Close();
        }

        private Task ProcessImageCode(IEnumerable<ContentSem> markList)
        {
            
            
            Queue<Task> queue = new Queue<Task>();
            Task processImgCodeTask = new Task(() =>
            {
                foreach (ContentSem semItem in markList)
                {
                    if (semItem.Type == "img")
                    {
                        queue.Enqueue(Task.Factory.StartNew(() => { processImage(semItem); }, TaskCreationOptions.AttachedToParent));
                    }
                    if (semItem.Type == "code")
                    {
                        queue.Enqueue(Task.Factory.StartNew(() => { processCode(semItem); }, TaskCreationOptions.AttachedToParent));
                    }
                }
                Task.Factory.ContinueWhenAll(queue.ToArray(), task => { Console.WriteLine("Meta files Done"); });
            });

            return processImgCodeTask;
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
            if (sem.Content.StartsWith("http://img.blog.csdn.net/"))
            {
                fileNameIndex = sem.Content.Split('/').Length;
                filename = sem.Content.Split('/')[fileNameIndex - 1];
                filename = filename + ".jpg";
            }
            if (File.Exists(filename))
            { }
            else
            {
                WebRequest wr = WebRequest.Create(sem.Content);
                WebResponse response = wr.GetResponse();
                Stream responseStream = response.GetResponseStream();
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

            string imageOnPost = "/{0}/{1}/{2}";
            sem.Content = string.Format(imageOnPost, DateTime.Now.Year, DateTime.Now.Month, filename);
        }

        private void processCode(ContentSem sem)
        {
            Match code = regCode.Match(sem.Content);
            string codePatern = "[{0}]{1}[/{2}]";
            sem.Content = string.Format(codePatern, code.Groups[1].Value, code.Groups[2].Value, code.Groups[1].Value);
        }

        public void Next(int index)
        {
            Task getListTask = new Task(() => { GetList(string.Format(baseUrl, index)); });
            getListTask.ContinueWith(task => { Console.WriteLine("Done in Next"); });
            getListTask.Start();
            getListTask.Wait();
        }
   } 
}
