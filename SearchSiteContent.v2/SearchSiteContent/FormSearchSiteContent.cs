using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace SearchSiteContent
{
    public partial class FormSearchSiteContent : Form
    {
        private Thread thread;

        public FormSearchSiteContent()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void openSitemapFile()
        {
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                toolStripTextBoxPath.Text = openFileDialog1.FileName;
                loadSitemap(toolStripTextBoxPath.Text);
            }
        }

        private void loadSitemapUrl()
        {
            // https://somovstudio.github.io/sitemap.xml
            FormInputBox inputBox = new FormInputBox();
            inputBox.FormClosed += InputBox_FormClosed;
            inputBox.Parent = this;
            inputBox.ShowDialog();
            
        }

        private void InputBox_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (toolStripTextBoxPath.Text != "") loadSitemap(toolStripTextBoxPath.Text);
            else MessageBox.Show("Вы не ввели URL ссылку к карте сайта.", "Сообщение");
        }

        /*
        private ArrayList readSitemap(string sitemap)
        {
            ArrayList list = new ArrayList();
            Match match;
            string pattern = @"<loc>(\w+://[^<]+)";
            match = Regex.Match(sitemap, pattern);
            while (match.Success)
            {
                list.Add(match.Groups[1].ToString());
                match = match.NextMatch();
            }
            return list;
        }
        */

        private ArrayList readXML(string filename)
        {
            ArrayList list = new ArrayList();

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            XmlDocument xDoc;
            if (checkBoxUserAgent.Checked == false)
            {
                WebClient client = new WebClient();
                client.Headers["User-Agent"] = textBoxUserAgent.Text;
                client.Headers["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                string data = client.DownloadString(filename);

                xDoc = new XmlDocument();
                xDoc.LoadXml(data);
            }
            else
            {
                xDoc = new XmlDocument();
                xDoc.Load(filename);
            }

            XmlElement xRoot = xDoc.DocumentElement;
            foreach (XmlNode xnode in xRoot)
            {
                for (int j = 0; j <= xnode.ChildNodes.Count; j++)
                {
                    if (xnode.ChildNodes[j].Name == "loc")
                    {
                        string xmlLink = xnode.ChildNodes[j].InnerText;
                        list.Add(xmlLink);
                        break;
                    }
                }
            }

            return list;
        }

        private string getPageHtmlDOM(string url)
        {

            string html;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            //ServicePointManager.SecurityProtocol =  SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            using (WebClient web = new WebClient())
            {
                web.Encoding = Encoding.UTF8;
                if (checkBoxUserAgent.Checked == false) web.Headers.Add("user-agent", textBoxUserAgent.Text);
                html = web.DownloadString(url);
            }
            return html;
        }

        private void loadSitemap(string sitemapPath)
        {
            try
            {
                ArrayList targets = new ArrayList();
                ArrayList sitemaps = new ArrayList();

                /* собираю все sitemap */
                sitemaps.Add(sitemapPath);
                for (int i = 0; i < sitemaps.Count; i++)
                {
                    string xmlLink = sitemaps[i].ToString();
                    if (xmlLink.Contains(".xml") == true)
                    {
                        ArrayList listSitemaps = readXML(xmlLink);
                        foreach (string urlSitemap in listSitemaps)
                        {
                            if (urlSitemap.Contains(".xml") == true)
                            {
                                sitemaps.Add(urlSitemap);
                            }
                            else
                            {
                                targets.Add(urlSitemap);
                                textBoxLinks.Text += urlSitemap + Environment.NewLine;
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        
        


        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void checkBoxUserAgent_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxUserAgent.Checked == true)
            {
                checkBoxUserAgent.Text = "Включен User-Agent по умолчанию";
                textBoxUserAgent.Text = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.202 Safari/535.1";
                textBoxUserAgent.Enabled = false;
            }
            else
            {
                checkBoxUserAgent.Text = "Отключен User-Agent по умолчанию";
                textBoxUserAgent.Enabled = true;
            }
        }

        private void открытьSitemapФайлToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openSitemapFile();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            openSitemapFile();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            loadSitemapUrl();
        }

        private void загрузитьСсылкиИзSitemapПоURLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadSitemapUrl();
        }
    }
}
