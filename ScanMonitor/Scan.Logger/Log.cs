using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scan.Logger
{
    public static class Log
    {
        public static void WriteLine(string message)
        {
            message = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "\t" + message;

            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
           
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(message);
                }
            }
        }
    }
}
