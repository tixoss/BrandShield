using Microsoft.Owin.Hosting;
using System;

namespace ServiceApplication
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>(url: Consts.BASE_ADDRESS))
            {
                log.Info("Service started. Press any key to stop");
                Console.ReadLine();
            }
        }
    }
}
