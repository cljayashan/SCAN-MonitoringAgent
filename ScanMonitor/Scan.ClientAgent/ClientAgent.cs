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
        int retryCount;
        double successPercentage;

        public ClientAgent()
        {
            Log.WriteLine("Client Agent Service initialization started");
            InitializeComponent();
            Log.WriteLine("Client Agent Service initialization completed");

            try
            {
                config = GetConfigXml();
                Log.WriteLine("Configuration file attached");

                timer = new System.Timers.Timer();
                timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerElapsed);
                timer.Interval = int.Parse(config.SelectSingleNode("//ClientConfig/TimerInterval").InnerText.ToString());
                Log.WriteLine("Timer initialization completed");

                val = new Validator();
                this.pingTimeOut = int.Parse(config.SelectSingleNode("//ClientConfig/PingTimeOut").InnerText.ToString());
                this.retryCount = int.Parse(config.SelectSingleNode("//ClientConfig/RetryAttempts").InnerText.ToString());
                this.successPercentage = double.Parse(config.SelectSingleNode("//ClientConfig/SuccessPercentage").InnerText.ToString());
            }
            catch (Exception ex)
            {
                Log.WriteLine("Initialization failed");
                Log.WriteLine(ex.Message);
            }
        }

        protected override void OnStart(string[] args)
        {
            timer.Start();
            Log.WriteLine("Timer ticking started");
            CallCheckRound();
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
                Notification.Send(failures, notifConfigElem, EnumReport.ClientAgent);
            }
        }

        private List<Report> CheckRound()
        {
            reports = new List<Report>();

            var ApplianceList = GetApplianceList(config.SelectNodes("//ApplianceNode"));

            reports.Add(new Report(DateTime.MaxValue, EnumTestCriteria.ReachGateway, EnumTestResults.Pending));
            reports.Add(new Report(DateTime.MaxValue, EnumTestCriteria.ReachDomain, EnumTestResults.Pending));
            //reports.Add(new Report(DateTime.MaxValue, EnumTestCriteria.ReachApplication, EnumTestResults.Pending));
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
                    Log.WriteLine("Ping to gateway is started : " + gatewayIp);
                    double succRate = (val.PingToIP(gatewayIp, this.pingTimeOut, this.retryCount) / retryCount) * 100;
                    if (succRate >= successPercentage)
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachGateway).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Successful;
                        //rep.Remarks = rep.Criteria.ToString() + " [" + gatewayIp + "] " + rep.Result.ToString();
                        Log.WriteLine("Ping to gateway is successful : [" + succRate.ToString() + "%] " + gatewayIp);
                        //Log : Ping to gateway is successful [100%] - 192.168.1.1
                    }
                    else
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachGateway).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Failed;
                        rep.Remarks = rep.Criteria.ToString() + " " + rep.Result.ToString() + " [" + succRate.ToString() + "%] - " + gatewayIp;
                        Log.WriteLine("Ping to gateway is failed [" + succRate.ToString() + "%] - " + gatewayIp);
                    }
                }
                catch (Exception ex)
                {
                    var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachGateway).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                    rep.Result = EnumTestResults.Failed;
                    rep.Remarks = rep.Criteria.ToString() + " failed due to exception. - " + gatewayIp + ". ";
                    Log.WriteLine("Ping to gateway is failed due to  exception  : " + gatewayIp);
                    Log.WriteLine("EXCEPTION : " + ex.Message);
                }
                Log.WriteLine("Ping to gateway is completed : " + gatewayIp);


                //Check server connectivity here
                string serverIp = config.SelectSingleNode("//ServerConfig/ServerIp").InnerText.ToString();
                try
                {
                    Log.WriteLine("Ping to server is started " + serverIp);
                    double succRate = (val.PingToIP(serverIp, this.pingTimeOut, this.retryCount) / retryCount) * 100;
                    if (succRate >= successPercentage)
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachServer).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Successful;
                        //rep.Remarks = rep.Criteria.ToString() + " [" + serverIp + "] " + rep.Result.ToString();
                        Log.WriteLine("Ping to server is successful : [" + succRate.ToString() + "%] " + serverIp);
                    }
                    else
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachServer).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Failed;
                        rep.Remarks = rep.Criteria.ToString() + " " + rep.Result.ToString() + " [" + succRate.ToString() + "%] - " + serverIp;
                        Log.WriteLine("Ping to server is failed [" + succRate.ToString() + "%] - " + serverIp);
                    }
                }
                catch (Exception ex)
                {
                    var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachServer).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                    rep.Result = EnumTestResults.Failed;
                    rep.Remarks = rep.Criteria.ToString() + " failed due to exception - " + serverIp;
                    Log.WriteLine("Ping to server is failed due to exception : " + serverIp);
                    Log.WriteLine("EXCEPTION : " + ex.Message);
                }
                Log.WriteLine("Ping to server is completed " + serverIp);


                //Check internet connectivity by pinging to a web address
                string pingDomain = config.SelectSingleNode("//ClientConfig/TestPingDomain").InnerText.ToString();
                try
                {
                    Log.WriteLine("Ping to domain is started : " + pingDomain);
                    double succRate = (val.PingToDomain(pingDomain, this.pingTimeOut, this.retryCount) / retryCount) * 100;
                    if (succRate >= successPercentage)
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachDomain).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Successful;
                        //rep.Remarks = rep.Criteria.ToString() + " [" + pingDomain + "] " + rep.Result.ToString();
                        Log.WriteLine("Ping to domain is successful : [" + succRate.ToString() + "%] " + pingDomain);
                    }
                    else
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachDomain).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Failed;
                        rep.Remarks = rep.Criteria.ToString() + " " + rep.Result.ToString() + " [" + succRate.ToString() + "%] - " + pingDomain;
                        //rep.Remarks = rep.Criteria.ToString() + " " + rep.Result.ToString() + " [" + succRate.ToString() + "%]. ";
                        Log.WriteLine("Ping to domain is failed [" + succRate.ToString() + "%] - " + pingDomain);
                    }
                }
                catch (Exception ex)
                {
                    var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachDomain).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                    rep.Result = EnumTestResults.Failed;
                    rep.Remarks = rep.Criteria.ToString() + " failed due to exception [" + pingDomain + "].";
                    Log.WriteLine("Ping to domain is failed due to exceptioin " + pingDomain);
                    Log.WriteLine("EXCEPTION : " + ex.Message);
                }
                Log.WriteLine("Ping to domain is completed : " + pingDomain);

                ////Check application here
                //string webAppUrl = config.SelectSingleNode("//ServerConfig/ServerApplicationConfig").InnerText.ToString();
                //try
                //{
                //    WebRequest request = WebRequest.Create(webAppUrl);
                //    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //    if (response == null || response.StatusCode != HttpStatusCode.OK)
                //    {
                //        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachApplication).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                //        rep.Result = EnumTestResults.Failed;
                //        rep.Remarks = rep.Criteria.ToString() + " [" + webAppUrl + "] " + rep.Result.ToString();
                //        Log.WriteLine("Application is not available " + webAppUrl);
                //        Log.WriteLine("Status : " + response.StatusDescription);
                //    }
                //    else
                //    {
                //        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachApplication).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                //        rep.Result = EnumTestResults.Successful;
                //        rep.Remarks = rep.Criteria.ToString() + " [" + webAppUrl + "] " + rep.Result.ToString();
                //        Log.WriteLine("Web app is up and running : " + webAppUrl);
                //    }
                //}
                //catch (Exception ex)
                //{
                //    var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachApplication).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                //    rep.Result = EnumTestResults.Failed;
                //    rep.Remarks = rep.Criteria.ToString() + " [" + webAppUrl + "] " + rep.Result.ToString();
                //    Log.WriteLine("Application is not available due to exception " + webAppUrl);
                //    Log.WriteLine("Exception " + ex.Message);
                //}



                //Check appliance here
                foreach (var item in ApplianceList)
                {
                    try
                    {
                        Log.WriteLine("Ping to appliance is started : " + item.DisplayName + "-" + item.IpAddress);
                        double succRate = (val.PingToIP(item.IpAddress, this.pingTimeOut, this.retryCount) / retryCount) * 100;
                        if (succRate >= successPercentage)
                            if (succRate >= successPercentage)
                            {
                                var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachAppliance).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                                rep.Result = EnumTestResults.Successful;
                                //rep.Remarks = rep.Criteria.ToString() + " [" + item.DisplayName + "-" + item.IpAddress + "] " + rep.Result.ToString() + "-[" + perc + "] ";
                                Log.WriteLine("Ping to appliance is successful : [" + succRate.ToString() + "%] " + item.IpAddress);
                            }
                            else
                            {
                                var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachAppliance).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                                rep.Result = EnumTestResults.Failed;
                                rep.Remarks = rep.Criteria.ToString() + " [" + item.DisplayName + "-" + item.IpAddress + "] " + rep.Result.ToString() + "-[" + succRate + "%] ";
                                Log.WriteLine("Ping to appliance is failed [" + succRate.ToString() + "%] - " + item.IpAddress);
                            }
                    }
                    catch (Exception ex)
                    {
                        var rep = reports.Where(x => x.Criteria == EnumTestCriteria.ReachAppliance).Where(y => y.Result == EnumTestResults.Pending).FirstOrDefault();
                        rep.Result = EnumTestResults.Failed;
                        rep.Remarks = rep.Criteria.ToString() + " failed due to exception - " + "[" + item.DisplayName + " - " + item.IpAddress + "]";
                        Log.WriteLine("Ping to appliance'" + item.DisplayName + "' is failed due to exception : " + item.IpAddress);
                        Log.WriteLine("EXCEPTION : " + ex.Message);
                        continue;
                    }
                    Log.WriteLine("Ping to appliance is completed : " + item.DisplayName + "-" + item.IpAddress);
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
