using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace DiscordTimeLauncher
{

    public partial class Form1 : Form
    {
        IniFile ConfigIni;
        public static string HomeDir = @".\";
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Читаем конфиги, ничего особенного
            ConfigIni = new IniFile(HomeDir + @"data\config.ini");
            string[] Profiles = Directory.GetFiles(HomeDir + @"data\Profile", "*.pack");
            foreach(string Profile in Profiles)
            {
                Packets_comboBox.Items.Add(Path.GetFileName(Profile));
            }
            Packets_comboBox.SelectedItem = ConfigIni.Read("Packets_comboBox", "launcher");

            string[] Patchs = Directory.GetFiles(HomeDir + @"data\Patch", "*.zip");
            foreach (string lPath in Patchs)
            {
                Patchs_comboBox.Items.Add(Path.GetFileName(lPath));
            }
            Patchs_comboBox.SelectedItem = ConfigIni.Read("Patchs_comboBox", "launcher");

            string[] inis = Directory.GetFiles(HomeDir + @"data\Config", "*.ini");
            foreach (string ini in inis)
            {
                Config_comboBox.Items.Add(Path.GetFileName(ini));
            }
            Config_comboBox.SelectedItem = ConfigIni.Read("Config_comboBox", "launcher");

            string[] maps = Directory.GetFiles(HomeDir + @"data\user_maps_build", "*.zip");
            foreach (string map in maps)
            {
                MAPS_comboBox.Items.Add(Path.GetFileName(map));
            }
            MAPS_comboBox.SelectedItem = ConfigIni.Read("MAPS_comboBox", "launcher");
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            await Task.Factory.StartNew(() =>
            {
                //Проверка на лицензию, возможно будет работать даже с установлеными легендами
                if ((RegFinder.RegFind(Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"), "aterdux.com") == null) && (RegFinder.RegFind(Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall"), "aterdux.com") == null))
                {
                    MessageBox.Show("Error: license version not install");
                    Application.Exit();
                }
            });

            string build_data = Packets_comboBox.Items[Packets_comboBox.SelectedIndex].ToString() + Config_comboBox.Items[Config_comboBox.SelectedIndex].ToString() + checkBox1.Checked.ToString() + checkBox2.Checked.ToString() + MAPS_comboBox.Items[MAPS_comboBox.SelectedIndex].ToString() + Patchs_comboBox.Items[Patchs_comboBox.SelectedIndex].ToString();
            //Читаем старый хеш с конфигов для сравнения с новым
            string last_build = ConfigIni.Read("last_build_hash", "launcher");
            //Считаем хеш по данным конфигурации что бы дважны не распаковывать одинаковые сборки
            //При изменении хоть одного параметра, старая сборка удалится, новая распакуется с нуля
            string this_build = GetHash(build_data);

            if(last_build != this_build)
            {
                await Task.Factory.StartNew(() =>
                {
                    ConfigIni.Write("last_build_hash", this_build, "launcher");
                    if ((Directory.Exists(HomeDir + @"game\saves")) && (Directory.Exists(HomeDir + @"data\saves")))
                        FileExtensions.CopyDir(HomeDir + @"game\saves", HomeDir + @"data\saves", true);

                    if (File.Exists(HomeDir + @"game\Rus_DiscordTimes.ini"))
                        File.Copy(HomeDir + @"game\Rus_DiscordTimes.ini", HomeDir + @"data\Config\" + Config_comboBox.Items[Config_comboBox.SelectedIndex], true);

                    if (Directory.Exists(HomeDir + @"game"))
                    {
                        Directory.Delete(HomeDir + @"game", true);
                        Directory.CreateDirectory(HomeDir + @"game");
                        Directory.CreateDirectory(HomeDir + @"game\saves");
                    }
                    else
                    {
                        Directory.CreateDirectory(HomeDir + @"game");
                        Directory.CreateDirectory(HomeDir + @"game\saves");
                    }


                    //Шифрование сборок, ключ публиковатся не будет
                    Crypto3 crypto3 = new Crypto3("");
                    crypto3.Decrypt(HomeDir + @"data\Profile\" + Packets_comboBox.Items[Packets_comboBox.SelectedIndex], HomeDir + @"data\Profile\" + Packets_comboBox.Items[Packets_comboBox.SelectedIndex] + ".zip");
                    ZipArchive tempzipArchive = ZipFile.Open(HomeDir + @"data\Profile\" + Packets_comboBox.Items[Packets_comboBox.SelectedIndex] + ".zip", ZipArchiveMode.Read, Encoding.GetEncoding(866));
                    ZipArchiveExtensions.ExtractToDirectory(tempzipArchive, HomeDir + @"game", true);
                    tempzipArchive.Dispose();
                    File.Delete(HomeDir + @"data\Profile\" + Packets_comboBox.Items[Packets_comboBox.SelectedIndex] + ".zip");

                    //Проверяем checkbox патча
                    if (checkBox1.Checked)
                        ZipArchiveExtensions.ExtractToDirectory(ZipFile.Open(HomeDir + @"data\Patch\" + Patchs_comboBox.Items[Patchs_comboBox.SelectedIndex], ZipArchiveMode.Read, Encoding.GetEncoding(866)), HomeDir + @"game", true);

                    //Проверяем checkbox сборок карт
                    if (checkBox2.Checked)
                        ZipArchiveExtensions.ExtractToDirectory(ZipFile.Open(HomeDir + @"data\user_maps_build\" + MAPS_comboBox.Items[MAPS_comboBox.SelectedIndex], ZipArchiveMode.Read, Encoding.GetEncoding(866)), HomeDir + @"game", true);

                    //Копируем сейвы из хранилища
                    FileExtensions.CopyDir(HomeDir + @"data\saves", HomeDir + @"game\saves", true);
                    File.Copy(HomeDir + @"data\Config\" + Config_comboBox.Items[Config_comboBox.SelectedIndex], HomeDir + @"game\Rus_DiscordTimes.ini", true);

                    //Запускаем из start.bat что бы запускать игру из папки игры 
                    File.Copy(HomeDir + @"data\start.bat", HomeDir + @"game\start.bat", true);
                    
                });
            }
            
            System.Diagnostics.Process.Start(HomeDir + @"game\start.bat", HomeDir + @"game\");
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
                Patchs_comboBox.Enabled = true;
            else
                Patchs_comboBox.Enabled = false;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            //Код отвечающий за перемещение окна из любой точки этого самого окна
            base.Capture = false;
            Message m = Message.Create(base.Handle, 0xa1, new IntPtr(2), IntPtr.Zero);
            this.WndProc(ref m);
        }

        private void PictureBox1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://aterdux.com/");
        }

        //Дальше перезапись конфигов сразу при изменении значений в лаунчере
        private void Packets_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConfigIni.Write("Packets_comboBox", Packets_comboBox.SelectedItem.ToString(), "launcher");
        }

        private void Config_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConfigIni.Write("Config_comboBox", Config_comboBox.SelectedItem.ToString(), "launcher");
        }

        private void Patchs_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConfigIni.Write("Patchs_comboBox", Patchs_comboBox.SelectedItem.ToString(), "launcher");
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
                MAPS_comboBox.Enabled = true;
            else
                MAPS_comboBox.Enabled = false;
        }

        private void MAPS_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConfigIni.Write("MAPS_comboBox", MAPS_comboBox.SelectedItem.ToString(), "launcher");
        }

        //Дефолтный мд5 для хеша сборки
        public string GetHash(string input)
        {
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

            return Convert.ToBase64String(hash);
        }

        private void Label1_Click(object sender, EventArgs e)
        {

        }
    }

    //Дальше код одолженый с инета, ничего сложного
    class Crypto3
    {
        private readonly int[] _key;
        private readonly int[] _inversedKey;

        public Crypto3(string key)
        {
            var indexPairs = key
                .Select((chr, idx1) => new { chr, idx1 })
                .OrderBy(arg => arg.chr)
                .Select((arg, idx2) =>
                    new
                    {
                        arg.idx1,
                        idx2
                    })
                .ToArray();

            _key = indexPairs
                .OrderBy(arg => arg.idx1)
                .Select(arg => arg.idx2)
                .ToArray();

            _inversedKey = indexPairs
                .OrderBy(arg => arg.idx2)
                .Select(arg => arg.idx1)
                .ToArray();
        }

        public void Encrypt(string sourceFile, string destinationFile)
        {
            EncryptDecrypt(sourceFile, destinationFile, _key);
        }

        public void Decrypt(string sourceFile, string destinationFile)
        {
            EncryptDecrypt(sourceFile, destinationFile, _inversedKey);
        }

        private static void EncryptDecrypt(string sourceFile, string destinationFile, int[] key)
        {
            var keyLength = key.Length;
            var buffer1 = new byte[keyLength];
            var buffer2 = new byte[keyLength];
            using (var source = new FileStream(sourceFile, FileMode.Open))
            using (var destination = new FileStream(destinationFile, FileMode.OpenOrCreate))
            {
                while (true)
                {
                    var read = source.Read(buffer1, 0, keyLength);
                    if (read == 0)
                    {
                        source.Close();
                        destination.Flush();
                        destination.Close();
                        return;
                    }
                    else if (read < keyLength)
                    {
                        for (int i = read; i < keyLength; i++)
                        {
                            buffer1[i] = (byte)' ';
                        }
                    }

                    for (var i = 0; i < keyLength; i++)
                    {
                        var idx = i / keyLength * keyLength + key[i % keyLength];
                        buffer2[idx] = buffer1[i];
                    }

                    destination.Write(buffer2, 0, keyLength);
                }
                
            }
        }
    }

    public static class ZipArchiveExtensions
    {

        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryName);
                archive = null;
                return;
            }
            foreach (ZipArchiveEntry file in archive.Entries)
            { 
                string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                if (file.Name == "")
                {// Assuming Empty for Directory
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    continue;
                }
                file.ExtractToFile(completeFileName, true);
            }
            archive = null;
        }


    }

    public static class FileExtensions
    {
        public static void CopyDir(string begin_dir, string end_dir)
        {
            DirectoryInfo dir_inf = new DirectoryInfo(begin_dir);
            foreach (DirectoryInfo dir in dir_inf.GetDirectories())
            {
                if (Directory.Exists(end_dir + "\\" + dir.Name) != true)
                {
                    Directory.CreateDirectory(end_dir + "\\" + dir.Name);
                }
                CopyDir(dir.FullName, end_dir + "\\" + dir.Name);
            }

            foreach (string file in Directory.GetFiles(begin_dir))
            {
                string filik = file.Substring(file.LastIndexOf('\\'), file.Length - file.LastIndexOf('\\'));
                File.Copy(file, end_dir + "\\" + filik, false);
            }
        }
        public static void CopyDir(string begin_dir, string end_dir, bool overwrite)
        {
            DirectoryInfo dir_inf = new DirectoryInfo(begin_dir);
            foreach (DirectoryInfo dir in dir_inf.GetDirectories())
            {
                if (Directory.Exists(end_dir + "\\" + dir.Name) != true)
                {
                    Directory.CreateDirectory(end_dir + "\\" + dir.Name);
                }
                CopyDir(dir.FullName, end_dir + "\\" + dir.Name, overwrite);
            }

            foreach (string file in Directory.GetFiles(begin_dir))
            {
                string filik = file.Substring(file.LastIndexOf('\\'), file.Length - file.LastIndexOf('\\'));
                File.Copy(file, end_dir + "\\" + filik, overwrite);
            }
        }
    }
}
