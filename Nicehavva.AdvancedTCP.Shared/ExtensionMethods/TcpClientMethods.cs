using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


public static class TcpClientMethods
{
    public static String GetIP(this TcpClient client)
    {
        return ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
    }
    public static TcpState GetState(this TcpClient tcpClient)
    {
        var foo = IPGlobalProperties.GetIPGlobalProperties()
          .GetActiveTcpConnections()
          .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint)
                             && x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint)
          );

        return foo != null ? foo.State : TcpState.Unknown;
    }
}



