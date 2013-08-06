using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BlogGhost
{
    class Program
    {
        static void Main()
        {
            BlogCSDN csdn = new BlogCSDN();
            csdn.Next(2);

            Console.ReadLine();
        }
    }
}
