using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Net;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;


namespace SearchSiteContent
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        public string sitemapPath;
        public string searchValue;

        private Thread thread;

        private ArrayList readSitemap(string sitemap)
        {
            ArrayList list = new ArrayList();
            Match match;
            string pattern = @"<loc>(\w+://[^<]+)";
            match = Regex.Match(sitemap, pattern);
            while (match.Success)
            {
                //consoleRichTextBox.Text = "Found href " + m.Groups[1] + " at " + m.Groups[1].Index + Environment.NewLine + consoleRichTextBox.Text ;
                list.Add(match.Groups[1].ToString());
                match = match.NextMatch();
            }
            return list;
        }

        private string getPageHtmlDOM(string page)
        {
            string html;
            using (WebClient web = new WebClient())
            {
                web.Encoding = Encoding.UTF8;
                html = web.DownloadString(page);
            }
            return html;
        }

        private bool checkThisIsSitemap(string page)
        {
            return page.Contains("www.sitemaps.org/schemas/sitemap/");
        }

        private bool searchContentOnPage(string page, string value)
        {
            return page.Contains(value);
        }

        private void addConsoleMessage(string message)
        {
            consoleRichTextBox.Text = message + Environment.NewLine + consoleRichTextBox.Text;
        }

        private void addResultMessage(string message)
        {
            resultRichTextBox.Text = resultRichTextBox.Text + message + Environment.NewLine;
        }

        private void addValueInComboBox(ToolStripComboBox _cbox)
        {
            bool resolution = true;
            for (int k = 0; k < _cbox.Items.Count; k++)
                if (_cbox.Items[k].ToString() == _cbox.Text) resolution = false;
            if (resolution) _cbox.Items.Add(_cbox.Text);
        }

        private void start()
        {
            consoleRichTextBox.Clear();
            resultRichTextBox.Clear();
            toolStripProgressBar1.Maximum = 0;
            toolStripProgressBar1.Value = 0;
            toolStripStatusLabel3.Text = "...";
            toolStripStatusLabel4.Text = "0%";
            addValueInComboBox(toolStripComboBox1);
            addValueInComboBox(toolStripComboBox2);
            sitemapPath = toolStripComboBox1.Text;
            searchValue = toolStripComboBox2.Text;
            addConsoleMessage("Поиск запущен");
            thread = new Thread(runSearch);
            thread.Start();
        }

        private void startSelenium()
        {
            consoleRichTextBox.Clear();
            resultRichTextBox.Clear();
            toolStripProgressBar1.Maximum = 0;
            toolStripProgressBar1.Value = 0;
            toolStripStatusLabel3.Text = "...";
            toolStripStatusLabel4.Text = "0%";
            addValueInComboBox(toolStripComboBox1);
            addValueInComboBox(toolStripComboBox2);
            sitemapPath = toolStripComboBox1.Text;
            searchValue = toolStripComboBox2.Text;
            addConsoleMessage("Поиск запущен");
            thread = new Thread(runSeleniumSearch);
            thread.Start();
        }

        private void stop()
        {
            addConsoleMessage("Поиск прерван пользователем");
            endSearch();
            try
            {
                thread.Abort();
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
                addConsoleMessage("Сообщение: " + error.Message);
            }
        }

        private void runSearch()
        {
            try
            {
                ArrayList targets;
                ArrayList sitemaps;
                sitemaps = new ArrayList();
                targets = new ArrayList();

                /* собираю все sitemap */
                string page = getPageHtmlDOM(sitemapPath);
                if (checkThisIsSitemap(page) == true)
                {
                    sitemaps.Add(sitemapPath);
                }
                else
                {
                    MessageBox.Show("Сообщение: вы не выбрали файл sitemap.xml");
                    addConsoleMessage("Сообщение: вы не выбрали файл sitemap.xml");
                    stop();
                    return;
                }
                    
                for(int i = 0; i < sitemaps.Count; i++)
                {
                    page = getPageHtmlDOM(sitemaps[i].ToString());
                    if (checkThisIsSitemap(page) == false) continue;
                    addConsoleMessage("Чтение данных из сайтмап: " + sitemaps[i].ToString());
                    ArrayList listSitemaps = readSitemap(page);
                    foreach (string urlSitemap in listSitemaps)
                    {
                        if (urlSitemap.Contains(".xml") == true)
                        {
                            sitemaps.Add(urlSitemap);
                            //addConsoleMessage("Чтение данных из сайтмап: " + urlSitemap);
                        }
                        else
                        {
                            targets.Add(urlSitemap);
                        }
                    }
                }

                /* Выполняю поиск по всем собранным url */
                int index = 0;
                int totalPages = targets.Count;
                int onePercent = 0;
                if (totalPages < 100) onePercent = (100 / totalPages);
                else onePercent = (totalPages / 100);
                toolStripStatusLabel3.Text = "Процесс: 0/" + totalPages;
                toolStripProgressBar1.Maximum = totalPages;
                foreach (string target in targets)
                {
                    index++;
                    toolStripStatusLabel3.Text = "Процесс: " + index.ToString() + "/" + totalPages.ToString();
                    toolStripProgressBar1.Value = index;

                    if (totalPages < 100 && onePercent > 0) toolStripStatusLabel4.Text = Convert.ToString(index * onePercent) + "%";
                    if (totalPages >= 100) toolStripStatusLabel4.Text = Convert.ToString(index / onePercent) + "%";

                    try
                    {
                        string pagetarget = getPageHtmlDOM(target);
                        if (searchContentOnPage(pagetarget, searchValue) == true)
                        {
                            addConsoleMessage("Поиск значение в " + target + " - значение найдено");
                            addResultMessage("Страница: " + target + " - значение найдено");
                        }
                        else addConsoleMessage("Поиск значение в " + target + " - значение не найдено");
                    }
                    catch (Exception ex)
                    {
                        addConsoleMessage("Поиск значение в " + target + " - " + ex.Message);
                    }
                }
                toolStripStatusLabel4.Text = "100%";
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
                addConsoleMessage("Сообщение: " + error.Message);
                //MessageBox.Show(error.ToString());
            }
            finally
            {
                endSearch();
                thread.Abort();
            }

            endSearch();
            thread.Abort();
        }

        private void readSeleniumXML()
        {
            IWebDriver driver = new ChromeDriver();
            try
            {
                driver.Manage().Window.Maximize();

                ArrayList targets;
                ArrayList sitemaps;
                sitemaps = new ArrayList();
                targets = new ArrayList();

                /* собираю все sitemap */
                string page = getPageHtmlDOM(sitemapPath);
                if (checkThisIsSitemap(page) == true) sitemaps.Add(sitemapPath);
                else
                {
                    MessageBox.Show("Сообщение: вы не выбрали файл sitemap.xml");
                    addConsoleMessage("Сообщение: вы не выбрали файл sitemap.xml");
                    stop();
                    return;
                }

                IList<IWebElement> elements;
                for (int i = 0; i < sitemaps.Count; i++)
                {
                    driver.Navigate().GoToUrl(sitemaps[i].ToString());
                    elements = driver.FindElements(By.XPath("//*[contains(text(), '.xml')]"));
                    foreach (IWebElement element in elements)
                    {
                        if (element.Text == "") continue;
                        if (element.Text.Contains(".xml") == true)
                        {
                            sitemaps.Add(element.Text);
                        }
                    }
                }

                /* собираю страницы из sitemap */
                int index = 0;
                int count = 0;
                toolStripStatusLabel3.Text = "Чтение данных из sitemap: ...";
                toolStripStatusLabel4.Text = "...";
                foreach (string sitemapUrl in sitemaps)
                {
                    driver.Navigate().GoToUrl(sitemapUrl);
                    elements = driver.FindElements(By.XPath("//*[contains(text(), 'http://') or contains(text(), 'https://')]"));
                    index = 0;
                    count = elements.Count;
                    toolStripProgressBar1.Value = index;
                    toolStripProgressBar1.Maximum = count;
                    toolStripStatusLabel3.Text = "Чтение данных из sitemap: " + index.ToString() + "/" + count.ToString();
                    foreach (IWebElement element in elements)
                    {
                        index++;
                        toolStripProgressBar1.Value = index;
                        toolStripStatusLabel3.Text = "Чтение данных из sitemap: " + index.ToString() + "/" + count.ToString();
                        if (element.Text == "") continue;
                        if (element.Text.Contains(".xml") == false && element.Text.Contains("www.sitemaps.org") == false)
                        {
                            targets.Add(element.Text);
                        }
                    }
                    addConsoleMessage("Получил данные из sitemap: " + sitemapUrl);
                }

                toolStripStatusLabel4.Text = "100%";
            }
            catch (Exception error)
            {
                //MessageBox.Show(error.Message);
                addConsoleMessage("Сообщение: " + error.Message);
                MessageBox.Show(error.ToString());
            }
            finally
            {
                endSearch();
                driver.Close();
                driver.Quit();
                thread.Abort();
            }

            endSearch();
            driver.Close();
            driver.Quit();
            thread.Abort();
        }

        private void runSeleniumSearch()
        {
            IWebDriver driver = new ChromeDriver();
            IList<IWebElement> elements;

            try
            {
                ArrayList targets;
                ArrayList sitemaps;
                sitemaps = new ArrayList();
                targets = new ArrayList();

                /* собираю все sitemap */
                string page = getPageHtmlDOM(sitemapPath);
                if (checkThisIsSitemap(page) == true)
                {
                    sitemaps.Add(sitemapPath);
                }
                else
                {
                    MessageBox.Show("Сообщение: вы не выбрали файл sitemap.xml");
                    addConsoleMessage("Сообщение: вы не выбрали файл sitemap.xml");
                    stop();
                    return;
                }

                for (int i = 0; i < sitemaps.Count; i++)
                {
                    page = getPageHtmlDOM(sitemaps[i].ToString());
                    if (checkThisIsSitemap(page) == false) continue;
                    addConsoleMessage("Чтение данных из сайтмап: " + sitemaps[i].ToString());
                    ArrayList listSitemaps = readSitemap(page);
                    foreach (string urlSitemap in listSitemaps)
                    {
                        if (urlSitemap.Contains(".xml") == true) sitemaps.Add(urlSitemap);
                        else targets.Add(urlSitemap);
                    }
                }

                /* Выполняю поиск по всем собранным url */
                driver.Manage().Window.Maximize();

                int index = 0;
                int totalPages = targets.Count;
                int onePercent = 0;
                if (totalPages < 100) onePercent = (100 / totalPages);
                else onePercent = (totalPages / 100);
                toolStripStatusLabel3.Text = "Процесс: 0/" + totalPages;
                toolStripProgressBar1.Maximum = totalPages;
                foreach (string target in targets)
                {
                    index++;
                    toolStripStatusLabel3.Text = "Процесс: " + index.ToString() + "/" + totalPages.ToString();
                    toolStripProgressBar1.Value = index;

                    if (totalPages < 100 && onePercent > 0) toolStripStatusLabel4.Text = Convert.ToString(index * onePercent) + "%";
                    if (totalPages >= 100) toolStripStatusLabel4.Text = Convert.ToString(index / onePercent) + "%";

                    try
                    {
                        elements = null;
                        driver.Navigate().GoToUrl(target);
                        elements = driver.FindElements(By.XPath(searchValue));

                        if(elements != null)
                        {
                            if(elements.Count > 0)
                            {
                                addConsoleMessage("Поиск значения на странице " + target + " - значение найдено");
                                addResultMessage("Страница: " + target + " - найдено " + elements.Count.ToString() + " значений");
                            }
                            else
                            {
                                addConsoleMessage("Поиск значения на странице " + target + " - значение не найдено");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        addConsoleMessage("Поиск значения на странице " + target + " - " + ex.Message);
                    }
                }

                toolStripStatusLabel4.Text = "100%";
            }
            catch (Exception error)
            {
                //MessageBox.Show(error.Message);
                addConsoleMessage("Сообщение: " + error.Message);
                MessageBox.Show(error.ToString());
            }
            finally
            {
                endSearch();
                driver.Close();
                driver.Quit();
                thread.Abort();
            }

            endSearch();
            driver.Close();
            driver.Quit();
            thread.Abort();
        }

        private void endSearch()
        {
            MessageBox.Show("Поиск завершен!");
            addConsoleMessage("Поиск завершен");
            if (resultRichTextBox.Text == "")
            {
                addResultMessage("На страницах заданное значение для поиска - не найдено.");
            }
        }

        

        private void openSitemapFile()
        {
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                toolStripComboBox1.Text = openFileDialog1.FileName;
            }
        }

        private void saveFile(bool resule = false, bool console = false)
        {
            try
            {
                saveFileDialog1.FileName = "";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    if (resule == true && console == false)
                    {
                        resultRichTextBox.SaveFile(saveFileDialog1.FileName, RichTextBoxStreamType.PlainText);
                    }
                    if (console == true && resule == false)
                    {
                        consoleRichTextBox.SaveFile(saveFileDialog1.FileName, RichTextBoxStreamType.PlainText);
                    }
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
                addConsoleMessage("Сообщение: " + error.Message);
            }
        }

        /* Поиск по тексту */
        int _findIndex = 0;
        int _findLast = 0;
        String _findText = "";
        private void findText(ToolStripComboBox _cbox)
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
                if (resultRichTextBox.Find(_cbox.Text, _findIndex, resultRichTextBox.TextLength - 1, RichTextBoxFinds.None) >= 0)
                {
                    resultRichTextBox.Select();
                    _findIndex = resultRichTextBox.SelectionStart + resultRichTextBox.SelectionLength;
                    if (_findLast == resultRichTextBox.SelectionStart)
                    {
                        addConsoleMessage("Поиск в списке результатов - завершен");
                        _findIndex = 0;
                        _findLast = 0;
                        _findText = _cbox.Text;
                    }
                    else
                    {
                        _findLast = resultRichTextBox.SelectionStart;
                    }
                }
                else
                {
                    addConsoleMessage("Поиск в списке результатов - завершен");
                    _findIndex = 0;
                    _findLast = 0;
                    _findText = _cbox.Text;
                }

            }
            catch (Exception ex)
            {
                addConsoleMessage("Сообщение: " + ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            thread = new Thread(runSearch);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                thread.Abort();
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
                addConsoleMessage("Сообщение: " + error.Message);
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            openSitemapFile();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            start();
        }
               

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            stop();
        }

        private void запуститьПоискToolStripMenuItem_Click(object sender, EventArgs e)
        {
            start();
        }

        private void остановитьПоискToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stop();
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }

        private void resultRichTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.LinkText);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void consoleRichTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.LinkText);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            saveFile(true);
        }

        private void открытьSitemapФайлToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openSitemapFile();
        }

        private void сохранитьЛогКонсолиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFile(false, true);
        }

        private void сохранитьРезультатПоискаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFile(true);
        }

        private void toolStripComboBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (e.KeyChar.GetHashCode().ToString() == "851981")
                {
                    findText(toolStripComboBox3);
                }
            }
            catch (Exception ex)
            {
                addConsoleMessage("Сообщение: " + ex.Message);
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            try
            {
                findText(toolStripComboBox3);
            }
            catch (Exception ex)
            {
                addConsoleMessage("Сообщение: " + ex.Message);
            }
        }

        private void закрытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void запуститьПоискЧерезSeleniumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startSelenium();   
            /*
            // https://testguild.com/selenium-webdriver-visual-studio/
            //IWebDriver driver = new ChromeDriver(@"C:\GIT\SearchSiteContent\SearchSiteContent\bin\Debug\");
            IWebDriver driver = new ChromeDriver();
            driver.Manage().Window.Maximize();
            driver.Navigate().GoToUrl("https://www.google.com/");

            IWebElement search = driver.FindElement(By.Name("q"));
            search.SendKeys("GeForce 1650");
            search.SendKeys(OpenQA.Selenium.Keys.Enter);

            IList<IWebElement> elements = driver.FindElements(By.ClassName("g"));
            if (elements.Count == 0) MessageBox.Show("FAILED");
            else MessageBox.Show("PASSED");

            driver.Close();
            driver.Quit();
            */
        }
    }
}
