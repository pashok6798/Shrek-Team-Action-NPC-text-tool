using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace ShrekNPCTextTool
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public struct Texts
        {
            public int str_sz;
            public int str_off;
            public string str;
        }

        public static void FillBytes(byte[] originalArray, byte with, int off)
        {
            if (off < originalArray.Length)
            {
                for (int i = off; i < originalArray.Length; i++)
                {
                    originalArray[i] = with;
                }
            }
        }

        public static uint pad_it(uint num, uint pad)
        {
            uint t;
            t = num % pad;

            if (Convert.ToBoolean(t)) num += pad - t;
            return (num);
        }

        private async void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog npc_fbd = new FolderBrowserDialog();
            npc_fbd.Description = "Выберите путь к NPC";

            FolderBrowserDialog txt_fbd = new FolderBrowserDialog();
            txt_fbd.Description = "Выберите путь для извлечения текста";

            if((npc_fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                 && (txt_fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK))
            {
                string InputDirPath = npc_fbd.SelectedPath;
                string OutputDirPath = txt_fbd.SelectedPath;

                DirectoryInfo di = new DirectoryInfo(InputDirPath);
                FileInfo[] fi = di.GetFiles("*.npc", SearchOption.AllDirectories);

                var ReportStatus = new Progress<string>(value => lb.Items.Add(value));
                int off = 0;
                byte[] tmp;

                if (fi.Length > 0)
                {
                    await Task.Run(() =>
                    {
                        for (int i = 0; i < fi.Length; i++)
                        {
                            FileStream fs = new FileStream(fi[i].FullName, FileMode.Open);
                            BinaryReader br = new BinaryReader(fs);
                            try
                            {
                                off = 0;
                                int str_count = br.ReadInt32();
                                int hz1 = br.ReadInt32();
                                int hz2 = br.ReadInt32();
                                int bl_size = br.ReadInt32();
                                off += (4 * 4);

                                Texts[] strs = new Texts[str_count];

                                for (int j = 0; j < str_count; j++)
                                {
                                    strs[j].str_sz = br.ReadInt32();
                                    strs[j].str_off = br.ReadInt32();
                                    off += 8;
                                    br.BaseStream.Seek(strs[j].str_off, SeekOrigin.Begin);
                                    tmp = br.ReadBytes(strs[j].str_sz);
                                    strs[j].str = Encoding.GetEncoding(1251).GetString(tmp);
                                    tmp = null;
                                    br.BaseStream.Seek(off, SeekOrigin.Begin);
                                }

                                br.Close();
                                fs.Close();

                                string NewFile = OutputDirPath + "\\" + fi[i].Name.ToLower().Replace(".npc", ".txt");

                                if (File.Exists(NewFile)) File.Delete(NewFile);

                                fs = new FileStream(NewFile, FileMode.CreateNew);
                                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

                                string str = "";

                                for (int j = 0; j < str_count; j++)
                                {
                                    tmp = Encoding.GetEncoding(1251).GetBytes(strs[j].str);
                                    tmp = Encoding.Convert(Encoding.GetEncoding(1251), Encoding.UTF8, tmp);
                                    str = Encoding.UTF8.GetString(tmp);

                                    if ((str.IndexOf("\n") >= 0) || (str.IndexOf("\r\n") >= 0))
                                    {
                                        if (str.IndexOf("\r\n") >= 0) str = str.Replace("\r\n", "\\r\\n");
                                        else str = str.Replace("\n", "\\n");
                                    }

                                    if (j + 1 < str_count) str += "\r\n";

                                    sw.Write(str);
                                }

                                sw.Close();
                                fs.Close();

                                ((IProgress<string>)ReportStatus).Report("Файл " + fi[i].FullName + " успешно извлечён");
                            }
                            catch
                            {
                                if (br != null) br.Close();
                                if (fs != null) fs.Close();

                                ((IProgress<string>)ReportStatus).Report("Что-то пошло не так. Пришлите мне файл " + fi[i].Name);
                            }
                        }
                    });
                }
            }
        }

        private async void ImportBtn_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog npc_fbd = new FolderBrowserDialog();
            npc_fbd.Description = "Выберите путь к NPC файлам";

            FolderBrowserDialog txt_fbd = new FolderBrowserDialog();
            txt_fbd.Description = "Выберите путь к текстовым файлам с переводом";

            if ((npc_fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                 && (txt_fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK))
            {
                string InputDirPath = npc_fbd.SelectedPath;
                string OutputDirPath = txt_fbd.SelectedPath;

                DirectoryInfo di = new DirectoryInfo(InputDirPath);
                FileInfo[] fi = di.GetFiles("*.npc", SearchOption.AllDirectories);

                DirectoryInfo di2 = new DirectoryInfo(OutputDirPath);
                FileInfo[] fi2 = di2.GetFiles("*.txt", SearchOption.AllDirectories);

                var ReportStatus = new Progress<string>(value => lb.Items.Add(value));

                byte[] end_block = null;
                byte[] tmp;
                int off = 0;

                if (fi.Length > 0 && fi2.Length > 0)
                {
                    await Task.Run(() =>
                    {
                        for (int i = 0; i < fi.Length; i++)
                        {
                            for(int j = 0; j < fi2.Length; j++)
                            {
                                if(fi[i].Name.Remove(fi[i].Name.Length - 4, 4) == fi2[j].Name.Remove(fi2[j].Name.Length - 4, 4))
                                {
                                    FileStream fs = new FileStream(fi[i].FullName, FileMode.Open);
                                    BinaryReader br = new BinaryReader(fs);
                                    
                                    try
                                    {
                                        off = 0;
                                        int str_count = br.ReadInt32();
                                        int hz1 = br.ReadInt32();
                                        int hz2 = br.ReadInt32();
                                        int bl_size = br.ReadInt32();
                                        off += (4 * 4);

                                        //Texts[] strs = new Texts[str_count];
                                        br.BaseStream.Seek(bl_size, SeekOrigin.Begin);
                                        end_block = br.ReadBytes((int)fi[i].Length - bl_size);


                                        br.Close();
                                        fs.Close();

                                        string[] strs = File.ReadAllLines(fi2[j].FullName, Encoding.UTF8);

                                        off = 16 + (8 * str_count);
                                        int len = off;

                                        byte[] block = new byte[off];
                                        tmp = BitConverter.GetBytes(str_count);
                                        Array.Copy(tmp, 0, block, 0, tmp.Length);
                                        tmp = BitConverter.GetBytes(16);
                                        Array.Copy(tmp, 0, block, 4, tmp.Length);
                                        tmp = BitConverter.GetBytes(4);
                                        Array.Copy(tmp, 0, block, 8, tmp.Length);

                                        int bl_off = 16;

                                        if (strs.Length == str_count)
                                        {
                                            Texts[] im_strs = new Texts[str_count];

                                            for(int k = 0; k < im_strs.Length; k++)
                                            {
                                                im_strs[k].str_off = off;
                                                im_strs[k].str = strs[k];
                                                tmp = Encoding.UTF8.GetBytes(im_strs[k].str);
                                                tmp = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(1251), tmp);
                                                im_strs[k].str = Encoding.GetEncoding(1251).GetString(tmp) + "\0";
                                                im_strs[k].str_sz = im_strs[k].str.Length - 1;
                                                off += (int)pad_it((uint)im_strs[k].str_sz + 1, 4);
                                                len += (int)pad_it((uint)im_strs[k].str_sz + 1, 4);

                                                tmp = BitConverter.GetBytes(im_strs[k].str_sz);
                                                Array.Copy(tmp, 0, block, bl_off, tmp.Length);
                                                tmp = BitConverter.GetBytes(im_strs[k].str_off);
                                                Array.Copy(tmp, 0, block, bl_off + 4, tmp.Length);
                                                bl_off += 8;
                                            }

                                            tmp = BitConverter.GetBytes(len);
                                            Array.Copy(tmp, 0, block, 12, tmp.Length);

                                            if (File.Exists(fi[i].FullName)) File.Delete(fi[i].FullName);

                                            fs = new FileStream(fi[i].FullName, FileMode.CreateNew);
                                            BinaryWriter bw = new BinaryWriter(fs);
                                            bw.Write(block);
                                            for(int k = 0; k < im_strs.Length; k++)
                                            {
                                                tmp = Encoding.GetEncoding(1251).GetBytes(im_strs[k].str);
                                                byte[] tmp_block = new byte[pad_it((uint)im_strs[k].str_sz + 1, 4)];
                                                Array.Copy(tmp, 0, tmp_block, 0, tmp.Length);
                                                FillBytes(tmp_block, 0x55, tmp.Length);
                                                bw.Write(tmp_block);
                                            }

                                            bw.Write(end_block);
                                            bw.Close();
                                            fs.Close();

                                            ((IProgress<string>)ReportStatus).Report("Файл " + fi[i].FullName + " успешно модифицирован");
                                        }
                                        else ((IProgress<string>)ReportStatus).Report("В файле " + fi[i].FullName + " не соответствует количество строк");
                                    }
                                    catch
                                    {
                                        if (br != null) br.Close();
                                        if (fs != null) fs.Close();

                                        ((IProgress<string>)ReportStatus).Report("Что-то пошло не так. Пришлите мне файл " + fi[i].Name);
                                    }
                                }
                            }
                        }
                    });
                }
            }
        }
    }
}
