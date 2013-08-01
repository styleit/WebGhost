using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlogGhost
{
    public class BlogIndexItem
    {
        public string Title { get; set; }
        public string URL { get; set; }
        public BlogIndexItem(string title, string url)
        {
            this.Title = title;
            this.URL = url;
        }
        public BlogIndexItem() { }
    }
}
