﻿using Scan.Entities;
using Scan.Enums;
using Scan.ServerAgent.Classes;
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
using Scan.Logger;
using Scan.Notifier;

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
            Log.WriteLine("Service initialization started");
            InitializeComponent();
            Log.WriteLine("Service initialization completed");

            config = GetConfigXml();
            Log.WriteLine("Configuration file attached");

            timer = new System.Timers.Timer();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerElapsed);
            timer.Interval = int.Parse(config.SelectSingleNode("//ServerConfig/TimerInterval").InnerText.ToString());
            Log.WriteLine("Timer initialization completed");

            val = new Validator();
            this.pingTimeOut = int.Parse(config.SelectSingleNode("//ServerConfig/PingTimeOut").InnerText.ToString());
        }

        #region Service Methods

        protected override void OnStart(string[] args)
        {
            timer.Start();
            Log.WriteLine("Timer ticking started");
        }

        protected override void OnStop()
        {

        }

        public void OnDebug()
        {
            CallCheckRound();
        }

        #endregion

        #region Defined Methods

        private XmlDocument GetConfigXml()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
#if DEBUG
                doc.Load("ServerAgentConfig.xml");
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
                Log.WriteLine(ex.Message);
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
                Log.WriteLine("ERROR : " + ex.Message);
                throw ex;
            }

        }

        public List<Report> CheckRound()
        {
            reports = new List<Report>();
            //Get configured Clients
            var ClientList = GetClientList(config.SelectNodes("//ClientNode"));

            reports.Add(new Report(DateTime.MaxValue, EnumTestCriteria.ReachGateway, EnumTestResults.Pending));
            reports.Add(new Report(DateTime.MaxValue, EnumTestCriteria.ReachDomain, EnumTestResults.Pending));
            foreach (var item in ClientList)
            {
                reports.Add(new Report(DateTime.MaxValue, EnumTestCriteria.ReachClient, EnumTestResults.Pending));
            }


            Log.WriteLine("Check round started");

            try
            {
                //Check Gateway connectivity
                string gatewayIp = config.SelectSingleNode("//ServerConfig/Gateway").InnerText.ToString();
                try
                {

                    if (val.PingToIP(gatewayIp, this.pingTimeOut))
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachGateway).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Successful;
                        rep.Remarks = rep.Criteria.ToString() + " [" + gatewayIp + "] " + rep.Result.ToString();
                        Log.WriteLine("Ping to gateway is successful : " + gatewayIp);
                        //Console.WriteLine("Ping to gateway is successful: " + gatewayIp);
                    }
                    else
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachGateway).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Failed;
                        rep.Remarks = rep.Criteria.ToString() + " [" + gatewayIp + "]" + rep.Result.ToString();
                        Log.WriteLine("Ping to gateway is failed  : " + gatewayIp);
                        //Console.WriteLine("Ping to gateway is  : failed " + gatewayIp);
                    }
                }
                catch (Exception _ex1)
                {
                    var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachGateway).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                    rep.Result = EnumTestResults.Failed;
                    rep.Remarks = rep.Criteria.ToString() + " [" + gatewayIp + "]" + rep.Result.ToString();
                    Log.WriteLine("Ping to gateway is failed  : " + gatewayIp);
                    Log.WriteLine(_ex1.Message + " : " + gatewayIp);
                    //Console.WriteLine("Ping to gateway is  : failed " + gatewayIp);
                }

                //Check internet connectivity by pinging to a web address
                string pingDomain = config.SelectSingleNode("//ServerConfig/TestPingDomain").InnerText.ToString();
                try
                {
                    if (val.PingToDomain(pingDomain, this.pingTimeOut))
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachDomain).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Successful;
                        rep.Remarks = rep.Criteria.ToString() + " [" + pingDomain + "] " + rep.Result.ToString();
                        Log.WriteLine("Ping to domain is successful : " + pingDomain);
                        //Console.WriteLine("Ping to domain is successful : " + pingDomain);
                    }
                    else
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachDomain).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Failed;
                        rep.Remarks = rep.Criteria.ToString() + " [" + pingDomain + "] " + rep.Result.ToString();
                        Log.WriteLine("Ping to domain is failed : " + pingDomain);
                        //Console.WriteLine("Ping to domain is failed : " + pingDomain);
                    }
                }
                catch (Exception _ex2)
                {
                    var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachDomain).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                    rep.Result = EnumTestResults.Failed;
                    rep.Remarks = rep.Criteria.ToString() + " [" + pingDomain + "] " + rep.Result.ToString();
                    Log.WriteLine("Ping to domain is failed : " + pingDomain);
                    Log.WriteLine(_ex2.Message + " : " + pingDomain);
                    //Console.WriteLine("Ping to domain is failed : " + pingDomain);
                }



                //Check all clients one by one
                foreach (var item in ClientList)
                {

                    try
                    {
                        if (val.PingToIP(item.IpAddress, this.pingTimeOut))
                        {
                            var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachClient).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                            rep.Result = EnumTestResults.Successful;
                            rep.Remarks = rep.Criteria.ToString() + " [" + item.ClientName + "-" + item.IpAddress + "] " + rep.Result.ToString();
                            Log.WriteLine("Ping to client " + item.ClientName + "  is successful : " + item.IpAddress);
                            //Console.WriteLine("Ping to client " + item.ClientName + "  is successful : " + item.IpAddress);
                        }
                        else
                        {
                            var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachClient).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                            rep.Result = EnumTestResults.Failed;
                            rep.Remarks = rep.Criteria.ToString() + " [" + item.ClientName + "-" + item.IpAddress + "] " + rep.Result.ToString();
                            Log.WriteLine("Ping to client " + item.ClientName + "  is failed : " + item.IpAddress);
                            //Console.WriteLine("Ping to client " + item.ClientName + "  is failed : " + item.IpAddress);
                        }
                    }
                    catch (Exception _ex3)
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachClient).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Failed;
                        rep.Remarks = rep.Criteria.ToString() + " [" + item.ClientName + "-" + item.IpAddress + "] " + rep.Result.ToString();
                        Log.WriteLine("Ping to client " + item.ClientName + "  is failed : " + item.IpAddress);
                        Log.WriteLine(_ex3.Message + " : " + item.ClientName + " | " + item.IpAddress);
                        //Console.WriteLine("Ping to client " + item.ClientName + "  is failed : " + item.IpAddress);
                        continue;
                    }

                }

                Log.WriteLine("Check round completed" + Environment.NewLine);
                return reports;

            }
            catch (Exception ex)
            {
                Log.WriteLine("ERROR : " + ex.Message);
                throw ex;
            }

        }

        private void CallCheckRound()
        {
            List<Report> rList = CheckRound();

            var failures = rList.Where(x => x.Result == EnumTestResults.Failed).ToList();
            var notifConfigElem = config.SelectSingleNode("//NotificationConfig");

            if (failures.Count > 0)
            {
                Notification.Send(failures, notifConfigElem);
            }
        }

        #endregion

        #region Events

        public void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Timer elapsed");
            Log.WriteLine("Timer elapsed");

            CallCheckRound();

        }

        #endregion
    }
}
