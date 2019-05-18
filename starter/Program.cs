using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace starter
{
    static class Program
    {
        const string cdn = "https://d3mq2c18xv0s3o.cloudfront.net/api/v1/distribution/";

        const string repoId = "187329279";

        // key: これは適当にしている
        const string key = "c57f4c778b9cd22fa992ee866a102754";

        // runnerディレクトリ名
        const string runnerDirName = @"pcl";

        // cacheファイル名
        const string cacheFileName = @"c57f4c778b9cd22fa992ee866a102754.zip";

        // runner
        const string runnerName = "runner.exe";

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // ファイルがあればMD5を導出する
            string md5 = null;
            string runnerPath = Path.Combine(runnerDirName, cacheFileName);
            if (File.Exists(runnerPath))
            {
                md5 = CalculateMd5(runnerPath);
            }

            // リクエストURLを組み立てる
            System.Net.WebClient wc = new System.Net.WebClient();

            var nvc = new NameValueCollection();
            nvc.Add("phase", "prod");
            if(md5 != null)
            {
                nvc.Add("dll_md5", md5);
            }
            wc.QueryString = nvc;
            var uri = new Uri(cdn  + repoId + "/" + key);

            try
            {
                // zipをダウンロードする
                var tmp = Path.GetTempFileName();
                wc.DownloadFile(uri, tmp);

                // zipを展開する
                ExtractToDirectory(tmp, runnerDirName, true);

                var zipPath = Path.Combine(runnerDirName, cacheFileName);
                File.Move(tmp, zipPath);
            }
            catch (Exception e)
            {
                // 410　更新しなくても良い
                Console.WriteLine("410 gone");
                
            }
            wc.Dispose();

            // 起動する
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = Path.Combine(runnerDirName,runnerName);
            Process.Start(psi);
        }

        public static string CalculateMd5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static void ExtractToDirectory(string archivePath, string destinationDirectoryName, bool overwrite)
        {

            using (var archive = ZipFile.OpenRead(archivePath))
            {
                if (!overwrite)
                {
                    archive.ExtractToDirectory(destinationDirectoryName);
                    return;
                }
                foreach (var file in archive.Entries)
                {
                    var completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                    var directory = Path.GetDirectoryName(completeFileName);

                    if (!Directory.Exists(directory) && directory != null)
                        Directory.CreateDirectory(directory);

                    if (file.Name != "")
                    {
                        file.ExtractToFile(completeFileName, true);
                    }
                }
            }
        }
    }
}
