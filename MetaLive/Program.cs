using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;

namespace MetaLive
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            new DataReceiver().DoJob();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
