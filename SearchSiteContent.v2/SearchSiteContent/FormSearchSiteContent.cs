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
        public FormBrowser Browser;
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
                textBoxLinks.Clear();
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

        private void stop()
        {
            if (Browser != null)
            {
                Browser.Close();
                Browser = null;
                MessageBox.Show("Процесс прерван пользователем!", "Сообщение");
                return;
            }
            
            if (thread.ThreadState.ToString() == "Unstarted")
            {
                MessageBox.Show("Процесс еще не запущен", "Сообщение");
                return;
            }

            try
            {
                thread.Abort();
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Ошибка");
            }
            MessageBox.Show("Процесс прерван пользователем!", "Сообщение");
        }

        private void startFastSearch()
        {
            if (thread.ThreadState.ToString() == "Running" || Browser != null)
            {
                MessageBox.Show("Процесс поиска уже запущен", "Сообщение");
                return;
            }

            richTextBoxReport.Clear();
            richTextBoxValueFound.Clear();
            richTextBoxValueNotFound.Clear();
            toolStripProgressBar1.Maximum = 0;
            toolStripProgressBar1.Value = 0;
            toolStripStatusLabel3.Text = "...";
            toolStripStatusLabel4.Text = "0%";
            thread = new Thread(runFastSearch);
            thread.Start();
        }

        private void startSmartSearch()
        {
            if (thread.ThreadState.ToString() == "Running" || Browser != null)
            {
                MessageBox.Show("Процесс поиска уже запущен", "Сообщение");
                return;
            }

            richTextBoxReport.Clear();
            richTextBoxValueFound.Clear();
            richTextBoxValueNotFound.Clear();
            toolStripProgressBar1.Maximum = 0;
            toolStripProgressBar1.Value = 0;
            toolStripStatusLabel3.Text = "...";
            toolStripStatusLabel4.Text = "0%";
            runSmartSearchAsync();
        }

        private bool searchContentOnPage(string page, string value)
        {
            return page.Contains(value);
        }

        public void addReport(string message)
        {
            richTextBoxReport.Text = message + Environment.NewLine + richTextBoxReport.Text;
        }

        public void addValueFound(string message)
        {
            richTextBoxValueFound.Text = richTextBoxValueFound.Text + message + Environment.NewLine;
        }

        public void addValueNotFound(string message)
        {
            richTextBoxValueNotFound.Text = richTextBoxValueNotFound.Text + message + Environment.NewLine;
        }

        private void runFastSearch()
        {
            try
            {
                /* Выполняю поиск по всем собранным url */
                string page = "";
                int index = 0;
                int totalPages = textBoxLinks.Lines.Length;
                int onePercent = 0;

                if (totalPages <= 0)
                {
                    MessageBox.Show("Отсутствуют ссылки для выполнения поиска", "Сообщения");
                    thread.Abort();
                    return;
                }

                toolStripStatusLabel3.Text = "Процесс: 0/" + totalPages;
                toolStripProgressBar1.Maximum = totalPages;
                int percent = 0;

                foreach (string target in textBoxLinks.Lines)
                {
                    index++;
                    toolStripStatusLabel3.Text = "Процесс: " + index.ToString() + "/" + totalPages.ToString();
                    toolStripProgressBar1.Value = index;

                    percent = (int)(((double)toolStripProgressBar1.Value / (double)toolStripProgressBar1.Maximum) * 100);
                    toolStripStatusLabel4.Text = Convert.ToString(percent) + "%";

                    try
                    {
                        string pagetarget = getPageHtmlDOM(target);

                        if (listBoxValues.Items.Count > 0)
                        {
                            foreach (string searchValue in listBoxValues.Items)
                            {
                                if (searchValue == "") continue;
                                if (searchContentOnPage(pagetarget, searchValue) == true)
                                {
                                    addReport("Найдено значение [" + searchValue + "] на странице [" + target + "]");
                                    addValueFound("Значение [" + searchValue + "]: " + target + " - найдено");
                                }
                                else
                                {
                                    addReport("Не найдено значение [" + searchValue + "] на странице [" + target + "]");
                                    addValueNotFound("Значение [" + searchValue + "]: " + target + " - не найдено");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        addReport("Ошибка \"" + ex.Message + "\" | Страница: " + target);
                    }
                }
                toolStripStatusLabel4.Text = "100%";
                MessageBox.Show("Поиск завершен!", "Сообщение");
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Ошибка");
            }
            finally
            {
                thread.Abort();
            }
            thread.Abort();
        }

        private void runSmartSearchAsync()
        {
            Browser = new FormBrowser();
            Browser.Parent = this;
            Browser.Show();
            Browser.StartSearch();
        }

        /*
         * == Events ============================================
         */

        private void Form1_Load(object sender, EventArgs e)
        {
            Browser = null;
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

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            startFastSearch();
        }

        private void запуститьПоискToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startFastSearch();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            stop();
        }

        private void остановитьПоискToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stop();
        }

        private void richTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.LinkText);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Ошибка");
            }
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            startSmartSearch();
        }

        private void запуститьПродвинутыйПоискToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startSmartSearch();
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormAbout about = new FormAbout();
            about.ShowDialog();
        }
    }
}
