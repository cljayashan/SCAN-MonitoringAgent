using Scan.ServerAgent.Classes;
using Scan.ServerAgent.Entities;
using Scan.ServerAgent.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Scan.ServerAgent
{
    public partial class ServerAgent : ServiceBase
    {
        XmlDocument config;
        private System.Timers.Timer timer;
        private Validator val;
        List<Report> reports;
        Server server;

        public ServerAgent()
        {
            InitializeComponent();
            config = GetConfigXml();

            timer = new System.Timers.Timer();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerElapsed);
            timer.Interval = int.Parse(config.SelectSingleNode("//ServerConfig/TimerInterval").InnerText.ToString());

            //val = new Validator();
        }

        #region ServiceMethods

        protected override void OnStart(string[] args)
        {
            timer.Start();
        }

        protected override void OnStop()
        {

        }

        public void OnDebug()
        {

        }

        #endregion



        private XmlDocument GetConfigXml()
        {
            try
            {
                XmlDocument doc = new XmlDocument();


#if DEBUG
                            doc.Load("../../ServerAgentConfig.xml");
#else
                doc.Load(@"F:\scanpublish\ServerAgentConfig.xml");
#endif
                string xmlcontents = doc.InnerXml;

                return doc;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }




        private List<Client> GetClientList(XmlNodeList nodeList)
        {
            List<Client> clients = new List<Client>();
            foreach (XmlNode client in nodeList)
            {
                Client c = new Client();

                c.ClientId = "";
                c.ClientName = client["ClientName"].InnerText;
                c.IpAddress = client["IPAddress"].InnerText;
                c.WinLogin = client["WinLogin"].InnerText;
                c.DisplayName = client["DisplayName"].InnerText;

                clients.Add(c);

            }
            return clients;
        }

        public void StartRound()
        {
            reports = new List<Report>();

            //Check Gateway connectivity
            string gatewayIp = config.SelectSingleNode("//ServerConfig/Gateway").InnerText.ToString();
            if (val.PingToIP(gatewayIp))
            {
                Console.WriteLine("Ping to gateway is failed : " + gatewayIp);
                reports.Add(new Report(DateTime.Now, EnumTestCriteria.ReachGateway, EnumTestResults.Successful));
            }
            else
            {
                reports.Add(new Report(DateTime.Now, EnumTestCriteria.ReachGateway, EnumTestResults.Failed));
                Console.WriteLine("Ping to gateway is successful : " + gatewayIp);
            }


            //Check internet connectivity by pinging to a web address
            string pingDomain = config.SelectSingleNode("//ServerConfig/TestPingDomain").InnerText.ToString();
            if (val.PingToDomain(pingDomain))
            {
                Console.WriteLine("Ping to domain is failed : " + pingDomain);
                reports.Add(new Report(DateTime.Now, EnumTestCriteria.ReachDomainName, EnumTestResults.Successful));
            }
            else
            {
                reports.Add(new Report(DateTime.Now, EnumTestCriteria.ReachDomainName, EnumTestResults.Failed));
                Console.WriteLine("Ping to domain is successful : " + pingDomain);
            }


            //Get configured Clients
            var ClientList = GetClientList(config.SelectNodes("//ClientNode"));

            //Check all clients one by one
            foreach (var item in ClientList)
            {
                if (val.PingToIP(item.IpAddress))
                {
                    Console.WriteLine("Ping to client " + item.ClientName + "  is successful : " + item.IpAddress);
                    reports.Add(new Report(DateTime.Now, EnumTestCriteria.ReachClient, EnumTestResults.Successful));
                }
                else
                {
                    Console.WriteLine("Ping to client " + item.ClientName + "  is failed : " + item.IpAddress);
                    reports.Add(new Report(DateTime.Now, EnumTestCriteria.ReachClient, EnumTestResults.Failed));
                }
            }
        }



        #region Events

        public void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Timer Ticking");
            StartRound();
        }

        #endregion
    }
}
