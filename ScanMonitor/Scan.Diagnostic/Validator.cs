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

    }
}
