using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace starter
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            //System.Net.WebClient wc = new System.Net.WebClient();
            //wc.DownloadFile(url,"hoge.dll");
            //wc.Dispose();

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "raballo.exe";

            Process.Start(psi);
        }
    }
}
