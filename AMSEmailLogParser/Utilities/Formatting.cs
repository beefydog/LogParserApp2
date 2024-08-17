using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AMSEmailLogParser.Utilities;
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

    //note: this formats IPv4 addresses w/padding for sorting in the DB
    private static string FormatIPv4Address(string ip)
    {
        string[] octets = ip.Split('.');
        if (octets.Length != 4) return ip;

        return string.Join(".", octets.Select(octet => int.Parse(octet).ToString("D3")));
    }

    // note: this formats IPv6 addresses to full size - makes sorting easier in the database
    private static string FormatIPv6Address(string ip)
    {
        var address = IPAddress.Parse(ip);
        var bytes = address.GetAddressBytes();
        ushort[] segments = new ushort[8];

        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = (ushort)((bytes[i * 2] << 8) + bytes[i * 2 + 1]);
        }

        return string.Join(":", segments.Select(segment => segment.ToString("x4")));
    }


}
