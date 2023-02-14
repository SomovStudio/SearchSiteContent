using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
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

        public FormBrowser()
        {
            InitializeComponent();
        }

        private void FormBrowser_Load(object sender, EventArgs e)
        {
            
        }

        private void FormBrowser_FormClosed(object sender, FormClosedEventArgs e)
        {
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

        private async void webView2_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                link.Text = webView2.Source.ToString();

                if (Parent.listBoxValuesXPath.Items.Count > 0)
                {
                    foreach(string xpath in Parent.listBoxValuesXPath.Items)
                    {
                        if (xpath == "") continue;
                        if (await SearchContentAsync(FormBrowser.BY_XPATH, xpath) == true)
                        {
                            Parent.addReport("Найдено значение [" + xpath + "] на странице [" + link.Text + "]");
                            Parent.addValueFound("Значение [" + xpath + "]: " + link.Text + " - найдено");
                        }
                        else
                        {
                            Parent.addReport("Не найдено значение [" + xpath + "] на странице [" + link.Text + "]");
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
                            Parent.addReport("Найдено значение [" + css + "] на странице [" + link.Text + "]");
                            Parent.addValueFound("Значение [" + css + "]: " + link.Text + " - найдено");
                        }
                        else
                        {
                            Parent.addReport("Не найдено значение [" + css + "] на странице [" + link.Text + "]");
                            Parent.addValueNotFound("Значение [" + css + "]: " + link.Text + " - не найдено");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Parent.addReport("Ошибка: " + ex.Message + " | Страница: " + webView2.Source.ToString());
            }

            await Task.Delay(1000);
            linkIndex++;
            if (Parent.textBoxLinks.Lines.Length < linkIndex)
            {
                Parent.addReport("Следубщая страница");
                webView2.CoreWebView2.Navigate(Parent.textBoxLinks.Lines[linkIndex]);
            }
            else
            {
                Parent.addReport(Parent.textBoxLinks.Lines.Length.ToString() + " < " + linkIndex.ToString());
                Parent.addReport("Поиск завершен");
                this.Close();
            }
        }

        public void StartSearch()
        {
            try
            {
                linkIndex = 0;
                if (Parent != null)
                {
                    if (Parent.textBoxLinks.Lines.Length > 0)
                    {
                        Parent.addReport("Запущен продвинутый поиск");
                        webView2.Source = new Uri(Parent.textBoxLinks.Lines[linkIndex]);
                        linkIndex++;
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

        
    }
}
