using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlogGhost
{
    public class ContentSem
    {
        public ContentSem(string type,int index, int length, string content)
        {
            this.Type = type;
            this.Index = index;
            this.Length = length;
            this.Content = content;
        }
        public ContentSem()
        {
            
        }
        public string Content { get; set; }
        public string Type { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }
    }
}
