using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace SearchSiteContent
{
    public partial class FormBrowser : Form
    {
        public const string BY_CSS = "BY_CSS";
        public const string BY_XPATH = "BY_XPATH";

        public FormSearchSiteContent Parent;
        private int linkIndex = 0;
        private string page = "";
        private int index = 0;
        private int totalPages = 0;
        private int onePercent = 0;
        private int percent = 0;
        private bool found = false;
        private bool notfound = false;

        public FormBrowser()
        {
            InitializeComponent();
        }

        private void FormBrowser_Load(object sender, EventArgs e)
        {
            
        }

        private void FormBrowser_FormClosed(object sender, FormClosedEventArgs e)
        {
            webView2.CoreWebView2.Stop();
            Parent.Browser = null;
        }

        private void webView21_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            webView2.EnsureCoreWebView2Async();
            webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.setIgnoreCertificateErrors", "{\"ignore\": true}");
            webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.enable", "{}");
            webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.clearBrowserCache", "{}");
            webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.setCacheDisabled", @"{""cacheDisabled"":true}");
            if (Parent != null)
            {
                if (Parent.checkBoxUserAgent.Checked == false) webView2.CoreWebView2.Settings.UserAgent = Parent.textBoxUserAgent.Text;
                
            }
            webView2.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
        }

        private async void webView2_ContentLoading(object sender, Microsoft.Web.WebView2.Core.CoreWebView2ContentLoadingEventArgs e)
        {
            
        }

        private async void webView2_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                if (Parent.Browser == null) return;

                index++;
                Parent.toolStripStatusLabel3.Text = "Процесс: " + index.ToString() + "/" + totalPages.ToString();
                Parent.toolStripProgressBar1.Value = index;

                percent = (int)(((double)Parent.toolStripProgressBar1.Value / (double)Parent.toolStripProgressBar1.Maximum) * 100);
                Parent.toolStripStatusLabel4.Text = Convert.ToString(percent) + "%";


                message.Text = "[" + (linkIndex + 1).ToString() + "/" + Parent.textBoxLinks.Lines.Length.ToString() + "] Загрузка завершена |";
                link.Text = webView2.Source.ToString();

                Parent.addReport("Страница: " + link.Text);
                found = false;
                notfound = false;

                if (Parent.listBoxValuesXPath.Items.Count > 0)
                {
                    foreach(string xpath in Parent.listBoxValuesXPath.Items)
                    {
                        if (xpath == "") continue;
                        if (await SearchContentAsync(FormBrowser.BY_XPATH, xpath) == true)
                        {
                            found = true;
                            Parent.addReport("+ Найдено значение: " + xpath);
                            Parent.addValueFound("Значение [" + xpath + "]: " + link.Text + " - найдено");
                        }
                        else
                        {
                            notfound = true;
                            Parent.addReport("- Не найдено значение: " + xpath);
                            Parent.addValueNotFound("Значение [" + xpath + "]: " + link.Text + " - не найдено");
                        }
                    }
                }

                if (Parent.listBoxValuesCSS.Items.Count > 0)
                {
                    foreach (string css in Parent.listBoxValuesCSS.Items)
                    {
                        if (css == "") continue;
                        if (await SearchContentAsync(FormBrowser.BY_CSS, css) == true)
                        {
                            found = true;
                            Parent.addReport("+ Найдено значение: " + css);
                            Parent.addValueFound("Значение [" + css + "]: " + link.Text + " - найдено");
                        }
                        else
                        {
                            notfound = true;
                            Parent.addReport("- Не найдено значение: " + css);
                            Parent.addValueNotFound("Значение [" + css + "]: " + link.Text + " - не найдено");
                        }
                    }
                }

                Parent.addReport("========================================");
                if (found == true) Parent.addValueFound("");
                if (notfound == true) Parent.addValueNotFound("");

                linkIndex++;
                if ((Parent.textBoxLinks.Lines.Length - 1) < linkIndex)
                {
                    Parent.addReport("Поиск завершен");
                    this.Close();
                }
                else
                {
                    message.Text = "[" + (linkIndex + 1).ToString() + "/" + Parent.textBoxLinks.Lines.Length.ToString() + "] Идет загрузка, подождите... |";
                    link.Text = Parent.textBoxLinks.Lines[linkIndex];
                    webView2.CoreWebView2.Navigate(Parent.textBoxLinks.Lines[linkIndex]);
                }
            }
            catch (Exception ex)
            {
                Parent.addReport("Ошибка: " + ex.Message + " | Страница: " + link.Text);
                Parent.addReport("========================================");
            }
        }

        public void StartSearch()
        {
            try
            {
                linkIndex = 0;
                if (Parent != null)
                {
                    page = "";
                    index = 0;
                    totalPages = Parent.textBoxLinks.Lines.Length;
                    onePercent = 0;
                    Parent.toolStripStatusLabel3.Text = "Процесс: 0/" + totalPages;
                    Parent.toolStripProgressBar1.Maximum = totalPages;
                    percent = 0;

                    if (Parent.textBoxLinks.Lines.Length > 0)
                    {
                        Parent.addReport("Запущен продвинутый поиск");
                        webView2.Source = new Uri(Parent.textBoxLinks.Lines[linkIndex]);
                    }
                    else
                    {
                        Parent.addReport("Нет ссылок для поиска");
                        MessageBox.Show("Нет ссылок для поиска", "Сообщение");
                        Close();
                    }
                }
                else
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                if (Parent != null) Parent.addReport("Ошибка: " + ex.Message);
            }
        }


        public async Task<bool> SearchContentAsync(string by, string locator)
        {
            bool found = false;
            try
            {
                string script = "";
                script += "(function(){ ";
                if (by == BY_CSS) script += $"var elem = document.querySelector(\"{locator}\");";
                else if (by == BY_XPATH) script += $"var elem = document.evaluate(\"{locator}\", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;";
                script += "return elem.innerHTML;";
                script += "}());";

                string result = await webView2.CoreWebView2.ExecuteScriptAsync(script);
                if (result != "null" && result != null) found = true;
            }
            catch (Exception ex)
            {
                Parent.addReport("Ошибка: " + ex.Message + " | Страница: " + webView2.Source.ToString());
            }
            return found;
        }

        private void остановитьЗагрузкуСтраницыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                webView2.CoreWebView2.Stop();
                Parent.addReport("Остановлена загрузка страницы: " + link.Text);
            }
            catch (Exception ex)
            {
                Parent.addReport("Ошибка: " + ex.Message);
            }
        }
    }
}
