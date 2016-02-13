using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        protected string currGrabPath = "";
        protected string nextGrab = "/singapore-7020";
        bool grabNext = false;
        bool downloaded = false;
        private DateTime lastUpdate;
        private long lastBytes = 0;

        public Form1()
        {
            InitializeComponent();
            this.currGrabPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "grabbed");
            if (!Directory.Exists(this.currGrabPath)) Directory.CreateDirectory(this.currGrabPath);
            label1.Text = "";
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
            label2.Text = "";
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(3000);
                    Grab();
                }
            });
        }

        private void Grab()
        {
            WebClient client = new WebClient();
            grabNext = false;
            lastBytes = 0;
            if (this.nextGrab != "")
            {
                if (label1.InvokeRequired)
                    label1.Invoke(new Action(() => label1.Text = "Download data from " + "http://earthview.withgoogle.com" + this.nextGrab));
                else
                    label1.Text = "Download data from " + "http://earthview.withgoogle.com" + this.nextGrab;

                client.DownloadDataCompleted -= client_DownloadDataCompleted;
                client.DownloadProgressChanged -= client_DownloadProgressChanged;
                client.DownloadDataCompleted += client_DownloadDataCompleted;
                client.DownloadProgressChanged += client_DownloadProgressChanged;
                client.DownloadDataAsync(new Uri("http://earthview.withgoogle.com" + this.nextGrab));

            }
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (progressBar1.InvokeRequired)
                progressBar1.Invoke(new Action(() => progressBar1.Value = e.ProgressPercentage));
            else progressBar1.Value = e.ProgressPercentage;

            long bytes = e.BytesReceived;
            if (lastBytes == 0)
            {
                lastUpdate = DateTime.Now;
                lastBytes = bytes;
                return;
            }

            var now = DateTime.Now;
            var timeSpan = now - lastUpdate;
            var bytesChange = bytes - lastBytes;
            double bytesPerSecond = 0.0;
            try
            {
                bytesPerSecond = bytesChange / timeSpan.Milliseconds;
            }
            catch (Exception)
            {
                bytesPerSecond = 0.0;
            }
            bytesPerSecond = Math.Abs(bytesPerSecond);
            string misura = "B/s";
            if (bytesPerSecond >= 1024)
            {
                bytesPerSecond /= 1024;
                misura = "KB/s";
            }
            lastBytes = bytes;
            lastUpdate = now;

            if (label2.InvokeRequired)
                label2.Invoke(new Action(() => label2.Text = bytesPerSecond.ToString("F") + " " + misura));
            else label2.Text = bytesPerSecond.ToString("F") + " " + misura;
        }

        void client_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            byte[] data = e.Result;
            IEnumerable<XNode> auctionNodes = Majestic12ToXml.ConvertNodesToXml(data);
            WebClient client = new WebClient();
            downloaded = false;
            if (progressBar1.InvokeRequired)
                progressBar1.Invoke(new Action(() => progressBar1.Value = 0));
            else progressBar1.Value = 0;
            client.DownloadProgressChanged -= client_DownloadProgressChanged;
            client.DownloadFileCompleted -= client_DownloadFileCompleted;
            client.DownloadProgressChanged += client_DownloadProgressChanged;
            client.DownloadFileCompleted += client_DownloadFileCompleted;
            foreach (XElement anchorTag in auctionNodes.OfType<XElement>().DescendantsAndSelf("a"))
            {
                if (anchorTag.Attribute("href") == null)
                    continue;
                if (anchorTag.Attribute("class").Value.Contains("menu__item--download"))
                {
                    string app = anchorTag.Attribute("href").Value;
                    string fileName = app.Split('/').ToList<string>().Last<string>();
                    fileName = Path.Combine(this.currGrabPath, fileName);
                    string URLDownload = "http://earthview.withgoogle.com" + app;
                    if (label1.InvokeRequired)
                        label1.Invoke(new Action(() => label1.Text = "Download file from " + URLDownload));
                    else
                        label1.Text = "Download file from " + URLDownload;
                    lastBytes = 0;
                    if (!File.Exists(fileName))
                        client.DownloadFileAsync(new Uri(URLDownload), fileName);


                }
                if (anchorTag.Attribute("class").Value.Contains("pagination__link--next"))
                {
                    this.nextGrab = anchorTag.Attribute("href").Value;
                    grabNext = true;
                }

                if (downloaded && grabNext) break;
            }
        }

        void client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            downloaded = true;
            if (label1.InvokeRequired)
                label1.Invoke(new Action(() => label1.Text = "Download completed"));
            else
                label1.Text = "Download completed";
        }

    }
}