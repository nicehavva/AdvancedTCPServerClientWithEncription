using Nicehavva.AdvancedTCP.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nicehavva.AdvancedTCP.Server.UI
{
    public partial class ServerForm : Form
    {
        private Nicehavva.AdvancedTCP.Server.Server server;
        private Timer updateListTimer;

        public ServerForm()
        {
            server = new Server(5904);
            InitializeComponent();
            RegisterEvents();

            updateListTimer = new Timer();
            updateListTimer.Interval = 1000;
            updateListTimer.Tick += updateListTimer_Tick;
            updateListTimer.Start();
        }

        void updateListTimer_Tick(object sender, EventArgs e)
        {
            UpdateClientsList();
        }

        private void RegisterEvents()
        {
            //this
            btnStart.Click += btnStart_Click;
            btnStop.Click += btnStop_Click;
            this.Load += ServerForm_Load;

            //Server
            server.ClientValidating += server_ClientValidating;
        }

        void ServerForm_Load(object sender, EventArgs e)
        {
            btnStart.PerformClick();
        }

        void server_ClientValidating(EventArguments.ClientValidatingEventArgs args)
        {
            if (!server.Receivers.Exists(x => x.Status==StatusEnum.Validated && x.ClientName == args.Request.ClientName))
            {
                args.Confirm();
            }
            else
            {
                args.Refuse();
            }
        }

        void btnStop_Click(object sender, EventArgs e)
        {
            server.Stop();
            btnStop.Enabled = false;
            btnStart.Enabled = true;
        }

        void btnStart_Click(object sender, EventArgs e)
        {
            server.Start();
            btnStop.Enabled = true;
            btnStart.Enabled = false;
        }

        private void InvokeUI(Action action)
        {
            this.Invoke(action);
        }

        private void UpdateClientsList()
        {
            InvokeUI(() =>
            {
                listClients.Items.Clear();

                foreach (var receiver in server.Receivers)
                {
                    String[] str = new String[5];
                    str[0] = receiver.ID.ToString();
                    str[1] = receiver.ClientName;
                    str[2] = receiver.Status.ToString();
                    str[3] = receiver.TotalBytesUsage.ToString();

                    //if (receiver.OtherSideReceiver != null)
                    //{
                    //    str[4] = receiver.OtherSideReceiver.ClientName;
                    //}

                    ListViewItem item = new ListViewItem(str);
                    listClients.Items.Add(item);
                }
            });

        }
    }
}
