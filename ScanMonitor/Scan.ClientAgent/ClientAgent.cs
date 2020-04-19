using Scan.Diagnostic;
using Scan.Entities;
using Scan.Enums;
using Scan.Logger;
using Scan.Notifier;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Scan.ClientAgent
{
    public partial class ClientAgent : ServiceBase
    {
        XmlDocument config;
        private System.Timers.Timer timer;
        private Validator val;
        List<Report> reports;
        Server server;
        int pingTimeOut;

        public ClientAgent()
        {
            Log.WriteLine("Client Agent Service initialization started");
            InitializeComponent();
            Log.WriteLine("Client Agent Service initialization completed");

            config = GetConfigXml();
            Log.WriteLine("Configuration file attached");

            timer = new System.Timers.Timer();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerElapsed);
            timer.Interval = int.Parse(config.SelectSingleNode("//ClientConfig/TimerInterval").InnerText.ToString());
            Log.WriteLine("Timer initialization completed");

            val = new Validator();
            this.pingTimeOut = int.Parse(config.SelectSingleNode("//ClientConfig/PingTimeOut").InnerText.ToString());
        }

        protected override void OnStart(string[] args)
        {
        }

        protected override void OnStop()
        {
        }

        public void OnDebug()
        {
            CallCheckRound();
        }

        private XmlDocument GetConfigXml()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
#if DEBUG
                doc.Load("ClientAgentConfig.xml");
#else
                doc.Load(AppDomain.CurrentDomain.BaseDirectory + @"\ClientAgentConfig.xml");
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

        private List<Report> CheckRound()
        {
            reports = new List<Report>();

            var ApplianceList = GetApplianceList(config.SelectNodes("//ApplianceNode"));

            reports.Add(new Report(DateTime.MaxValue, EnumTestCriteria.ReachGateway, EnumTestResults.Pending));
            reports.Add(new Report(DateTime.MaxValue, EnumTestCriteria.ReachDomain, EnumTestResults.Pending));
            reports.Add(new Report(DateTime.MaxValue, EnumTestCriteria.ReachApplication, EnumTestResults.Pending));
            reports.Add(new Report(DateTime.MaxValue, EnumTestCriteria.ReachServer, EnumTestResults.Pending));
            foreach (var item in ApplianceList)
            {
                reports.Add(new Report(DateTime.MaxValue, EnumTestCriteria.ReachAppliance, EnumTestResults.Pending));
            }

            Log.WriteLine("Check round started");

            try
            {
                //Check gateway connctivity
                string gatewayIp = config.SelectSingleNode("//ClientConfig/Gateway").InnerText.ToString();
                try
                {
                    if (val.PingToIP(gatewayIp, this.pingTimeOut))
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachGateway).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Successful;
                        rep.Remarks = rep.Criteria.ToString() + " [" + gatewayIp + "] " + rep.Result.ToString();
                        Log.WriteLine("Ping to gateway is successful : " + gatewayIp);
                    }
                    else
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachGateway).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Failed;
                        rep.Remarks = rep.Criteria.ToString() + " [" + gatewayIp + "]" + rep.Result.ToString();
                        Log.WriteLine("Ping to gateway is failed  : " + gatewayIp);
                    }
                }
                catch (Exception _ex1)
                {
                    var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachGateway).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                    rep.Result = EnumTestResults.Failed;
                    rep.Remarks = rep.Criteria.ToString() + " [" + gatewayIp + "]" + rep.Result.ToString();
                    Log.WriteLine("Ping to gateway is failed  : " + gatewayIp);
                    Log.WriteLine(_ex1.Message + " : " + gatewayIp);
                }

                //Check server connectivity here
                string serverIp = config.SelectSingleNode("//ServerConfig/ServerIp").InnerText.ToString();
                try
                {
                    if (val.PingToIP(serverIp, this.pingTimeOut))
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachServer).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Successful;
                        rep.Remarks = rep.Criteria.ToString() + " [" + serverIp + "] " + rep.Result.ToString();
                        Log.WriteLine("Ping to server is successful : " + serverIp);
                    }
                    else
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachServer).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Failed;
                        rep.Remarks = rep.Criteria.ToString() + " [" + serverIp + "]" + rep.Result.ToString();
                        Log.WriteLine("Ping to server is failed  : " + serverIp);
                    }
                }
                catch (Exception _ex1)
                {
                    var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachServer).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                    rep.Result = EnumTestResults.Failed;
                    rep.Remarks = rep.Criteria.ToString() + " [" + serverIp + "]" + rep.Result.ToString();
                    Log.WriteLine("Ping to server is failed  : " + serverIp);
                    Log.WriteLine(_ex1.Message + " : " + serverIp);
                }



                //Check internet connectivity by pinging to a web address
                string pingDomain = config.SelectSingleNode("//ClientConfig/TestPingDomain").InnerText.ToString();
                try
                {
                    if (val.PingToDomain(pingDomain, this.pingTimeOut))
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachDomain).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Successful;
                        rep.Remarks = rep.Criteria.ToString() + " [" + pingDomain + "] " + rep.Result.ToString();
                        Log.WriteLine("Ping to domain is successful : " + pingDomain);
                    }
                    else
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachDomain).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Failed;
                        rep.Remarks = rep.Criteria.ToString() + " [" + pingDomain + "] " + rep.Result.ToString();
                        Log.WriteLine("Ping to domain is failed : " + pingDomain);
                    }
                }
                catch (Exception _ex2)
                {
                    var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachDomain).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                    rep.Result = EnumTestResults.Failed;
                    rep.Remarks = rep.Criteria.ToString() + " [" + pingDomain + "] " + rep.Result.ToString();
                    Log.WriteLine("Ping to domain is failed : " + pingDomain);
                    Log.WriteLine(_ex2.Message + " : " + pingDomain);
                }


                //Check application here
                string webAppUrl = config.SelectSingleNode("//ServerConfig/ServerApplicationConfig").InnerText.ToString();
                try
                {
                    WebRequest request = WebRequest.Create(webAppUrl);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response == null || response.StatusCode != HttpStatusCode.OK)
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachApplication).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Failed;
                        rep.Remarks = rep.Criteria.ToString() + " [" + webAppUrl + "] " + rep.Result.ToString();

                        Log.WriteLine("Application is not available " + webAppUrl);
                        Log.WriteLine("Status : " + response.StatusDescription);
                    }
                    else
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachApplication).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Successful;
                        rep.Remarks = rep.Criteria.ToString() + " [" + webAppUrl + "] " + rep.Result.ToString();
                        Log.WriteLine("Web app is up and running : " + webAppUrl);
                    }
                }
                catch (Exception ex)
                {
                    var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachApplication).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                    rep.Result = EnumTestResults.Failed;
                    rep.Remarks = rep.Criteria.ToString() + " [" + webAppUrl + "] " + rep.Result.ToString();

                    Log.WriteLine("Application is not available due to exception " + webAppUrl);
                    Log.WriteLine("Exception " + ex.Message);
                }
                              


                //Check appliance here
                foreach (var item in ApplianceList)
                {
                    try
                    {
                        if (val.PingToIP(item.IpAddress, this.pingTimeOut))
                        {
                            var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachAppliance).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                            rep.Result = EnumTestResults.Successful;
                            rep.Remarks = rep.Criteria.ToString() + " [" + item.DisplayName + "-" + item.IpAddress + "] " + rep.Result.ToString();
                            Log.WriteLine("Ping to appliance " + item.DisplayName + "  is successful : " + item.IpAddress);
                        }
                        else
                        {
                            var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachAppliance).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                            rep.Result = EnumTestResults.Failed;
                            rep.Remarks = rep.Criteria.ToString() + " [" + item.DisplayName + "-" + item.IpAddress + "] " + rep.Result.ToString();
                            Log.WriteLine("Ping to appliance " + item.DisplayName + "  is failed : " + item.IpAddress);
                        }
                    }
                    catch (Exception _ex3)
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachAppliance).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Failed;
                        rep.Remarks = rep.Criteria.ToString() + " [" + item.DisplayName + "-" + item.IpAddress + "] " + rep.Result.ToString();
                        Log.WriteLine("Ping to appliance " + item.DisplayName + "  is failed : " + item.IpAddress);
                        Log.WriteLine(_ex3.Message + " : " + item.DisplayName + " | " + item.IpAddress);
                        continue;
                    }
                }
                
            }
            catch (Exception exMain)
            {
                throw exMain;
            }

            return reports;
        }

        private List<Appliance> GetApplianceList(XmlNodeList nodeList)
        {
            try
            {
                List<Appliance> appliance = new List<Appliance>();
                foreach (XmlNode client in nodeList)
                {
                    Appliance a = new Appliance();

                    a.ApplianceId = "";
                    a.ApplianceName = client["ApplianceName"].InnerText;
                    a.IpAddress = client["IPAddress"].InnerText;
                    a.DisplayName = client["DisplayName"].InnerText;

                    appliance.Add(a);
                }
                return appliance;
            }
            catch (Exception ex)
            {
                Log.WriteLine("ERROR : " + ex.Message);
                throw ex;
            }

        }

        public void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Timer elapsed");
            Log.WriteLine("Timer elapsed");

            CallCheckRound();
        }
    }
}
