using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Nicehavva.AdvancedTCP.Shared.Messages;
using Microsoft.CodeAnalysis.Scripting;
using System.IO;
using System.IO.Compression;
using Nicehavva.AdvancedTCP.Shared.Utility;
using System.Timers;

namespace Nicehavva.AdvancedTCP.Client.WinService
{
    public class Globals
    {
        public Client client;
        public String sender;
        public UtilityForServiceUse utility;
    }
    public class GlobalsForAfterDownload
    {
        public String filePath;
        public String fileName;
        public UtilityForServiceUse utility;
    }

    public partial class Service1 : ServiceBase
    {
        private Client client;
        Timer timerForDisconnectClient;
        public Service1()
        {
            InitializeComponent();
        }
        public string ServerIP { get; set; }
        public string Port { get; set; }
        protected override void OnStart(string[] args)
        {
           
            ServerIP = ConfigurationManager.AppSettings["ServerIP"];
            int port= Convert.ToInt32(ConfigurationManager.AppSettings["ServerPort"]);
            Port = port.ToString();

            client = new Client();
            RegisterEvents();

            var loginName = ConfigurationManager.AppSettings["username"];

            client.Connect(ServerIP, port);
            client.Login(loginName, (sender, response) => { });
        }

        protected override void OnStop()
        {
            client.Disconnect();
        }
        protected override void OnCustomCommand(int command)
        {
            base.OnCustomCommand(command);
            try
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains($"CustomCommand{command}"))
                {
                    var script = GetScriptRunner(ConfigurationManager.AppSettings[$"CustomCommand{command}"], typeof(Globals));
                    var result = script(new Globals { client = client, sender = "", utility = new UtilityForServiceUse() });
                }
            }
            catch
            {
            }
        }
        private void RegisterEvents()
        {
            //client
            client.SessionRequest += client_SessionRequest;
            client.TextMessageReceived += client_TextMessageReceived;
            client.FileUploadRequest += client_FileUploadRequest;
            client.FileUploadProgress += client_FileUploadProgress;
            client.ClientDisconnected += client_ClientDisconnected;
            client.SessionClientDisconnected += client_SessionClientDisconnected;
            client.GenericRequestReceived += client_GenericRequestReceived;
            client.SessionEndedByTheRemoteClient += client_SessionEndedByTheRemoteClient;
        }
        void client_SessionRequest(Client client, EventArguments.SessionRequestEventArguments args)
        {
            var validClient = ConfigurationManager.AppSettings["ValidClient"];
            if (validClient.Split(',').Contains(args.Request.ClientName))
            {
                args.Confirm();
            }
            else
            {
                args.Refuse();
            }
            
        }
        void client_TextMessageReceived(Client sender,TextMessageRequest request, string message)
        {
            try
            {
                if (ConfigurationManager.AppSettings.AllKeys.Contains(message))
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains($"{message}ValidClient"))
                    {
                        if (!ConfigurationManager.AppSettings[$"{message}ValidClient"].Split(',').Contains(request.SenderClient))
                        {
                            return;
                        }
                    }
                    var script = GetScriptRunner(ConfigurationManager.AppSettings[message], typeof(Globals));
                    script(new Globals { client = client, sender = request.SenderClient, utility = new UtilityForServiceUse() });
                }
            }
            catch
            {
            }
        }
        void client_FileUploadRequest(Client client, EventArguments.FileUploadRequestEventArguments args)
        {
            var validClient = ConfigurationManager.AppSettings["AcceptDownLoadFrom"];
            if (validClient.Split(',').Contains(args.Request.SenderClient))
            {
                if (File.Exists(ConfigurationManager.AppSettings["DownLoadFolder"] + args.Request.FileName))
                {
                    File.Delete(ConfigurationManager.AppSettings["DownLoadFolder"] + args.Request.FileName);
                }
                args.Confirm(ConfigurationManager.AppSettings["DownLoadFolder"]+args.Request.FileName);
            }
            else
            {
                args.Refuse();
            }
        }
        void client_FileUploadProgress(Client client, EventArguments.FileUploadProgressEventArguments args)
        {
            if (args.CurrentPosition >= args.TotalBytes)
            {
                if (!String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["AfterDownLoadAction"]))
                {
                    var script = GetScriptRunner(ConfigurationManager.AppSettings["AfterDownLoadAction"],typeof(GlobalsForAfterDownload));
                    script(new GlobalsForAfterDownload { filePath = args.DestinationPath, fileName=args.FileName, utility = new UtilityForServiceUse() });
                }
            }
        }
        void client_ClientDisconnected(Client obj)
        {
            timerForDisconnectClient = new Timer();
            timerForDisconnectClient.Elapsed += new ElapsedEventHandler(OnElapsedTimeForDisconnectClient);
            timerForDisconnectClient.Interval = 5000;
            timerForDisconnectClient.Enabled = true;
        }
        void client_SessionClientDisconnected(Client obj)
        {
            
        }
        void client_GenericRequestReceived(Client client, Shared.Messages.GenericRequest msg)
        {
            
        }
        void client_SessionEndedByTheRemoteClient(Client client)
        {
        }

        ScriptRunner<object> GetScriptRunner(string Command,Type type)
        {
            return CSharpScript.Create(Command, ScriptOptions.Default.AddReferences(typeof(Enumerable).Assembly.Location, typeof(ZipFile).Assembly.Location, typeof(System.Data.SqlClient.SqlConnection).Assembly.Location).WithImports("System.Linq", "System.IO", "System.IO.Compression"), globalsType: type)
                        .CreateDelegate();
        }

        private void OnElapsedTimeForDisconnectClient(object sender, ElapsedEventArgs e)
        {
            try
            {
                var loginName = ConfigurationManager.AppSettings["username"];

                client.Connect(ServerIP, Convert.ToInt32(Port));
                client.Login(loginName, (senders, response) => { });

                timerForDisconnectClient.Stop();
                timerForDisconnectClient.Dispose();
            }
            catch { }
        }
    }
}
