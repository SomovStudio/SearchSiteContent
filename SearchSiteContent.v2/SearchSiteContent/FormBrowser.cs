using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SearchSiteContent
{
    public partial class FormBrowser : Form
    {
        public const string BY_CSS = "BY_CSS";
        public const string BY_XPATH = "BY_XPATH";

        public string UserAgent = "";
        public RichTextBox Report;

        private bool statusPageLoadCompleted = false; // false - не загружена, true - загрузка завершена

        public FormBrowser()
        {
            InitializeComponent();
        }

        private void FormBrowser_Load(object sender, EventArgs e)
        {

        }

        private void webView21_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            webView2.EnsureCoreWebView2Async();
            webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.setIgnoreCertificateErrors", "{\"ignore\": true}");
            webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.enable", "{}");
            webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.clearBrowserCache", "{}");
            webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.setCacheDisabled", @"{""cacheDisabled"":true}");
            if (UserAgent != "") webView2.CoreWebView2.Settings.UserAgent = UserAgent;
            webView2.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
        }

        /*
        public async Task<bool> SearchContent()
        {
            string script =
                @"(function(){
                var performance = window.performance || window.mozPerformance || window.msPerformance || window.webkitPerformance || {};
                var network = performance.getEntriesByType('resource') || {};
                var result = JSON.stringify(network);
                return result;
                }());";
            string result = await webView2.CoreWebView2.ExecuteScriptAsync(script);
            return true;
        }
        */

        public void OpenPage(string url)
        {
            try
            {
                statusPageLoadCompleted = false;
                //if(webView2.Source == null) webView2.Source = new Uri(url);
                //else webView2.CoreWebView2.Navigate(url);
                webView2.Source = new Uri(url);
            }
            catch (Exception ex)
            {
                Report.Text = "Ошибка: " + ex.Message + " | Страница: " + webView2.Source.ToString() + Environment.NewLine + Report.Text;
            }
        }

        public async Task<bool> SearchContentAsync(string by, string locator)
        {
            bool found = false;
            try
            {
                for(int i = 0; i < 30; i++)
                {
                    await Task.Delay(1000);
                    if (statusPageLoadCompleted == true) break;
                }

                if (statusPageLoadCompleted == false)
                {
                    Report.Text = "Неудалось загрузить страницу: " + webView2.Source.ToString() + Environment.NewLine + Report.Text;
                    return false;
                }

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
                Report.Text = "Ошибка: " + ex.Message + " | Страница: " + webView2.Source.ToString() + Environment.NewLine + Report.Text;
            }
            return found;
        }

        private void webView2_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {

        }
    }
}
