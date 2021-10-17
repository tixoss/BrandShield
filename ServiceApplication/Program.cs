using Microsoft.Owin.Hosting;
using System;

namespace ServiceApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>(url: Consts.BASE_ADDRESS))
            {
                Console.WriteLine("Service started. Press any key to stop");
                Console.ReadLine();
            }
        }
    }
}
