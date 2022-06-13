using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Win32;
using System.IO;

namespace ChoreTimingEditor
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

        string landbFolderPath;
        string ChoreFilePath;
        List<landbs.landb> LandbStrs = null;
        List<Chores.Chore> ChoreList = null;
        List<Chores.CameraChore> CamChoreList = null;
        float commonLength = 0.0f;
        int commonPos = 0;

        float commonTiming = 0.0f;
        float newCommonTiming = 0.0f;

        anm.animation[] anms;

        byte[] value = { 0x84, 0xF0, 0x0E, 0x83, 0x25, 0x01, 0x07, 0x6B }; //value<float> что-то там
        byte[] timeCRC64 = { 0x55, 0xD3, 0xDC, 0xA6, 0x0F, 0xA3, 0xD6, 0x4B }; //CRC64 слова "time"
        byte[] contributionCRC64 = { 0xF6, 0xB5, 0x7F, 0x6E, 0x58, 0x41, 0xD3, 0x9B }; //CRC64 слова "contribution"

        private int SearchBinText(FileStream fs, int pos, string pattern)
        {
            byte[] tmpPattern = Encoding.UTF8.GetBytes(pattern);
            byte[] tmp;

            while(pos <= fs.Length)
            {
                tmp = new byte[tmpPattern.Length];
                fs.Seek(pos, SeekOrigin.Begin);
                fs.Read(tmp, 0, tmp.Length);

                if (Encoding.UTF8.GetString(tmp).ToLower() == pattern) return pos;
                pos++;
            }

            return -1;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "CHORE file (*.chore) | *.chore";

            if (ofd.ShowDialog() == true && ((LandbStrs != null) && (LandbStrs.Count > 0)))
            {
                if (cam_cb.Items.Count > 0) cam_cb.Items.Clear();

                inputFile.Text = ofd.FileName;
                ChoreFilePath = ofd.FileName;
                ChoreList = new List<Chores.Chore>();
                CamChoreList = new List<Chores.CameraChore>();

                FileStream fs = new FileStream(ofd.FileName, FileMode.Open);
                BinaryReader br = new BinaryReader(fs);
                int tmp = -1;
                int poz = 16;
                br.BaseStream.Seek(poz, SeekOrigin.Begin);

                tmp = br.ReadInt32();
                poz += (tmp * 12) + 4;
                br.BaseStream.Seek(poz, SeekOrigin.Begin);
                tmp = br.ReadInt32();
                poz += tmp + 4;
                br.BaseStream.Seek(poz, SeekOrigin.Begin);
                commonLength = br.ReadSingle();
                commonPos = poz;

                int CurPos;
                byte[] tmpCheck;
                float tmpSingle;
                int tmpInt = -1;
                int tmpInt2 = -1;

                while (poz <= fs.Length)
                {
                    br.BaseStream.Seek(poz, SeekOrigin.Begin);
                    tmpCheck = br.ReadBytes(8);
                    
                    if(BitConverter.ToString(tmpCheck) == BitConverter.ToString(value))
                    {
                        br.BaseStream.Seek(12, SeekOrigin.Current);
                        tmpCheck = br.ReadBytes(8);
                        poz += 12;

                        if ((BitConverter.ToString(tmpCheck) == BitConverter.ToString(timeCRC64))
                            || (BitConverter.ToString(tmpCheck) == BitConverter.ToString(contributionCRC64)))
                        {
                            CurPos = poz + 0x24;
                            poz -= (0x55 + 8 + 12);
                            ChoreList.Add(new Chores.Chore());
                            br.BaseStream.Seek(poz, SeekOrigin.Begin);
                            ChoreList[ChoreList.Count - 1].someData = br.ReadBytes(8);
                            ChoreList[ChoreList.Count - 1].isLandb = LandbStrs.Any(s => s.someData.SequenceEqual(ChoreList[ChoreList.Count - 1].someData));
                            ChoreList[ChoreList.Count - 1].Pos = (int)br.BaseStream.Position;
                            ChoreList[ChoreList.Count - 1].phraseTime = br.ReadSingle();
                            ChoreList[ChoreList.Count - 1].time.hasTime = false;
                            poz += 0x30;
                            ChoreList[ChoreList.Count - 1].previousPhraseTime = ChoreList[ChoreList.Count - 1].phraseTime;
                            br.BaseStream.Seek(poz, SeekOrigin.Begin);
                            ChoreList[ChoreList.Count - 1].sizeBlock = br.ReadInt32();
                            tmpInt = poz + ChoreList[ChoreList.Count - 1].sizeBlock;

                            br.BaseStream.Seek(CurPos, SeekOrigin.Begin);

                            if(BitConverter.ToString(tmpCheck) == BitConverter.ToString(timeCRC64))
                            {
                                ChoreList[ChoreList.Count - 1].time.Pos = (int)br.BaseStream.Position;
                                ChoreList[ChoreList.Count - 1].time.begTime = br.ReadSingle();
                                br.BaseStream.Seek(9, SeekOrigin.Current);
                                ChoreList[ChoreList.Count - 1].time.endTime = br.ReadSingle();

                                ChoreList[ChoreList.Count - 1].contribution = null;
                                ChoreList[ChoreList.Count - 1].time.hasTime = true;

                                while (poz <= tmpInt)
                                {
                                    br.BaseStream.Seek(poz, SeekOrigin.Begin);
                                    tmpCheck = br.ReadBytes(8);

                                    if (BitConverter.ToString(tmpCheck) == BitConverter.ToString(contributionCRC64))
                                    {
                                        CurPos = poz + 0x18;
                                        br.BaseStream.Seek(CurPos, SeekOrigin.Begin);
                                        tmpInt2 = br.ReadInt32();
                                        ChoreList[ChoreList.Count - 1].contribution = new Chores.Contribution[tmpInt2 / 2];

                                        for (int k = 0; k < ChoreList[ChoreList.Count - 1].contribution.Length; k++)
                                        {
                                            ChoreList[ChoreList.Count - 1].contribution[k].Pos = (int)br.BaseStream.Position;
                                            ChoreList[ChoreList.Count - 1].contribution[k].begTime = br.ReadSingle();
                                            br.BaseStream.Seek(9, SeekOrigin.Current);
                                            ChoreList[ChoreList.Count - 1].contribution[k].endTime = br.ReadSingle();
                                            br.BaseStream.Seek(9, SeekOrigin.Current);
                                        }

                                        break;
                                    }

                                    poz++;
                                }
                            }
                            else
                            {
                                CurPos = poz + 0x18;
                                br.BaseStream.Seek(CurPos, SeekOrigin.Begin);
                                tmpInt2 = br.ReadInt32();
                                ChoreList[ChoreList.Count - 1].contribution = new Chores.Contribution[tmpInt2 / 2];

                                for (int k = 0; k < ChoreList[ChoreList.Count - 1].contribution.Length; k++)
                                {
                                    ChoreList[ChoreList.Count - 1].contribution[k].Pos = (int)br.BaseStream.Position;
                                    ChoreList[ChoreList.Count - 1].contribution[k].begTime = br.ReadSingle();
                                    br.BaseStream.Seek(9, SeekOrigin.Current);
                                    ChoreList[ChoreList.Count - 1].contribution[k].endTime = br.ReadSingle();
                                    br.BaseStream.Seek(9, SeekOrigin.Current);
                                }
                            }

                            br.BaseStream.Seek(tmpInt + 8, SeekOrigin.Begin);

                            ChoreList[ChoreList.Count - 1].info.addPos = tmpInt + 8;
                            ChoreList[ChoreList.Count - 1].info.beginTime = br.ReadSingle();
                            ChoreList[ChoreList.Count - 1].info.endTime = br.ReadSingle();

                            poz = tmpInt + 16;
                        }
                    }

                    poz++;
                }

                poz = 0;
                FileInfo fi = new FileInfo(ChoreFilePath);

                byte[] binLen = null;
                byte[] tmpContent = null;
                string tmpStr = "";

                while (true)
                {
                    poz = SearchBinText(fs, poz, fi.Name.ToLower() + ":cam_");
                    if (poz == -1) break;
                    binLen = new byte[4];
                    fs.Seek(poz - 4, SeekOrigin.Begin);
                    fs.Read(binLen, 0, binLen.Length);
                    tmpContent = new byte[BitConverter.ToInt32(binLen, 0)];
                    poz += BitConverter.ToInt32(binLen, 0);
                    fs.Read(tmpContent, 0, tmpContent.Length);
                    tmpStr = Encoding.UTF8.GetString(tmpContent);
                    binLen = new byte[4];
                    fs.Read(binLen, 0, binLen.Length);
                    tmpSingle = BitConverter.ToSingle(binLen, 0);
                    CamChoreList.Add(new Chores.CameraChore(poz, tmpStr, tmpSingle, tmpSingle));
                }

                br.Close();
                fs.Close();

                if(ChoreList.Count > 0)
                {
                    if (cb.Items.Count > 0) cb.Items.Clear();

                    ChoreList.Sort((x, y) => x.time.begTime.CompareTo(y.time.begTime));

                    for(int i = 0; i < ChoreList.Count; i++)
                    {
                        cb.Items.Add(BitConverter.ToString(ChoreList[i].someData));
                    }

                    cb.SelectedIndex = 0;
                    MessageBox.Show("Общее время: " + Convert.ToString(commonLength));
                }

                if(CamChoreList.Count > 0)
                {
                    for(int i = 0; i < CamChoreList.Count - 1; i++)
                    {
                        cam_cb.Items.Add(CamChoreList[i].CameraName);
                    }

                    CamChoreList.RemoveAt(CamChoreList.Count - 1);
                    cam_cb.SelectedIndex = 0;
                }


            }
        }

        private void landbFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();

            if(fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                landbFolderPath = fbd.SelectedPath;
                folderPath.Text = landbFolderPath;

                LandbStrs = new List<landbs.landb>();

                DirectoryInfo di = new DirectoryInfo(landbFolderPath);
                FileInfo[] fi = di.GetFiles("*.landb", SearchOption.AllDirectories);

                if(fi.Length > 0)
                {
                    for(int i = 0; i < fi.Length; i++)
                    {
                        landbs.ReadFiles(fi[i].FullName, ref LandbStrs);
                    }
                }
            }
        }

        private void cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            begTime.Text = "";
            endTime.Text = "";
            timePhrase.Text = "";

            if (ChoreList.Count > 0 && LandbStrs.Count > 0)
            {
                if ((cb.SelectedIndex != -1) && (ChoreList[cb.SelectedIndex].time.hasTime))
                {
                    begTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].time.begTime);
                    endTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].time.endTime);
                    timePhrase.Text = Convert.ToString(ChoreList[cb.SelectedIndex].phraseTime);
                    richText.Document.Blocks.Clear();
                    contribCB.Items.Clear();

                    if(ChoreList[cb.SelectedIndex].contribution != null)
                    {
                        for (int k = 0; k < ChoreList[cb.SelectedIndex].contribution.Length; k++)
                        {
                            contribCB.Items.Add(ChoreList[cb.SelectedIndex].contribution[k].Pos);
                        }

                        contribCB.SelectedIndex = 0;
                    }

                    //Need to think about correctly change naming button
                    changeBtn.Content = ChoreList[cb.SelectedIndex].isLandb ? "Change speech timing and another dialogs' timings" : "Change timing and speech's timing";
                    changeBtn.Width = ChoreList[cb.SelectedIndex].isLandb ? 380 : 280;
                    changeTimingBtn.IsEnabled = false;

                    if (ChoreList[cb.SelectedIndex].isLandb)//LandbStrs.Any(s => s.someData.SequenceEqual(ChoreList[cb.SelectedIndex].someData)))
                    {
                        string message = LandbStrs[LandbStrs.FindIndex(c => c.someData.SequenceEqual(ChoreList[cb.SelectedIndex].someData))].langid + ": " + LandbStrs[LandbStrs.FindIndex(c => c.someData.SequenceEqual(ChoreList[cb.SelectedIndex].someData))].ActorName + " - " + LandbStrs[LandbStrs.FindIndex(c => c.someData.SequenceEqual(ChoreList[cb.SelectedIndex].someData))].ActorSpeech;
                        richText.Document.Blocks.Add(new Paragraph(new Run(message)));
                        changeTimingBtn.IsEnabled = true;
                    }
                }
            }
        }

        private void changeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ChoreList[cb.SelectedIndex].isLandb)
            {
                try
                {
                    float tmpValue = Convert.ToSingle(timePhrase.Text);
                    if (tmpValue != ChoreList[cb.SelectedIndex].phraseTime)
                    {
                        ChoreList[cb.SelectedIndex].phraseTime = tmpValue;
                        float diff = ChoreList[cb.SelectedIndex].phraseTime - ChoreList[cb.SelectedIndex].previousPhraseTime;

                        ChoreList[cb.SelectedIndex].time.endTime = ChoreList[cb.SelectedIndex].time.begTime + ChoreList[cb.SelectedIndex].phraseTime;
                        commonLength += diff;

                        /*if (ChoreList[cb.SelectedIndex].contribution != null)
                        {
                            for (int i = 0; i < ChoreList[cb.SelectedIndex].contribution.Length; i++)
                            {
                                ChoreList[cb.SelectedIndex].contribution[i].begTime += diff;
                                ChoreList[cb.SelectedIndex].contribution[i].endTime += diff;
                            }
                        }*/

                        if (cb.SelectedIndex + 1 < ChoreList.Count)
                        {
                            for (int k = cb.SelectedIndex + 1; k < ChoreList.Count; k++)
                            {
                                if (ChoreList[k].isLandb)
                                {
                                    ChoreList[k].time.begTime += diff;
                                    ChoreList[k].info.beginTime += diff;
                                    ChoreList[k].time.endTime = ChoreList[k].time.begTime + ChoreList[k].phraseTime;
                                    ChoreList[k].info.endTime = ChoreList[k].time.endTime;
                                }
                            }
                        }
                    }

                    begTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].time.begTime);
                    endTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].time.endTime);
                    timePhrase.Text = Convert.ToString(ChoreList[cb.SelectedIndex].phraseTime);
                }
                catch
                {
                    timePhrase.Text = Convert.ToString(ChoreList[cb.SelectedIndex].previousPhraseTime);
                    ChoreList[cb.SelectedIndex].phraseTime = ChoreList[cb.SelectedIndex].previousPhraseTime;
                }
            }
            else
            {
                float beginTime = ChoreList[cb.SelectedIndex].time.begTime;
                float endingTime = ChoreList[cb.SelectedIndex].time.endTime;
                float phraseTime = ChoreList[cb.SelectedIndex].phraseTime;

                try
                {
                    if(Convert.ToSingle(begTime.Text) != ChoreList[cb.SelectedIndex].time.begTime)
                    {
                        ChoreList[cb.SelectedIndex].time.begTime = Convert.ToSingle(begTime.Text);
                        ChoreList[cb.SelectedIndex].time.endTime = ChoreList[cb.SelectedIndex].time.begTime + ChoreList[cb.SelectedIndex].phraseTime;
                        endTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].time.endTime);
                    }
                    if(Convert.ToSingle(endTime.Text) != ChoreList[cb.SelectedIndex].time.endTime)
                    {
                        ChoreList[cb.SelectedIndex].time.endTime = Convert.ToSingle(endTime.Text);
                        ChoreList[cb.SelectedIndex].phraseTime = ChoreList[cb.SelectedIndex].time.endTime - ChoreList[cb.SelectedIndex].time.begTime;
                        timePhrase.Text = Convert.ToString(ChoreList[cb.SelectedIndex].phraseTime);
                    }
                    if(Convert.ToSingle(timePhrase.Text) != ChoreList[cb.SelectedIndex].phraseTime)
                    {
                        ChoreList[cb.SelectedIndex].phraseTime = Convert.ToSingle(timePhrase.Text);
                        ChoreList[cb.SelectedIndex].time.endTime = ChoreList[cb.SelectedIndex].time.begTime + ChoreList[cb.SelectedIndex].phraseTime;
                        endTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].time.endTime);
                    }

                    begTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].time.begTime);
                    endTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].time.endTime);
                    timePhrase.Text = Convert.ToString(ChoreList[cb.SelectedIndex].phraseTime);
                }
                catch
                {
                    timePhrase.Text = Convert.ToString(phraseTime);
                    begTime.Text = Convert.ToString(beginTime);
                    endTime.Text = Convert.ToString(endingTime);

                    ChoreList[cb.SelectedIndex].time.begTime = beginTime;
                    ChoreList[cb.SelectedIndex].time.endTime = endingTime;
                    ChoreList[cb.SelectedIndex].phraseTime = phraseTime;
                }
            }
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ChoreList.Count > 0 && ChoreList != null)
            {
                FileStream fs = new FileStream(ChoreFilePath, FileMode.Open);
                BinaryWriter bw = new BinaryWriter(fs);

                bw.BaseStream.Seek(commonPos, SeekOrigin.Begin);
                bw.Write(commonLength);

                for(int i = 0; i < ChoreList.Count; i++)
                {
                    bw.BaseStream.Seek(ChoreList[i].Pos, SeekOrigin.Begin);
                    bw.Write(ChoreList[i].phraseTime);

                    if (ChoreList[i].time.hasTime)
                    {
                        bw.BaseStream.Seek(ChoreList[i].time.Pos, SeekOrigin.Begin);
                        bw.Write(ChoreList[i].time.begTime);
                        bw.BaseStream.Seek(9, SeekOrigin.Current);
                        bw.Write(ChoreList[i].time.endTime);
                    }
                    if(ChoreList[i].contribution != null)
                    {
                        for(int k = 0; k < ChoreList[i].contribution.Length; k++)
                        {
                            bw.BaseStream.Seek(ChoreList[i].contribution[k].Pos, SeekOrigin.Begin);
                            bw.Write(ChoreList[i].contribution[k].begTime);
                            bw.BaseStream.Seek(9, SeekOrigin.Current);
                            bw.Write(ChoreList[i].contribution[k].endTime);
                        }
                    }

                    bw.BaseStream.Seek(ChoreList[i].info.addPos, SeekOrigin.Begin);
                    bw.Write(ChoreList[i].info.beginTime);
                    bw.Write(ChoreList[i].info.endTime);
                }

                if(CamChoreList != null && CamChoreList.Count > 0)
                {
                    for(int i =0; i < CamChoreList.Count; i++)
                    {
                        bw.BaseStream.Seek(CamChoreList[i].Position, SeekOrigin.Begin);
                        bw.Write(CamChoreList[i].time);
                    }
                }

                bw.Close();
                fs.Close();
            }
        }

        private void cam_cb_changed(object sender, SelectionChangedEventArgs e)
        {
            if (cam_cb.SelectedIndex != -1)
            {
                camTime.Text = Convert.ToString(CamChoreList[cam_cb.SelectedIndex].time);
            }
        }

        private void anmFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "anm файл (*.anm) | *.anm";

            if(ofd.ShowDialog() == true)
            {
                anmFileInputText.Text = ofd.FileName;
                FileStream fs = new FileStream(anmFileInputText.Text, FileMode.Open);
                BinaryReader br = new BinaryReader(fs);

                br.BaseStream.Seek(16, SeekOrigin.Begin);
                int count = br.ReadInt32();

                br.BaseStream.Seek((count * 12) + 16, SeekOrigin.Current);
                commonTiming = br.ReadSingle();
                br.BaseStream.Seek(0x71, SeekOrigin.Current);
                count = br.ReadInt32();
                anms = new anm.animation[count];

                br.BaseStream.Seek(4, SeekOrigin.Current);

                byte[] tmp;

                for(int i = 0; i < count; i++)
                {
                    anms[i].oneValue = br.ReadByte();
                    anms[i].one = br.ReadInt32();
                    anms[i].length = br.ReadInt32(); //0x1C
                    anms[i].SomeData = br.ReadBytes(anms[i].length);
                    tmp = new byte[4];
                    Array.Copy(anms[i].SomeData, anms[i].SomeData.Length - 4, tmp, 0, tmp.Length);
                    anms[i].time = BitConverter.ToSingle(tmp, 0);
                }

                br.Close();
                fs.Close();

                timeAnm.Text = Convert.ToString(commonTiming);
            }
        }

        private void changeAnmBtn_Click(object sender, RoutedEventArgs e)
        {
            newCommonTiming = Convert.ToSingle(timeAnm.Text);
            float diff = newCommonTiming - commonTiming;
            commonTiming = newCommonTiming;

            byte[] tmp;

            for(int i = 1; i < anms.Length; i++)
            {
                anms[i].time += diff;
                tmp = BitConverter.GetBytes(anms[i].time);
                Array.Copy(tmp, 0, anms[i].SomeData, anms[i].SomeData.Length - 4, tmp.Length);
            }
        }

        private void saveAnmBtn_Click(object sender, RoutedEventArgs e)
        {
            int poz = 16;
            FileStream fs = new FileStream(anmFileInputText.Text, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);
            br.BaseStream.Seek(poz, SeekOrigin.Begin);
            int count = br.ReadInt32();
            br.Close();
            fs.Close();

            fs = new FileStream(anmFileInputText.Text, FileMode.OpenOrCreate);
            BinaryWriter bw = new BinaryWriter(fs);
            poz += (12 * count) + 16 + 4;
            bw.BaseStream.Seek(poz, SeekOrigin.Begin);
            bw.Write(commonTiming);
            poz += 0x7D;
            bw.BaseStream.Seek(poz, SeekOrigin.Begin);

            for(int i = 0; i < anms.Length; i++)
            {
                bw.Write(anms[i].oneValue);
                bw.Write(anms[i].one);
                bw.Write(anms[i].length);
                bw.Write(anms[i].SomeData);
            }

            bw.Close();
            fs.Close();
        }

        private void changeCamBtn_Click(object sender, RoutedEventArgs e)
        {
            if((CamChoreList.Count > 0) && (CamChoreList != null) && (cam_cb.SelectedIndex != -1))
            {
                int index = cam_cb.SelectedIndex;

                try
                {
                    CamChoreList[index].NewTime = Convert.ToSingle(camTime.Text);
                    camTime.Text = Convert.ToString(CamChoreList[index].NewTime);
                    CamChoreList[index].time = CamChoreList[index].NewTime;
                }
                catch
                {
                    camTime.Text = Convert.ToString(CamChoreList[index].time);
                }
            }
        }

        private void contribCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(contribCB.SelectedIndex != -1)
            {
                begContrTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].contribution[contribCB.SelectedIndex].begTime);
                endContrTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].contribution[contribCB.SelectedIndex].endTime);
            }
        }

        private void changeTimingBtn_Click(object sender, RoutedEventArgs e)
        {
            float beginTime = ChoreList[cb.SelectedIndex].time.begTime;
            float endingTime = ChoreList[cb.SelectedIndex].time.endTime;
            float phraseTime = ChoreList[cb.SelectedIndex].phraseTime;

            try
            {
                if (Convert.ToSingle(begTime.Text) != ChoreList[cb.SelectedIndex].time.begTime)
                {
                    ChoreList[cb.SelectedIndex].time.begTime = Convert.ToSingle(begTime.Text);
                    if((ChoreList[cb.SelectedIndex].time.begTime + ChoreList[cb.SelectedIndex].phraseTime > commonTiming)
                        && (cb.SelectedIndex + 1 == cb.Items.Count))
                    {
                        float diff = (commonTiming - (ChoreList[cb.SelectedIndex].time.begTime + ChoreList[cb.SelectedIndex].phraseTime));
                        commonTiming += diff;
                    }
                    ChoreList[cb.SelectedIndex].time.endTime = ChoreList[cb.SelectedIndex].time.begTime + ChoreList[cb.SelectedIndex].phraseTime;
                    endTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].time.endTime);
                }
                if (Convert.ToSingle(endTime.Text) != ChoreList[cb.SelectedIndex].time.endTime)
                {
                    ChoreList[cb.SelectedIndex].time.endTime = Convert.ToSingle(endTime.Text);
                    ChoreList[cb.SelectedIndex].phraseTime = ChoreList[cb.SelectedIndex].time.endTime - ChoreList[cb.SelectedIndex].time.begTime;
                    if ((ChoreList[cb.SelectedIndex].time.begTime + ChoreList[cb.SelectedIndex].phraseTime > commonTiming)
                        && (cb.SelectedIndex + 1 == cb.Items.Count))
                    {
                        float diff = (commonTiming - (ChoreList[cb.SelectedIndex].time.begTime + ChoreList[cb.SelectedIndex].phraseTime));
                        commonTiming += diff;
                    }
                    timePhrase.Text = Convert.ToString(ChoreList[cb.SelectedIndex].phraseTime);
                }
                if (Convert.ToSingle(timePhrase.Text) != ChoreList[cb.SelectedIndex].phraseTime)
                {
                    ChoreList[cb.SelectedIndex].phraseTime = Convert.ToSingle(timePhrase.Text);
                    ChoreList[cb.SelectedIndex].time.endTime = ChoreList[cb.SelectedIndex].time.begTime + ChoreList[cb.SelectedIndex].phraseTime;
                    if ((ChoreList[cb.SelectedIndex].time.begTime + ChoreList[cb.SelectedIndex].phraseTime > commonTiming)
                        && (cb.SelectedIndex + 1 == cb.Items.Count))
                    {
                        float diff = (commonTiming - (ChoreList[cb.SelectedIndex].time.begTime + ChoreList[cb.SelectedIndex].phraseTime));
                        commonTiming += diff;
                    }
                    endTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].time.endTime);
                }
            }
            catch
            {
                timePhrase.Text = Convert.ToString(phraseTime);
                begTime.Text = Convert.ToString(beginTime);
                endTime.Text = Convert.ToString(endingTime);

                ChoreList[cb.SelectedIndex].time.begTime = beginTime;
                ChoreList[cb.SelectedIndex].time.endTime = endingTime;
                ChoreList[cb.SelectedIndex].phraseTime = phraseTime;
            }
        }

        private void changeValuesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (contribCB.Items.Count > 0) {
                float first_val = ChoreList[cb.SelectedIndex].contribution[contribCB.SelectedIndex].begTime;
                float last_val = ChoreList[cb.SelectedIndex].contribution[contribCB.SelectedIndex].endTime;

                try
                {
                    ChoreList[cb.SelectedIndex].contribution[contribCB.SelectedIndex].begTime = Convert.ToSingle(begContrTime.Text);
                    ChoreList[cb.SelectedIndex].contribution[contribCB.SelectedIndex].endTime = Convert.ToSingle(endContrTime.Text);
                }
                catch
                {
                    ChoreList[cb.SelectedIndex].contribution[contribCB.SelectedIndex].begTime = first_val;
                    ChoreList[cb.SelectedIndex].contribution[contribCB.SelectedIndex].endTime = last_val;

                    begContrTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].contribution[contribCB.SelectedIndex].begTime);
                    endContrTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].contribution[contribCB.SelectedIndex].endTime);
                }
            }
        }
    }
}
