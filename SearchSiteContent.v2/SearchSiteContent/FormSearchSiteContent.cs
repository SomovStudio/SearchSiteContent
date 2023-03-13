using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.LinkLabel;

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
                textBoxLinks.Clear();
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
                string urlSitemap = "";

                /* собираю все sitemap */
                sitemaps.Add(toolStripTextBoxPath.Text);
                for (int i = 0; i < sitemaps.Count; i++)
                {
                    string xmlLink = sitemaps[i].ToString();
                    if (xmlLink.Contains(".xml") == true)
                    {
                        ArrayList listSitemaps = readXML(xmlLink);
                        for (int j = 0; j < listSitemaps.Count; j++)
                        {
                            urlSitemap = "";
                            urlSitemap = (string)listSitemaps[j];
                            if (urlSitemap.Contains(".xml") == true)
                            {
                                sitemaps.Add(urlSitemap);
                            }
                            else
                            {
                                textBoxLinks.Text += urlSitemap;
                                if (j != (listSitemaps.Count - 1)) textBoxLinks.Text += Environment.NewLine;
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
            //richTextBoxReport.Text = message + Environment.NewLine + richTextBoxReport.Text;
            richTextBoxReport.AppendText(message + Environment.NewLine);
            richTextBoxReport.ScrollToCaret();
        }

        public void addValueFound(string message)
        {
            richTextBoxValueFound.Text = richTextBoxValueFound.Text + message + Environment.NewLine;
            //richTextBoxValueFound.AppendText(message + Environment.NewLine);
            //richTextBoxValueFound.ScrollToCaret();
        }

        public void addValueNotFound(string message)
        {
            richTextBoxValueNotFound.Text = richTextBoxValueNotFound.Text + message + Environment.NewLine;
            //richTextBoxValueNotFound.AppendText(message + Environment.NewLine);
            //richTextBoxValueNotFound.ScrollToCaret();
        }

        public void writeFile(string content, string filename)
        {
            try
            {
                StreamWriter writer;
                // DEFAULT
                // writer = new StreamWriter(filename, false, Encoding.Default);
                // UTF8
                writer = new StreamWriter(filename, false, new UTF8Encoding(false));
                // UTF8 BOM
                // writer = new StreamWriter(filename, false, new UTF8Encoding(true));
                // WINDOWS 1251
                // writer = new StreamWriter(filename, false, Encoding.GetEncoding("Windows-1251"));
                writer.Write(content);
                writer.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        public string readFile(string filename)
        {
            string content = "";
            try
            {
                StreamReader reader;
                // DEFAULT
                // reader = new StreamReader(filename, Encoding.Default);
                // UTF8
                reader = new StreamReader(filename, new UTF8Encoding(false));
                // UTF8 BOM
                // reader = new StreamReader(filename, new UTF8Encoding(true));
                // WINDOWS 1251
                // reader = new StreamReader(filename, Encoding.GetEncoding("Windows-1251"));
                content = reader.ReadToEnd();
                reader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
            return content;
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

                bool found = false;
                bool notfound = false;

                foreach (string target in textBoxLinks.Lines)
                {
                    index++;
                    toolStripStatusLabel3.Text = "Процесс: " + index.ToString() + "/" + totalPages.ToString();
                    toolStripProgressBar1.Value = index;

                    percent = (int)(((double)toolStripProgressBar1.Value / (double)toolStripProgressBar1.Maximum) * 100);
                    toolStripStatusLabel4.Text = Convert.ToString(percent) + "%";

                    try
                    {
                        addReport("Страница: " + target);
                        found = false;
                        notfound = false;

                        string pagetarget = getPageHtmlDOM(target);

                        if (listBoxValues.Items.Count > 0)
                        {
                            foreach (string searchValue in listBoxValues.Items)
                            {
                                if (searchValue == "") continue;
                                if (searchContentOnPage(pagetarget, searchValue) == true)
                                {
                                    found = true;
                                    addReport("+ Найдено значение: " + searchValue);
                                    addValueFound("Значение [" + searchValue + "]: " + target + " - найдено");
                                }
                                else
                                {
                                    notfound = true;
                                    addReport("- Не найдено значение: " + searchValue);
                                    addValueNotFound("Значение [" + searchValue + "]: " + target + " - не найдено");
                                }
                            }
                        }

                        addReport("========================================");
                        if (found == true) addValueFound("");
                        if (notfound == true) addValueNotFound("");
                    }
                    catch (Exception ex)
                    {
                        addReport("Ошибка \"" + ex.Message + "\" | Страница: " + target);
                        addReport("========================================");
                    }
                }
                toolStripStatusLabel4.Text = "100%";
                addReport("Поиск завершен");
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

        /* Поиск по тексту */
        int _findIndex = 0;
        int _findLast = 0;
        String _findText = "";
        private void findText(RichTextBox _richTextBox, ToolStripComboBox _cbox)
        {
            try
            {
                bool resolution = true;
                for (int k = 0; k < _cbox.Items.Count; k++)
                    if (_cbox.Items[k].ToString() == _cbox.Text) resolution = false;
                if (resolution) _cbox.Items.Add(_cbox.Text);
                if (_findText != _cbox.Text)
                {
                    _findIndex = 0;
                    _findLast = 0;
                    _findText = _cbox.Text;
                }
                if (_richTextBox.Find(_cbox.Text, _findIndex, _richTextBox.TextLength - 1, RichTextBoxFinds.None) >= 0)
                {
                    _richTextBox.Select();
                    _findIndex = _richTextBox.SelectionStart + _richTextBox.SelectionLength;
                    if (_findLast == _richTextBox.SelectionStart)
                    {
                        MessageBox.Show("Поиск в списке результатов - завершен", "Сообщение");
                        _findIndex = 0;
                        _findLast = 0;
                        _findText = _cbox.Text;
                    }
                    else
                    {
                        _findLast = _richTextBox.SelectionStart;
                    }
                }
                else
                {
                    MessageBox.Show("Поиск в списке результатов - завершен", "Сообщение");
                    _findIndex = 0;
                    _findLast = 0;
                    _findText = _cbox.Text;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void loadListValues(ListBox listBox)
        {
            try
            {
                
                openFileDialog2.FileName = "";
                if (openFileDialog2.ShowDialog() == DialogResult.OK)
                {
                    listBox.Items.Clear();
                    string content = readFile(openFileDialog2.FileName);
                    string[] text = content.Split('\n');
                    foreach (string line in text)
                    {
                        listBox.Items.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void saveListValues(ListBox listBox)
        {
            try
            {
                int count = listBox.Items.Count;
                if(count > 0)
                {
                    string text = "";
                    for(int i = 0; i < count; i++)
                    {
                        text += listBox.Items[i].ToString();
                        if (i != (count - 1)) text += Environment.NewLine;
                    }

                    saveFileDialog1.FileName = "";
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        writeFile(text, saveFileDialog1.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
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

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            try
            {
                findText(richTextBoxReport, toolStripComboBoxFind);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            try
            {
                saveFileDialog1.FileName = "";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    //richTextBoxReport.SaveFile(saveFileDialog1.FileName, RichTextBoxStreamType.PlainText);
                    writeFile(richTextBoxReport.Text, saveFileDialog1.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void сохранитьРезультатПоискаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                saveFileDialog1.FileName = "";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    //richTextBoxReport.SaveFile(saveFileDialog1.FileName, RichTextBoxStreamType.PlainText);
                    writeFile(richTextBoxReport.Text, saveFileDialog1.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void списокСтраницНаКоторыхЗначенияБылиНайденыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                saveFileDialog1.FileName = "";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    //richTextBoxValueFound.SaveFile(saveFileDialog1.FileName, RichTextBoxStreamType.PlainText);
                    writeFile(richTextBoxValueFound.Text, saveFileDialog1.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void списокСтраницНаКоторыхЗначенияНеБылиНайденыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                saveFileDialog1.FileName = "";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    //richTextBoxValueNotFound.SaveFile(saveFileDialog1.FileName, RichTextBoxStreamType.PlainText);
                    writeFile(richTextBoxValueNotFound.Text, saveFileDialog1.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            FormAbout about = new FormAbout();
            about.ShowDialog();
        }

        private void закрытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void toolStripButton15_Click(object sender, EventArgs e)
        {
            loadListValues(listBoxValues);
        }

        private void toolStripButton16_Click(object sender, EventArgs e)
        {
            saveListValues(listBoxValues);
        }

        private void toolStripButton17_Click(object sender, EventArgs e)
        {
            loadListValues(listBoxValuesXPath);
        }

        private void toolStripButton18_Click(object sender, EventArgs e)
        {
            saveListValues(listBoxValuesXPath);
        }

        private void toolStripButton19_Click(object sender, EventArgs e)
        {
            loadListValues(listBoxValuesCSS);
        }

        private void toolStripButton20_Click(object sender, EventArgs e)
        {
            saveListValues(listBoxValuesCSS);
        }
    }
}
