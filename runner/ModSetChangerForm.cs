using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace claes
{
    public partial class ModSetChangerForm : Form
    {
        private const String defaulturl = "https://raw.githubusercontent.com/matanki-saito/modswitcher/master/set.default.yml";

        private ModSets modsets;
        private List<string> installedMods;

        private Process process;

        private MyComputerConfiguration config;

        private SettingsTxt settingsTxt;

        private FileSystemWatcher currentModDirectoryObserver = null;

        public ModSetChangerForm()
        {
            InitializeComponent();
            OptionalInitilaize();
        }

        private bool RunExe(bool launcherSkip, bool launcherHidden)
        {
            if (process != null) return false;

            // https://dobon.net/vb/dotnet/process/shell.html
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = config.GameExePath
            };

            if (launcherSkip)
            {
                psi.Arguments = "-skiplauncher";
            }

            if (launcherHidden)
            {
                psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
            }

            process = Process.Start(psi);
            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;

            return true;
        }

        // プロセスの終了を捕捉する Exited イベントハンドラ
        private void Process_Exited(object sender, EventArgs e)
        {
            process = null;
        }

        private void ShutDown()
        {
            if (process == null) return;
            process.CloseMainWindow();
            process.WaitForExit();
            process = null;
        }

        private void OptionalInitilaize()
        {
            config = new MyComputerConfiguration("Europa Universalis IV", 236850, "eu4.exe");

            LoadModSets();
            SetUpCombox();

            EnumInstalledMods();

            // Modフォルダの監視を開始する
            SetModDirectoryObserver();

            settingsTxt = new SettingsTxt(config.SettingsTxtFile);
            settingsTxt.Load();
        }

        private void SetUpCombox()
        {
            comboBox1.Items.Add("そのまま"); // 0
            foreach (var item in modsets.sets)
            {
                comboBox1.Items.Add(item.name);
            }
            comboBox1.SelectedIndex = 0;
        }

        private void EnumInstalledMods()
        {
            var di = new DirectoryInfo(config.ModDirectory);
            var files = di.GetFiles("*.mod", SearchOption.TopDirectoryOnly);

            installedMods = new List<string>();
            foreach (var fileInfo in files)
            {
                installedMods.Add(fileInfo.Name);
            }
        }

        private void LoadModSets()
        {
            modsets = LoadDefaultModSets();
            if (config.ModsetDirectory != null)
            {
                var di = new DirectoryInfo(config.ModsetDirectory);
                var files = di.GetFiles("*.yml", SearchOption.TopDirectoryOnly);
                var deserializer = new DeserializerBuilder().Build();
                foreach (var fileInfo in files)
                {
                    using (var text = new StreamReader(fileInfo.FullName))
                    {
                        modsets.sets.Add(deserializer.Deserialize<ModSet>(text));
                    }
                }
            }
        }

        private ModSets LoadDefaultModSets()
        {
            try
            {
                // gistにあるデフォルトのmodsetsを読み込む
                System.Net.WebClient wc = new System.Net.WebClient();
                wc.Encoding = System.Text.Encoding.UTF8;
                string loadtext = wc.DownloadString(defaulturl);
                wc.Dispose();

                // デシリアライズする
                var deserializer = new DeserializerBuilder().Build();
                return deserializer.Deserialize<ModSets>(loadtext);
            }catch(Exception e)
            {
                Console.WriteLine(e);
                return new ModSets();
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Restart_Exe(false,false);
        }

        private void Restart_Exe(bool launcherSkip, bool launcherHidden)
        {
            // 0は特別。何もしない
            if (comboBox1.SelectedIndex != 0 )
            {
                // 全てのチェックがオンなことを確認する
                if (launcherSkip)
                {
                    for (var i = 0; i < checkedListBox1.Items.Count; i++)
                    {
                        if (!checkedListBox1.GetItemChecked(i))
                        {
                            MessageBox.Show("全てのチェックが付いている必要があります");
                            return;
                        }
                    }
                }

                // 必要なModリストを作る
                var requiredModFilePaths = new List<string>();
                foreach (var item in modsets.sets[comboBox1.SelectedIndex - 1].required)
                {
                    requiredModFilePaths.Add(item.file);
                }
                settingsTxt.ActiveMods = requiredModFilePaths;

                // 更新
                ShutDown();
                settingsTxt.Update();
            }

            RunExe(launcherSkip, launcherHidden);
        }


        private void ComboBox1_ItemChecked(object sender, ItemCheckEventArgs e)
        {
            Console.WriteLine(e);

            if (e.CurrentValue == CheckState.Checked)
            {
                if (e.NewValue == CheckState.Checked)
                {
                    /* NOT CHANGED */
                }
                else
                {
                    // オフにすることはできません
                    e.NewValue = CheckState.Checked;
                }
            }else
            {
                // チェックが入りました
                if(e.NewValue == CheckState.Checked)
                {
                    var currentSet = modsets.sets[comboBox1.SelectedIndex-1];
                    var targetMod = currentSet.required[e.Index];
                    if (targetMod.s_id != 0)
                    {
                        // streamのページを開かせる
                        System.Diagnostics.Process.Start("https://steamcommunity.com/sharedfiles/filedetails/?l=japanese&id=" + targetMod.s_id);
                        //　オンにすることはここではできない
                        e.NewValue = CheckState.Unchecked;
                    } else
                    {
                        /* keyファイルを設置する */
                    }
                }
                else
                {
                    /* NOT CHANGED */
                }
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetModList();
        }

        private void SetModList()
        {
            // ModSetを切り替えたら表はリセットする
            checkedListBox1.Items.Clear();

            // index 0は特別扱い
            var index = comboBox1.SelectedIndex-1;
            if (index < 0)
            {
                return;
            }
            
            var requiredMods = modsets.sets[index].required;

            // URL開いてしまうので一時無効化する
            checkedListBox1.ItemCheck -= ComboBox1_ItemChecked;

            for (var i = 0; i < requiredMods.Count(); i++)
            {
                var item = requiredMods[i];

                checkedListBox1.Items.Add(item.name);
                var flag = false;
                if (installedMods.Contains(item.file))
                {
                    flag = true;
                }
                checkedListBox1.SetItemChecked(i, flag);

            }

            checkedListBox1.ItemCheck += ComboBox1_ItemChecked;
        }

        // https://dobon.net/vb/dotnet/file/filesystemwatcher.html
        private void SetModDirectoryObserver()
        {
            if (currentModDirectoryObserver != null)
            {
                currentModDirectoryObserver.EnableRaisingEvents = false;
                currentModDirectoryObserver.Dispose();
                currentModDirectoryObserver = null;
            }

            currentModDirectoryObserver = new System.IO.FileSystemWatcher
            {
                //監視するディレクトリを指定
                Path = config.ModDirectory,
                //ファイル、フォルダ名の変更を監視する
                NotifyFilter = (
                  System.IO.NotifyFilters.LastWrite
                | System.IO.NotifyFilters.FileName
                ),
                //すべてのファイルを監視
                Filter = "*.mod",
                SynchronizingObject = this
            };

            currentModDirectoryObserver.Created += ModDirectoryChangedEventHandler;
            currentModDirectoryObserver.Deleted += ModDirectoryChangedEventHandler;

            //監視を開始する
            currentModDirectoryObserver.EnableRaisingEvents = true;
        }

        private void ModDirectoryChangedEventHandler(Object source, FileSystemEventArgs e)
        {
            EnumInstalledMods();
            SetModList();
        }

        private void Button1_Click_1(object sender, EventArgs e)
        {
            Restart_Exe(true,true);
        }
    }
}
