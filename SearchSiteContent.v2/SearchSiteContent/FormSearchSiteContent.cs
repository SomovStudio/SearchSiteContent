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
            thread = new Thread(loadSitemap);
        }

        private void openSitemapFile()
        {
            if (thread.ThreadState.ToString() == "Running")
            {
                MessageBox.Show("Загрузка ссылок в процессе, пожалуйста подождите.", "Сообщение");
                return;
            }

            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                toolStripTextBoxPath.Text = openFileDialog1.FileName;
                thread = new Thread(loadSitemap);
                thread.Start();
                panelMessageLoadLinks.Visible = true;
            }
        }

        private void loadSitemapUrl()
        {
            if (thread.ThreadState.ToString() == "Running")
            {
                MessageBox.Show("Загрузка ссылок в процессе, пожалуйста подождите.", "Сообщение");
                return;
            }

            // https://somovstudio.github.io/sitemap.xml
            FormInputBox inputBox = new FormInputBox();
            inputBox.FormClosed += InputBox_FormClosed;
            inputBox.Parent = this;
            inputBox.ShowDialog();
        }

        private void InputBox_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (toolStripTextBoxPath.Text != "")
            {
                thread = new Thread(loadSitemap);
                thread.Start();
                panelMessageLoadLinks.Visible = true;
            }
            else
            {
                MessageBox.Show("Вы не ввели URL ссылку к карте сайта.", "Сообщение");
            }
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

        private void loadSitemap()
        {
            try
            {
                ArrayList sitemaps = new ArrayList();

                /* собираю все sitemap */
                sitemaps.Add(toolStripTextBoxPath.Text);
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
                                textBoxLinks.Text += urlSitemap + Environment.NewLine;
                                textBoxLinks.Update();
                            }
                        }
                    }
                }
                MessageBox.Show("Загрузка ссылок завершена", "Сообщение");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
            panelMessageLoadLinks.Visible = false;
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

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            if (toolStripTextBoxValue.Text == "")
            {
                MessageBox.Show("Нельзя добавить пустое значение", "Сообщение");
                return;
            }
            foreach (string value in listBoxValues.Items)
            {
                if (value == toolStripTextBoxValue.Text)
                {
                    MessageBox.Show("Значение " + toolStripTextBoxValue.Text + " уже добавлено в список", "Сообщение");
                    return;
                }
            }
            listBoxValues.Items.Add(toolStripTextBoxValue.Text);
            toolStripTextBoxValue.Text = "";
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            if (listBoxValues.SelectedIndex > -1) listBoxValues.Items.RemoveAt(listBoxValues.SelectedIndex);
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            if (toolStripTextBoxValueXPath.Text == "")
            {
                MessageBox.Show("Нельзя добавить пустое значение", "Сообщение");
                return;
            }
            foreach (string value in listBoxValuesXPath.Items)
            {
                if (value == toolStripTextBoxValueXPath.Text)
                {
                    MessageBox.Show("Значение " + toolStripTextBoxValueXPath.Text + " уже добавлено в список", "Сообщение");
                    return;
                }
            }
            listBoxValuesXPath.Items.Add(toolStripTextBoxValueXPath.Text);
            toolStripTextBoxValueXPath.Text = "";
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (listBoxValuesXPath.SelectedIndex > -1) listBoxValuesXPath.Items.RemoveAt(listBoxValuesXPath.SelectedIndex);
        }

        private void addXPathInField(object sender, EventArgs e)
        {
            toolStripTextBoxValueXPath.Text = ((ToolStripMenuItem)sender).Text;
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            if (toolStripTextBoxValueCSS.Text == "")
            {
                MessageBox.Show("Нельзя добавить пустое значение", "Сообщение");
                return;
            }
            foreach (string value in listBoxValuesCSS.Items)
            {
                if (value == toolStripTextBoxValueCSS.Text)
                {
                    MessageBox.Show("Значение " + toolStripTextBoxValueCSS.Text + " уже добавлено в список", "Сообщение");
                    return;
                }
            }
            listBoxValuesCSS.Items.Add(toolStripTextBoxValueCSS.Text);
            toolStripTextBoxValueCSS.Text = "";
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            if (listBoxValuesCSS.SelectedIndex > -1) listBoxValuesCSS.Items.RemoveAt(listBoxValuesCSS.SelectedIndex);
        }

        private void addCSSInField(object sender, EventArgs e)
        {
            toolStripTextBoxValueCSS.Text = ((ToolStripMenuItem)sender).Text;
        }

    }
}
