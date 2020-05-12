using Scan.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Scan.Diagnostic
{
    public class Validator
    {
        public bool PingToIP(string ipAddr, int timeout)
        {
            try
            {
                Ping myPing = new Ping();
                PingReply reply = myPing.Send(ipAddr, timeout);
                if (reply != null)
                {
                    if (reply.Status == IPStatus.Success)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool PingToDomain(string domainName, int timeout)
        {
            try
            {
                Ping myPing = new Ping();
                PingReply reply = myPing.Send(domainName, timeout);
                if (reply != null)
                {
                    if (reply.Status == IPStatus.Success)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public double PingToIP(string ipAddr, int timeout, int retryAttempts)
        {
            double successCount = 0.0;
            for (int i = 1; i <= retryAttempts; i++)
            {
                Ping myPing = new Ping();
                try
                {
                    PingReply reply = myPing.Send(ipAddr, timeout);
                    if (reply != null)
                    {
                        if (reply.Status == IPStatus.Success)
                        {
                            successCount++;
                            Log.WriteLine("PingIP [" + i + "] : " + ipAddr + " Passed");
                        }
                        else
                        {
                            Log.WriteLine("PingIP [" + i + "] : " + ipAddr + " Failed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("EXCEPTION IN PING : " + ipAddr);
                    Log.WriteLine(ex.Message);
                    continue;
                }
                myPing.Dispose();
                System.Threading.Thread.Sleep(10000);
            }
            return successCount;
        }

        public double PingToDomain(string domainName, int timeout, int retryAttempts)
        {
            double successCount = 0;
            for (int i = 1; i <= retryAttempts; i++)
            {
                try
                {
                    Ping myPing = new Ping();
                    PingReply reply = myPing.Send(domainName, timeout);
                    if (reply != null)
                    {
                        if (reply.Status == IPStatus.Success)
                        {
                            successCount++;
                            Log.WriteLine("PingDomain [" + i + "] : " + domainName + " Passed");
                        }
                        else
                        {
                            Log.WriteLine("PingDomain [" + i + "] : " + domainName + " Failed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("EXCEPTION IN PING : " + domainName);
                    Log.WriteLine(ex.Message);
                    continue;
                }
                System.Threading.Thread.Sleep(10000);
            }
            return successCount;
        }

    }
}
