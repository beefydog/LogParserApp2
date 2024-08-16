using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LogParserApp.Utilities;
public class Formatting
{

    public static string FormatIPAddress(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return ip;

        if (IPAddress.TryParse(ip, out IPAddress addr))
        {
            if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return FormatIPv4Address(ip);
            }
            else if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                return FormatIPv6Address(ip);
            }
        }
        return ip;
    }

    private static string FormatIPv4Address(string ip)
    {
        string[] octets = ip.Split('.');
        if (octets.Length != 4) return ip;

        return string.Join(".", octets.Select(octet => int.Parse(octet).ToString("D3")));
    }

    private static string FormatIPv6Address(string ip)
    {
        byte[] bytes = IPAddress.Parse(ip).GetAddressBytes();

        string[] segments = new string[8];
        for (int i = 0; i < 8; i++)
        {
            segments[i] = string.Format("{0:x4}", BitConverter.ToUInt16(bytes, i * 2));
        }

        string formattedIP = string.Join(":", segments);
        return formattedIP;
    }


}
