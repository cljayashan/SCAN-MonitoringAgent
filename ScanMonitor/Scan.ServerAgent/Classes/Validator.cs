using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Scan.ServerAgent.Classes
{
    public class Validator
    {
        public bool PingToIP(string ipAddr)
        {
            Ping myPing = new Ping();
            PingReply reply = myPing.Send(ipAddr, 1000);
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

        public bool PingToDomain(string domainName)
        {
            Ping myPing = new Ping();
            PingReply reply = myPing.Send(domainName, 1000);
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

    }
}
