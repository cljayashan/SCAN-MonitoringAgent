﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Scan.ClientAgent
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {

#if DEBUG
            new ClientAgent().OnDebug();
#else
           ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ClientAgent()
            };
            ServiceBase.Run(ServicesToRun);
#endif


        }
    }
}
