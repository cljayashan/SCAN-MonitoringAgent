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
        int pingTimeOut;

        public ServerAgent()
        {
            Logger.WriteLine("Service initialization started");
            InitializeComponent();
            Logger.WriteLine("Service initialization completed");

            config = GetConfigXml();
            Logger.WriteLine("Configuration file attached");

            timer = new System.Timers.Timer();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerElapsed);
            timer.Interval = int.Parse(config.SelectSingleNode("//ServerConfig/TimerInterval").InnerText.ToString());
            Logger.WriteLine("Timer initialization completed");

            val = new Validator();
            this.pingTimeOut = int.Parse(config.SelectSingleNode("//ServerConfig/PingTimeOut").InnerText.ToString());
        }

        #region Service Methods

        protected override void OnStart(string[] args)
        {
            timer.Start();
            Logger.WriteLine("Timer ticking started");
        }

        protected override void OnStop()
        {

        }

        public void OnDebug()
        {

        }

        #endregion

        #region Defined Methods

        private XmlDocument GetConfigXml()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
#if DEBUG
                            doc.Load("../../ServerAgentConfig.xml");
#else
                //doc.Load(Directory.GetCurrentDirectory() + @"\ServerAgentConfig.xml");
                doc.Load(AppDomain.CurrentDomain.BaseDirectory + @"\ServerAgentConfig.xml");
                //doc.Load(@"F:\scanpublish\ServerAgentConfig.xml");
#endif
                string xmlcontents = doc.InnerXml;
                return doc;
            }
            catch (Exception ex)
            {
                Logger.WriteLine(ex.Message);
                throw ex;
            }

        }

        private List<Client> GetClientList(XmlNodeList nodeList)
        {
            try
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
            catch (Exception ex)
            {
                Logger.WriteLine("ERROR : " + ex.Message);
                throw ex;
            }

        }

        public void CheckRound()
        {
            reports = new List<Report>();
            Logger.WriteLine("Check round started");

            try
            {
                //Check Gateway connectivity
                string gatewayIp = config.SelectSingleNode("//ServerConfig/Gateway").InnerText.ToString();
                if (val.PingToIP(gatewayIp, this.pingTimeOut))
                {
                    Console.WriteLine("Ping to gateway is failed : " + gatewayIp);
                    Logger.WriteLine("Ping to gateway is failed : " + gatewayIp);
                    reports.Add(new Report(DateTime.Now, EnumTestCriteria.ReachGateway, EnumTestResults.Successful));
                }
                else
                {
                    reports.Add(new Report(DateTime.Now, EnumTestCriteria.ReachGateway, EnumTestResults.Failed));
                    Console.WriteLine("Ping to gateway is successful : " + gatewayIp);
                    Logger.WriteLine("Ping to gateway is successful : " + gatewayIp);
                }


                //Check internet connectivity by pinging to a web address
                string pingDomain = config.SelectSingleNode("//ServerConfig/TestPingDomain").InnerText.ToString();
                if (val.PingToDomain(pingDomain, this.pingTimeOut))
                {
                    Console.WriteLine("Ping to domain is failed : " + pingDomain);
                    Logger.WriteLine("Ping to domain is failed : " + pingDomain);
                    reports.Add(new Report(DateTime.Now, EnumTestCriteria.ReachDomainName, EnumTestResults.Successful));
                }
                else
                {
                    reports.Add(new Report(DateTime.Now, EnumTestCriteria.ReachDomainName, EnumTestResults.Failed));
                    Console.WriteLine("Ping to domain is successful : " + pingDomain);
                    Logger.WriteLine("Ping to domain is successful : " + pingDomain);
                }

                //Get configured Clients
                var ClientList = GetClientList(config.SelectNodes("//ClientNode"));

                //Check all clients one by one
                foreach (var item in ClientList)
                {
                    if (val.PingToIP(item.IpAddress, this.pingTimeOut))
                    {
                        Console.WriteLine("Ping to client " + item.ClientName + "  is successful : " + item.IpAddress);
                        Logger.WriteLine("Ping to client " + item.ClientName + "  is successful : " + item.IpAddress);
                        reports.Add(new Report(DateTime.Now, EnumTestCriteria.ReachClient, EnumTestResults.Successful));
                    }
                    else
                    {
                        Console.WriteLine("Ping to client " + item.ClientName + "  is failed : " + item.IpAddress);
                        Logger.WriteLine("Ping to client " + item.ClientName + "  is failed : " + item.IpAddress);
                        reports.Add(new Report(DateTime.Now, EnumTestCriteria.ReachClient, EnumTestResults.Failed));
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLine("ERROR : " + ex.Message);
                throw ex;
            }

            Logger.WriteLine("Check round completed" + Environment.NewLine);
        }

        #endregion

        #region Events

        public void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Timer elapsed");
            Logger.WriteLine("Timer elapsed");
            CheckRound();
        }

        #endregion
    }
}
