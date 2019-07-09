using MahApps.Metro.Controls;
using System;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace DR2Rallymaster
{
    // Displays a browser window with the Codemasters login page
    // Once the user logs in and closes the window, grab the authetication
    // cookies to use in the client we get the rest of the data with
    public partial class CodiesLoginWindow : MetroWindow
    {
        [DllImport("wininet.dll", SetLastError = true)]
        public static extern bool InternetGetCookieEx(
        string url,
        string cookieName,
        StringBuilder cookieData,
        ref int size,
        Int32 dwFlags,
        IntPtr lpReserved);

        private const Int32 InternetCookieHttponly = 0x2000;

        public CodiesLoginWindow()
        {
            InitializeComponent();
        }

        // when the browser closes, grab the cookie container and pass it back to the main window for reuse
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // get the cookie container from the WebBrowser before it closes
            var cookieContainer = GetUriCookieContainer(new Uri("https://dirtrally2.com"));

            ((MainWindow)Application.Current.MainWindow).SharedCookieContainer = cookieContainer;
        }

        // this gets the cookie container from the web browser control,
        // the cookie container will have the authentication data from the racenet login
        // from: https://stackoverflow.com/questions/3382498/is-it-possible-to-transfer-authentication-from-webbrowser-to-webrequest
        public static CookieContainer GetUriCookieContainer(Uri uri)
        {
            CookieContainer cookies = null;
            // Determine the size of the cookie
            int datasize = 8192 * 16;
            StringBuilder cookieData = new StringBuilder(datasize);
            if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, InternetCookieHttponly, IntPtr.Zero))
            {
                if (datasize < 0)
                    return null;
                // Allocate stringbuilder large enough to hold the cookie
                cookieData = new StringBuilder(datasize);
                if (!InternetGetCookieEx(
                    uri.ToString(),
                    null, cookieData,
                    ref datasize,
                    InternetCookieHttponly,
                    IntPtr.Zero))
                    return null;
            }
            if (cookieData.Length > 0)
            {
                cookies = new CookieContainer();
                cookies.SetCookies(uri, cookieData.ToString().Replace(';', ','));
            }
            return cookies;
        }

        // uses reflection to suppress the script errors on the page
        // from https://stackoverflow.com/questions/6138199/wpf-webbrowser-control-how-to-suppress-script-errors/18289217#18289217
        private void RacenetLoginBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            dynamic activeX = this.racenetLoginBrowser.GetType().InvokeMember("ActiveXInstance",
                BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, this.racenetLoginBrowser, new object[] { });

            activeX.Silent = true;
        }
    }
}
