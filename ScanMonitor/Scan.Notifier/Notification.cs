using Scan.Entities;
using Scan.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Scan.Notifier
{
    public static class Notification
    {
        public static void Send(List<Report> reports, XmlNode notificationConfigurationElement)
        {
            try
            {
                string isSmsEnabled = notificationConfigurationElement.SelectSingleNode("//SMS").Attributes["Enabled"].Value;
                if (string.IsNullOrEmpty(isSmsEnabled))
                {
                    Log.WriteLine("SMS Enabled attribute in NotificationConfig is null or empty");
                }
                else
                {
                    int sendSms = 0;
                    if (!int.TryParse(isSmsEnabled, out sendSms))
                    {
                        Log.WriteLine("SMS Enabled attribute in NotificationConfig is NAN");
                    }
                    else
                    {
                        if (!((sendSms == 0) || (sendSms == 1)))
                        {
                            Log.WriteLine("SMS Enabled attribute in NotificationConfig is not '1' or '0'");
                        }
                        else
                        {
                            if (sendSms == 0)
                            {
                                Log.WriteLine("SMS Enabled attribute in NotificationConfig is off");
                            }
                            else
                            {
                                string msgText = "ServerAgent Report :" + Environment.NewLine;

                                foreach (var r in reports)
                                {
                                    msgText += r.Remarks + Environment.NewLine;
                                }
                                msgText += "Report End";


                                string gatewayUrl = notificationConfigurationElement.SelectSingleNode("//SMS/SMSGatewayUrl").InnerText;
                                string apiKey = notificationConfigurationElement.SelectSingleNode("//SMS/APIKey").InnerText;

                                using (var client = new HttpClient())
                                {
                                    client.DefaultRequestHeaders.Accept.Clear();
                                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                                    var recipients = notificationConfigurationElement.SelectSingleNode("//SMS/Recipients").ChildNodes;

                                    foreach (XmlNode item in recipients)
                                    {
                                        var mobile = item.InnerText.Length == 10 ? item.InnerText : 0 + item.InnerText;
                                        string uriDialog = gatewayUrl + "destination=" + mobile + "&q=" + apiKey + "&message=" + System.Uri.EscapeDataString(msgText);

                                        HttpResponseMessage response = client.GetAsync(uriDialog.ToString()).Result;
                                        string SendingStatus = "";
                                        if (response.IsSuccessStatusCode)
                                        {
                                            using (HttpContent content = response.Content)
                                            {
                                                SendingStatus = ((content.ReadAsStringAsync().Result == "0") ? "SENT" : "SENDING FAILED");
                                                Log.WriteLine("Report " + SendingStatus + " to " + mobile);
                                            }
                                        }
                                        else
                                        {
                                            Log.WriteLine("Report " + SendingStatus + " to " + mobile);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.Message);
                throw ex;
            }
        }
    }
}
