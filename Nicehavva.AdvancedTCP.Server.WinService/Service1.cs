using Nicehavva.AdvancedTCP.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Nicehavva.AdvancedTCP.Server.WinService
{
    public partial class Service1 : ServiceBase
    {
        private Server server;
        public string Port { get; set; }

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            int port;
            Port = "";
            if (args.Length >= 1)
            {
                Port = args[1];
            }
            Int32.TryParse(Port, out port);
            if (port == 0) port = 5904;
            Port = port.ToString();

            RegisterServer(port);
        }

        protected override void OnStop()
        {
        }
        private void RegisterServer(int port)
        {
            server = new Server(port);
            server.ClientValidating += server_ClientValidating;
            server.Start();
        }

        void server_ClientValidating(EventArguments.ClientValidatingEventArgs args)
        {
            if (!server.Receivers.Exists(x => x.Status == StatusEnum.Validated && x.ClientName == args.Request.ClientName))
            {
                args.Confirm();
            }
            else
            {
                args.Refuse();
            }
        }
    }
}
