using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> test = new List<string>();
            test.Add("A");
            test.Add("B");
            test.Add("C");

            foreach (string item in test)
            {
                processIt(item);
            }
        }

        static void processIt(string obj)
        {
            obj = "a";
        }
    }
}
