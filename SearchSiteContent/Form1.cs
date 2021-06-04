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

/* Регулярные выражения в C#
 * https://professorweb.ru/my/csharp/charp_theory/level4/4_10.php
 * Делегаты, события и лямбды Делегаты
 * https://metanit.com/sharp/tutorial/3.13.php
 */

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
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                toolStripComboBox1.Text = openFileDialog1.FileName;
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            consoleRichTextBox.Clear();
            toolStripStatusLabel3.Text = "...";
            sitemapPath = toolStripComboBox1.Text;
            searchValue = toolStripComboBox2.Text;
            thread = new Thread(runSearch);
            thread.Start();
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
                    ArrayList firstSitemap = readSitemap(page);
                    foreach (string urlFirstSitemap in firstSitemap)
                    {
                        if (urlFirstSitemap.Contains("sitemap") == true) sitemaps.Add(urlFirstSitemap);
                        else targets.Add(urlFirstSitemap);
                    }
                }
                else
                {
                    sitemaps.Add(sitemapPath);
                }


                /* собираю все url из собранных sitemap */
                foreach (string sitemap in sitemaps)
                {
                    string pageSitemap = getPageHtmlDOM(sitemap);
                    ArrayList listURLs = readSitemap(pageSitemap);

                    foreach (string url in listURLs)
                    {
                        targets.Add(url);
                    }
                }

                /* Выполняю поиск по всем собранным url */
                int index = 0;
                string totalPages = targets.Count.ToString();
                toolStripStatusLabel3.Text = "Процесс: 0/" + totalPages;
                foreach (string target in targets)
                {
                    index++;
                    toolStripStatusLabel3.Text = "Процесс: "+ index.ToString() +"/" + totalPages;
                    try
                    {
                        string pagetarget = getPageHtmlDOM(target);
                        if (searchContentOnPage(pagetarget, searchValue) == true)
                        {
                            consoleRichTextBox.Text = "Поиск значение в " + target + " - значение найдено" + Environment.NewLine + consoleRichTextBox.Text;
                            richTextBox1.Text = richTextBox1.Text + target + " - значение найдено" + Environment.NewLine;
                        }
                        else consoleRichTextBox.Text = "Поиск значение в " + target + " - значение не найдено" + Environment.NewLine + consoleRichTextBox.Text;
                    }
                    catch (Exception ex)
                    {
                        consoleRichTextBox.Text = "Поиск значение в " + target + " - " + ex.Message + Environment.NewLine + consoleRichTextBox.Text;
                    }
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
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

        private void endSearch()
        {
            MessageBox.Show("Поиск - завершен!");
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            endSearch();
            try
            {
                thread.Abort();
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        
    }
}
