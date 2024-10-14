using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using TTG_Tools;
using System.Windows.Forms;
using System.Linq;
using System.Windows.Documents;

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
        Chores.Chore chore;
        //List<Chores.Chore> ChoreList = null;
        //List<Chores.CameraChore> CamChoreList = null;
        float commonLength = 0.0f;
        fileList files = null;
        int commonPos = 0;

        float commonTiming = 0.0f;
        float newCommonTiming = 0.0f;

        anm.animation[] anms;

        byte[] value = { 0x84, 0xF0, 0x0E, 0x83, 0x25, 0x01, 0x07, 0x6B }; //value<float> что-то там
        byte[] timeCRC64 = { 0x55, 0xD3, 0xDC, 0xA6, 0x0F, 0xA3, 0xD6, 0x4B }; //CRC64 слова "time"
        byte[] contributionCRC64 = { 0xF6, 0xB5, 0x7F, 0x6E, 0x58, 0x41, 0xD3, 0x9B }; //CRC64 слова "contribution"
        byte[] someValue = { 0x47, 0xA9, 0x33, 0xCC, 0x28, 0x9F, 0x6B, 0xBC }; //WTF? After this value I see some property set junk

        private int SearchCRC64Value(byte[] block, ulong CRC64)
        {
            for(int i = 0; i + 8 < block.Length; i++)
            {
                ulong checkVal = BitConverter.ToUInt64(block, i);

                if (checkVal == CRC64) return i;
            }
            return -1;
        }

        private int SearchBinText(FileStream fs, int pos, string pattern)
        {
            byte[] tmpPattern = Encoding.UTF8.GetBytes(pattern);
            byte[] tmp;

            while (pos <= fs.Length)
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
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = "CHORE file (*.chore) | *.chore";

            if (ofd.ShowDialog() == true && ((LandbStrs != null) && (LandbStrs.Count > 0)))
            {
                FileStream fs = new FileStream(ofd.FileName, FileMode.Open);
                BinaryReader br = new BinaryReader(fs);

                chore = new Chores.Chore();

                chore.header = br.ReadBytes(4);
                chore.blockLength = br.ReadUInt32();
                chore.someBytes = br.ReadBytes(8);
                chore.engineCommandsCount = br.ReadInt32();
                InEngineWords words = new InEngineWords();

                bool hasChoreResource = false;
                bool hasAnimation = false;

                chore.commands = new Chores.engineCommands[chore.engineCommandsCount];

                for (int i = 0; i < chore.engineCommandsCount; i++)
                {
                    chore.commands[i].nameCRC64 = br.ReadUInt64();
                    chore.commands[i].value = br.ReadUInt32();

                    if (chore.commands[i].nameCRC64 == CRCs.CRC64(0, words.animation.ToLower())) hasAnimation = true;
                    if (chore.commands[i].nameCRC64 == CRCs.CRC64(0, words.choreResource.ToLower())) hasChoreResource = true;
                }

                if (hasAnimation || hasChoreResource)
                {
                    bool hasPropVal = false;

                    int blockLen = br.ReadInt32();
                    int len = br.ReadInt32();
                    byte[] tmp = br.ReadBytes(len);
                    chore.fileName = Encoding.ASCII.GetString(tmp);
                    chore.someValue = br.ReadInt32();
                    chore.commonTime = br.ReadSingle();
                    chore.countElements = br.ReadInt32();
                    chore.countObjects = br.ReadInt32();

                    chore.elements = new Chores.choreElements[chore.countElements];
                    chore.objects = new Chores.objectElements[chore.countObjects];

                    for (int i = 0; i < chore.countElements; i++)
                    {
                        chore.elements[i].isLandb = false;
                        chore.elements[i].hasTime = false;
                        chore.elements[i].hasContribution = false;
                        chore.elements[i].hasActiveCamera = false;
                        chore.elements[i].hasStyleGuide = false;
                        chore.elements[i].isPropBlock = false;

                        chore.elements[i].unknown1 = br.ReadInt32();
                        chore.elements[i].unknown2 = br.ReadInt32();
                        chore.elements[i].unknown3 = br.ReadInt32();
                        chore.elements[i].unknownLen1 = br.ReadInt32();
                        chore.elements[i].block1 = br.ReadBytes(chore.elements[i].unknownLen1 - 4);
                        chore.elements[i].unknownLen2 = br.ReadInt32();
                        chore.elements[i].block2 = br.ReadBytes(chore.elements[i].unknownLen2 - 4);
                        chore.elements[i].unknownPropBytes = null;

                        if (chore.elements[i].unknown3 == 0)
                        {
                            chore.elements[i].imports.unknownValue = br.ReadInt32();
                            chore.elements[i].imports.blockSize1 = br.ReadInt32();
                            chore.elements[i].imports.block1 = br.ReadBytes(chore.elements[i].imports.blockSize1 - 4);
                            chore.elements[i].imports.blockSize2 = br.ReadInt32();
                            chore.elements[i].imports.block2 = br.ReadBytes(chore.elements[i].imports.blockSize2 - 4);
                            chore.elements[i].imports.val = br.ReadByte();
                        }

                        chore.elements[i].unknownLen3 = br.ReadInt32();
                        chore.elements[i].block3 = br.ReadBytes(chore.elements[i].unknownLen3 - 4);

                        if (hasPropVal)
                        {
                            chore.elements[i].unknownPropBytes = br.ReadBytes(16); //Skip 16 bytes of junk
                        }

                        chore.elements[i].value1 = br.ReadInt32();
                        chore.elements[i].crc64Name1 = br.ReadUInt64();

                        chore.elements[i].name1 = BitConverter.ToString(BitConverter.GetBytes(chore.elements[i].crc64Name1), 0);

                        if (files.CRCs.Any(x => x == chore.elements[i].crc64Name1))
                        {
                            chore.elements[i].name1 = files.files[Array.IndexOf(files.CRCs, chore.elements[i].crc64Name1)];
                        }
                        else if (LandbStrs.Any(x => x.someData == chore.elements[i].crc64Name1))
                        {
                            chore.elements[i].name1 = Convert.ToString(LandbStrs.ToArray()[LandbStrs.FindIndex(p => p.someData == chore.elements[i].crc64Name1)].langid) + ".lang";
                            chore.elements[i].isLandb = true;
                        }

                        chore.elements[i].elementTime = br.ReadSingle();
                        chore.elements[i].value2 = br.ReadInt32();
                        chore.elements[i].value3 = br.ReadInt32();
                        chore.elements[i].unknownLen4 = br.ReadInt32();
                        chore.elements[i].block4 = br.ReadBytes(chore.elements[i].unknownLen4 - 4);
                        chore.elements[i].blockLen = br.ReadInt32();
                        chore.elements[i].blockName = br.ReadBytes(chore.elements[i].blockLen - 4);

                        //if it's not a prop shit
                        if (chore.elements[i].value2 != 0x186a0 && chore.elements[i].blockName.Length >= 8)
                        {
                            chore.elements[i].crc64Name2 = BitConverter.ToUInt64(chore.elements[i].blockName, 0);

                            if (chore.elements[i].blockName.Length - 8 >= 8)
                            {
                                hasPropVal = BitConverter.ToUInt64(chore.elements[i].blockName, 8) == BitConverter.ToUInt64(someValue, 0);
                            }

                            chore.elements[i].name2 = BitConverter.ToString(BitConverter.GetBytes(chore.elements[i].crc64Name2), 0);
                            if (files.CRCs.Contains(chore.elements[i].crc64Name2))
                            {
                                chore.elements[i].name2 = files.files[Array.IndexOf(files.CRCs, chore.elements[i].crc64Name2)];
                            }
                            else if (LandbStrs.Any(x => x.someData == chore.elements[i].crc64Name2))
                            {
                                chore.elements[i].name2 = Convert.ToString(LandbStrs.ToArray()[LandbStrs.FindIndex(p => p.someData == chore.elements[i].crc64Name2)].langid) + ".lang";
                            }
                        }
                        else
                        {
                            hasPropVal = false;
                            chore.elements[i].isPropBlock = true;
                        }

                        chore.elements[i].blockSize = br.ReadInt32();
                        chore.elements[i].elementBlock = br.ReadBytes(chore.elements[i].blockSize - 4);
                        chore.elements[i].subBlockSize = br.ReadInt32();
                        chore.elements[i].subBlockElement = br.ReadBytes(chore.elements[i].subBlockSize - 4);
                        chore.elements[i].logicValues = br.ReadBytes(8);

                        int timePos = SearchCRC64Value(chore.elements[i].elementBlock, CRCs.CRC64(0, words.time.ToLower()));
                        int contributionPos = SearchCRC64Value(chore.elements[i].elementBlock, CRCs.CRC64(0, words.contribution.ToLower()));
                        int activeCameraPos = SearchCRC64Value(chore.elements[i].elementBlock, CRCs.CRC64(0, words.activeCamera.ToLower()));
                        int stylePos = SearchCRC64Value(chore.elements[i].elementBlock, CRCs.CRC64(0, words.styleGuide.ToLower()));

                        if (activeCameraPos != -1)
                        {
                            chore.elements[i].hasActiveCamera = true;
                            using (MemoryStream ms = new MemoryStream(chore.elements[i].elementBlock))
                            {
                                using (BinaryReader mbr = new BinaryReader(ms))
                                {
                                    //skip camera pos values and 4 bytes of something
                                    mbr.BaseStream.Seek(activeCameraPos + 8 + 4, SeekOrigin.Begin);

                                    //Read some block data (probably it will be empty)
                                    for (int d = 0; d < 2; d++)
                                    {
                                        int sizeBlock = mbr.ReadInt32();
                                        byte[] tmpBlock = mbr.ReadBytes(sizeBlock - 4);
                                    }

                                    blockLen = mbr.ReadInt32();
                                    int countCam = mbr.ReadInt32();
                                    chore.elements[i].cameras.time = new float[countCam];
                                    chore.elements[i].cameras.Pos = new int[countCam];
                                    chore.elements[i].cameras.cameraName = new string[countCam];

                                    for (int c = 0; c < countCam; c++)
                                    {
                                        chore.elements[i].cameras.Pos[c] = (int)mbr.BaseStream.Position;
                                        chore.elements[i].cameras.time[c] = mbr.ReadSingle();
                                        byte zero = mbr.ReadByte();
                                        int countName = mbr.ReadInt32();
                                        int blNameSize = mbr.ReadInt32();
                                        int nameSize = mbr.ReadInt32();
                                        byte[] tmpName = mbr.ReadBytes(nameSize);
                                        chore.elements[i].cameras.cameraName[c] = Encoding.ASCII.GetString(tmpName);
                                    }
                                }
                            }
                        }

                        if(stylePos != -1)
                        {
                            chore.elements[i].hasStyleGuide = true;
                            using (MemoryStream ms = new MemoryStream(chore.elements[i].elementBlock))
                            {
                                using (BinaryReader mbr = new BinaryReader(ms))
                                {
                                    //skip camera pos values and 4 bytes of something
                                    mbr.BaseStream.Seek(stylePos + 8 + 4, SeekOrigin.Begin);

                                    //Read some block data (probably it will be empty)
                                    for (int d = 0; d < 2; d++)
                                    {
                                        int sizeBlock = mbr.ReadInt32();
                                        byte[] tmpBlock = mbr.ReadBytes(sizeBlock - 4);
                                    }

                                    blockLen = mbr.ReadInt32();
                                    int countActors = mbr.ReadInt32();
                                    chore.elements[i].styles.actorNames = new string[countActors];

                                    for (int a = 0; a < countActors; a++)
                                    {
                                        float floatVal = mbr.ReadSingle(); //skip read float values because it's just actor names
                                        byte zero = mbr.ReadByte();
                                        int countName = mbr.ReadInt32();
                                        int blNameSize = mbr.ReadInt32();
                                        int nameSize = mbr.ReadInt32();
                                        byte[] tmpName = mbr.ReadBytes(nameSize);
                                        chore.elements[i].styles.actorNames[a] = Encoding.ASCII.GetString(tmpName);
                                    }

                                    //Skip some values needed for style guide

                                    mbr.BaseStream.Seek(20, SeekOrigin.Current);

                                    //Read AGAIN some block data (probably it will be empty)
                                    for (int d = 0; d < 2; d++)
                                    {
                                        int sizeBlock = mbr.ReadInt32();
                                        byte[] tmpBlock = mbr.ReadBytes(sizeBlock - 4);
                                    }

                                    blockLen = mbr.ReadInt32();
                                    int countStyles = mbr.ReadInt32();

                                    chore.elements[i].styles.timeStyles = new float[countStyles];
                                    chore.elements[i].styles.Pos = new int[countStyles];
                                    chore.elements[i].styles.styles = new string[countStyles];

                                    for(int s = 0; s < countStyles; s++)
                                    {
                                        chore.elements[i].styles.Pos[s] = (int)mbr.BaseStream.Position;
                                        chore.elements[i].styles.timeStyles[s] = mbr.ReadSingle();
                                        byte zero = mbr.ReadByte();
                                        int countName = mbr.ReadInt32();
                                        int blNameSize = mbr.ReadInt32();
                                        int nameSize = mbr.ReadInt32();
                                        byte[] tmpName = mbr.ReadBytes(nameSize);
                                        chore.elements[i].styles.styles[s] = Encoding.ASCII.GetString(tmpName);
                                    }
                                }
                            }
                        }

                        if (timePos != -1)
                        {
                            chore.elements[i].hasTime = true;
                            timePos += 24;

                            int tmpCount = BitConverter.ToInt32(chore.elements[i].elementBlock, timePos);
                            chore.elements[i].timeElement = new Chores.Time[tmpCount];
                            timePos += 4;

                            for(int t = 0; t < tmpCount; t++)
                            {
                                chore.elements[i].timeElement[t].Pos = timePos;
                                chore.elements[i].timeElement[t].modifiedTime = false;
                                chore.elements[i].timeElement[t].timeElement = (float)Math.Round(BitConverter.ToSingle(chore.elements[i].elementBlock, timePos), 3);

                                timePos += 13;
                            }
                        }

                        if (contributionPos != -1)
                        {
                            chore.elements[i].hasContribution = true;
                            contributionPos += 24;

                            int tmpCount = BitConverter.ToInt32(chore.elements[i].elementBlock, contributionPos);
                            chore.elements[i].contribElement = new Chores.Contribution[tmpCount];
                            contributionPos += 4;

                            for(int c = 0; c < tmpCount; c++)
                            {
                                chore.elements[i].contribElement[c].Pos = contributionPos;
                                chore.elements[i].contribElement[c].modifiedTime = false;
                                chore.elements[i].contribElement[c].timeContribution = (float)Math.Round(BitConverter.ToSingle(chore.elements[i].elementBlock, contributionPos), 3);

                                contributionPos += 13;
                            }
                        }
                    }

                    chore.blockSize1 = br.ReadInt32();
                    chore.block1 = br.ReadBytes(chore.blockSize1 - 4);
                    chore.blockSize2 = br.ReadInt32();
                    chore.block2 = br.ReadBytes(chore.blockSize2 - 4);
                    chore.blockSize3 = br.ReadInt32();
                    chore.block3 = br.ReadBytes(chore.blockSize3 - 4);

                    for (int i = 0; i < chore.countObjects; i++)
                    {
                        chore.objects[i].blockNameLen1 = br.ReadInt32();
                        chore.objects[i].nameLen1 = br.ReadInt32();
                        tmp = br.ReadBytes(chore.objects[i].nameLen1);
                        chore.objects[i].name1 = Encoding.ASCII.GetString(tmp);
                        chore.objects[i].someValue = br.ReadInt32();
                        chore.objects[i].blockElementLen = br.ReadInt32();
                        chore.objects[i].elementsCount = br.ReadInt32();
                        chore.objects[i].elements = new int[chore.objects[i].elementsCount];

                        for (int j = 0; j < chore.objects[i].elementsCount; j++)
                        {
                            chore.objects[i].elements[j] = br.ReadInt32();
                        }

                        chore.objects[i].blockElementSize = br.ReadInt32();
                        chore.objects[i].blockElement = br.ReadBytes(chore.objects[i].blockElementSize - 4);
                        chore.objects[i].someValue2 = br.ReadInt32();
                        chore.objects[i].blockNameLen2 = br.ReadInt32();
                        chore.objects[i].nameLen2 = br.ReadInt32();
                        tmp = br.ReadBytes(chore.objects[i].nameLen2);
                        chore.objects[i].name2 = Encoding.ASCII.GetString(tmp);
                        chore.objects[i].blockSize = br.ReadInt32();
                        chore.objects[i].block = br.ReadBytes(chore.objects[i].blockSize - 4);
                    }

                    chore.endFileBlock = null;

                    if(br.BaseStream.Position < br.BaseStream.Length)
                    {
                        chore.endFileBlock = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
                    }

                    if (objectNamesCB.Items.Count > 0) objectNamesCB.Items.Clear();

                    for (int i = 0; i < chore.countObjects; i++)
                    {
                        objectNamesCB.Items.Add(chore.objects[i].name1);
                    }

                    if (objectNamesCB.Items.Count > 0) objectNamesCB.SelectedIndex = 0;

                    ChoreFilePath = ofd.FileName;
                }
                else
                {
                    System.Windows.MessageBox.Show("Timing elements not found.", "There is no needed timing elements");
                }

                br.Close();
                fs.Close();

                inputFile.Text = ofd.FileName;

                #region
                /*if (cam_cb.Items.Count > 0) cam_cb.Items.Clear();

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
                }*/
                #endregion

            }
        }

        private void landbFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                landbFolderPath = fbd.SelectedPath;
                folderPath.Text = landbFolderPath;

                LandbStrs = new List<landbs.landb>();

                DirectoryInfo di = new DirectoryInfo(landbFolderPath);
                FileInfo[] fi = di.GetFiles("*.landb", SearchOption.AllDirectories);

                if (fi.Length > 0)
                {
                    for (int i = 0; i < fi.Length; i++)
                    {
                        landbs.ReadFiles(fi[i].FullName, ref LandbStrs);
                    }
                }
            }
        }

        private void cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region старый код
            /*begTime.Text = "";
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
            }*/
            #endregion
        }

        private void changeBtn_Click(object sender, RoutedEventArgs e)
        {
            #region старый код
            /*if (ChoreList[cb.SelectedIndex].isLandb)
            {
                try
                {
                    float tmpValue = Convert.ToSingle(timePhrase.Text);
                    if (tmpValue != ChoreList[cb.SelectedIndex].phraseTime)
                    {
                        ChoreList[cb.SelectedIndex].phraseTime = tmpValue;
                        float diff = ChoreList[cb.SelectedIndex].phraseTime - ChoreList[cb.SelectedIndex].previousPhraseTime;

                        ChoreList[cb.SelectedIndex].time.endTime = ChoreList[cb.SelectedIndex].time.begTime + ChoreList[cb.SelectedIndex].phraseTime;
                        commonLength += diff;*/

            /*if (ChoreList[cb.SelectedIndex].contribution != null)
            {
                for (int i = 0; i < ChoreList[cb.SelectedIndex].contribution.Length; i++)
                {
                    ChoreList[cb.SelectedIndex].contribution[i].begTime += diff;
                    ChoreList[cb.SelectedIndex].contribution[i].endTime += diff;
                }
            }*/

            /*if (cb.SelectedIndex + 1 < ChoreList.Count)
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
}*/
            #endregion
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            #region старый код
            /*if (ChoreList.Count > 0 && ChoreList != null)
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
            }*/
            #endregion
        }

        private void cam_cb_changed(object sender, SelectionChangedEventArgs e)
        {
            #region Старый код
            /*if (cam_cb.SelectedIndex != -1)
            {
                camTime.Text = Convert.ToString(CamChoreList[cam_cb.SelectedIndex].time);
            }*/
            #endregion
        }

        private void anmFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = "anm файл (*.anm) | *.anm";

            if (ofd.ShowDialog() == true)
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

                for (int i = 0; i < count; i++)
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

            for (int i = 1; i < anms.Length; i++)
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

            for (int i = 0; i < anms.Length; i++)
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
            #region старый код
            /*if((CamChoreList.Count > 0) && (CamChoreList != null) && (cam_cb.SelectedIndex != -1))
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
            }*/
            #endregion
        }

        private void contribCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region старый код
            /*
            if(contribCB.SelectedIndex != -1)
            {
                begContrTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].contribution[contribCB.SelectedIndex].begTime);
                endContrTime.Text = Convert.ToString(ChoreList[cb.SelectedIndex].contribution[contribCB.SelectedIndex].endTime);
            }*/
            #endregion
        }

        private void changeTimingBtn_Click(object sender, RoutedEventArgs e)
        {
            #region старый код
            /*
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
            */
            #endregion
        }

        private void changeValuesBtn_Click(object sender, RoutedEventArgs e)
        {
            #region старый код
            /*if (contribCB.Items.Count > 0) {
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
            }*/
            #endregion
        }

        private void extractDataBtn_Click(object sender, RoutedEventArgs e)
        {
            #region старый код
            /*System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "Text file (*.txt) | *.txt";

            if(sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<string> strs = new List<string>();

                strs.Add("Common length=" + Convert.ToString(commonLength) + "\r\nCommon pos=" + Convert.ToString(commonPos));

                for(int i = 0; i < ChoreList.Count; i++)
                {
                    string str = "BlockNum=" + Convert.ToString(i + 1) + "\r\n";
                    str += "SomeData=" + BitConverter.ToString(ChoreList[i].someData) + "\r\n";
                    str += "isLandb=" + Convert.ToString(ChoreList[i].isLandb) + "\r\n";
                    str += "pos=" + Convert.ToString(ChoreList[i].Pos) + "\r\n";
                    str += "phraseTime=" + Convert.ToString(ChoreList[i].phraseTime) + "\r\n";
                    str += "time.hasTime=" + Convert.ToString(ChoreList[i].time.hasTime) + "\r\n";
                    str += "previousPhraseTime=" + Convert.ToString(ChoreList[i].previousPhraseTime) + "\r\n";

                    str += "time.Pos=" + Convert.ToString(ChoreList[i].time.Pos) + "\r\n";
                    str += "time.begTime=" + Convert.ToString(ChoreList[i].time.begTime) + "\r\n";
                    str += "time.endTime=" + Convert.ToString(ChoreList[i].time.endTime) + "\r\n";

                    str += "contributionCount=" + ChoreList[i].contribution.Length + "\r\n";

                    for (int c = 0; c < ChoreList[i].contribution.Length; c++)
                    {
                        str += "contribution(" + Convert.ToString(c + 1) + ").Pos=" + Convert.ToString(ChoreList[i].contribution[c].Pos) + "\r\n";
                        str += "contribution(" + Convert.ToString(c + 1) + ").begTime=" + Convert.ToString(ChoreList[i].contribution[c].begTime) + "\r\n";
                        str += "contribution(" + Convert.ToString(c + 1) + ").endTime=" + Convert.ToString(ChoreList[i].contribution[c].endTime) + "\r\n";
                    }

                    str += "info.addPos=" + Convert.ToString(ChoreList[i].info.addPos) + "\r\n";
                    str += "info.beginTime=" + Convert.ToString(ChoreList[i].info.beginTime) + "\r\n";
                    str += "info.endTime=" + Convert.ToString(ChoreList[i].info.endTime) + "\r\n";

                    strs.Add(str);
                }

                File.WriteAllLines(sfd.FileName, strs);
            }*/
            #endregion
        }

        private void resourceFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DirectoryInfo di = new DirectoryInfo(fbd.SelectedPath);
                FileInfo[] fi = di.GetFiles("*.*", SearchOption.AllDirectories);

                files = new fileList();
                files.files = new string[fi.Length];
                files.CRCs = new ulong[fi.Length];

                for (int i = 0; i < fi.Length; i++)
                {
                    files.files[i] = fi[i].Name;
                    files.CRCs[i] = CRCs.CRC64(0, files.files[i].ToLower());
                }

                resorceFolderTB.Text = fbd.SelectedPath;
            }
        }

        private void objectNamesCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (objectNamesCB.SelectedIndex != -1)
            {
                if (elementNamesCB.Items.Count > 0) elementNamesCB.Items.Clear();

                for (int i = 0; i < chore.objects[objectNamesCB.SelectedIndex].elementsCount; i++)
                {
                    elementNamesCB.Items.Add(chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[i]].name1);
                }

                if (elementNamesCB.Items.Count > 0) elementNamesCB.SelectedIndex = 0;
            }
        }

        private void elementNamesCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            landbRT.Document.Blocks.Clear();
            landbRT.Visibility = Visibility.Hidden;

            timeBlockLabel.Visibility = Visibility.Hidden;
            timeBlockTB.Visibility = Visibility.Hidden;
            changeTimeBtn.Visibility = Visibility.Hidden;

            timeTB.Text = "";
            timeCB.Items.Clear();
            timeCB.Visibility = Visibility.Hidden;
            timeLabel.Visibility = Visibility.Hidden;
            timeTB.Visibility = Visibility.Hidden;
            timeBtn.Visibility = Visibility.Hidden;

            contributionCB.Items.Clear();
            contributionTB.Text = "";
            contributionBtn.Visibility = Visibility.Hidden;
            contributionCB.Visibility = Visibility.Hidden;
            contributionTB.Visibility = Visibility.Hidden;
            contributionLabel.Visibility = Visibility.Hidden;

            cameraNameCB.Items.Clear();
            cameraTimeTB.Text = "";
            cameraNameCB.Visibility = Visibility.Hidden;
            cameraNameLabel.Visibility = Visibility.Hidden;
            cameraTimeLabel.Visibility = Visibility.Hidden;
            cameraTimeTB.Visibility = Visibility.Hidden;

            actorNameCB.Items.Clear();
            styleCB.Items.Clear();
            styleTimeTB.Text = "";
            styleCB.Visibility = Visibility.Hidden;
            styleLabel.Visibility = Visibility.Hidden;
            styleTimeTB.Visibility = Visibility.Hidden;
            styleTimeLabel.Visibility = Visibility.Hidden;
            actorNameCB.Visibility = Visibility.Hidden;
            actorLabel.Visibility = Visibility.Hidden;

            if ((elementNamesCB.SelectedIndex != -1) && (objectNamesCB.SelectedIndex != -1))
            {
                if (chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].isLandb)
                {
                    landbRT.Visibility = Visibility.Visible;
                    string str = LandbStrs.ToArray()[LandbStrs.FindIndex(p => p.someData == chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].crc64Name2)].ActorName + ": " + LandbStrs.ToArray()[LandbStrs.FindIndex(p => p.someData == chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].crc64Name2)].ActorSpeech;
                    landbRT.Document.Blocks.Add(new Paragraph(new Run(str)));
                }

                if (chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].hasTime)
                {
                    for(int i = 0; i < chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].timeElement.Length; i++)
                    {
                        timeCB.Items.Add((i + 1).ToString());
                    }

                    timeCB.Visibility = Visibility.Visible;
                    timeLabel.Visibility = Visibility.Visible;
                    timeTB.Visibility = Visibility.Visible;
                    timeBtn.Visibility = Visibility.Visible;

                    timeCB.SelectedIndex = 0;
                }

                if (chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].hasContribution)
                {
                    for (int i = 0; i < chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].contribElement.Length; i++)
                    {
                        contributionCB.Items.Add((i + 1).ToString());
                    }

                    contributionCB.Visibility = Visibility.Visible;
                    contributionLabel.Visibility = Visibility.Visible;
                    contributionTB.Visibility = Visibility.Visible;
                    contributionBtn.Visibility = Visibility.Visible;

                    contributionCB.SelectedIndex = 0;
                }

                if(chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].hasActiveCamera)
                {
                    for(int i = 0; i < chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].cameras.cameraName.Length; i++)
                    {
                        cameraNameCB.Items.Add(chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].cameras.cameraName[i]);
                    }

                    if(cameraNameCB.Items.Count > 0) cameraNameCB.SelectedIndex = 0;

                    cameraNameCB.Visibility = Visibility.Visible;
                    cameraNameLabel.Visibility = Visibility.Visible;
                    cameraTimeLabel.Visibility = Visibility.Visible;
                    cameraTimeTB.Visibility = Visibility.Visible;
                }

                if (chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].hasStyleGuide)
                {
                    for(int i = 0; i < chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].styles.actorNames.Length; i++)
                    {
                        actorNameCB.Items.Add(chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].styles.actorNames[i]);
                    }

                    for(int i = 0; i < chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].styles.styles.Length; i++)
                    {
                        styleCB.Items.Add(chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].styles.styles[i]);
                    }

                    if(actorNameCB.Items.Count > 0) actorNameCB.SelectedIndex = 0;
                    if(styleCB.Items.Count > 0) styleCB.SelectedIndex = 0;

                    actorLabel.Visibility = Visibility.Visible;
                    actorNameCB.Visibility = Visibility.Visible;
                    styleCB.Visibility = Visibility.Visible;
                    styleTimeLabel.Visibility = Visibility.Visible;
                    styleTimeTB.Visibility = Visibility.Visible;
                }

                if (!chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].isPropBlock)
                {
                    timeBlockLabel.Visibility = Visibility.Visible;
                    timeBlockTB.Visibility = Visibility.Visible;
                    changeTimeBtn.Visibility = Visibility.Visible;

                    timeBlockTB.Text = Convert.ToString(chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].elementTime);
                }
            }
        }

        private void contributionCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (contributionCB.SelectedIndex != -1)
            {
                contributionTB.Text = Convert.ToString(chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].contribElement[contributionCB.SelectedIndex].timeContribution);
            }
        }

        private void timeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(timeCB.SelectedIndex != -1)
            {
                timeTB.Text = Convert.ToString(chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].timeElement[timeCB.SelectedIndex].timeElement);
            }
        }

        private void cameraNameCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cameraNameCB.SelectedIndex != -1)
            {
                cameraTimeTB.Text = Convert.ToString(chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].cameras.time[cameraNameCB.SelectedIndex]);
            }
        }

        private void styleCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(styleCB.SelectedIndex != -1)
            {
                styleTimeTB.Text = Convert.ToString(chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].styles.timeStyles[styleCB.SelectedIndex]);
            }
        }

        private void changeTimeBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                float newValue = Convert.ToSingle(timeBlockTB.Text);
                float diff = newValue - chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].elementTime;
                chore.commonTime += diff;
                bool modifiedCameras = false;
                bool modifiedStyles = false;

                if (chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].hasTime)
                {
                    float timeStart = chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].timeElement[0].timeElement;

                    for (int i = 1; i < chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].timeElement.Length; i++)
                    {
                        chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].timeElement[i].timeElement += diff;
                        chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].timeElement[i].modifiedTime = true;
                    }

                    for (int c = 0; c < chore.countElements; c++)
                    {
                        if (chore.elements[c].hasTime)
                        {
                            for (int t = 0; t < chore.elements[c].timeElement.Length; t++)
                            {
                                if (chore.elements[c].hasTime && chore.elements[c].timeElement[t].timeElement > timeStart)
                                {
                                    chore.elements[c].timeElement[t].timeElement += diff;
                                    chore.elements[c].timeElement[t].modifiedTime = true;
                                }
                            }
                        }

                        if (chore.elements[c].hasActiveCamera)
                        {
                            for(int t = 0; t < chore.elements[c].cameras.time.Length; t++)
                            {
                                if (chore.elements[c].cameras.time[t] > timeStart)
                                {
                                    chore.elements[c].cameras.time[t] += diff;
                                }
                            }

                            modifiedCameras = true;
                        }

                        if (chore.elements[c].hasStyleGuide)
                        {
                            for (int t = 0; t < chore.elements[c].styles.timeStyles.Length; t++)
                            {
                                if (chore.elements[c].styles.timeStyles[t] > timeStart)
                                {
                                    chore.elements[c].styles.timeStyles[t] += diff;
                                }
                            }

                            modifiedStyles = true;
                        }
                    }
                }

                if (chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].hasContribution)
                {
                    float timeStart = chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].contribElement[0].timeContribution;

                    for (int i = 1; i < chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].contribElement.Length; i++)
                    {
                        if ((chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].contribElement[i].timeContribution != chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].contribElement[i - 1].timeContribution)
                            || chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].contribElement[i - 1].modifiedTime)
                        {
                            chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].contribElement[i].timeContribution += diff;
                            chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].contribElement[i].modifiedTime = true;
                        }
                    }

                    for (int c = 0; c < chore.countElements; c++)
                    {
                        if (chore.elements[c].hasContribution)
                        {
                            for (int t = 0; t < chore.elements[c].contribElement.Length; t++)
                            {
                                if (chore.elements[c].hasTime && chore.elements[c].contribElement[t].timeContribution > timeStart)
                                {
                                    chore.elements[c].contribElement[t].timeContribution += diff;
                                    //chore.elements[c].timeElement[t].modifiedTime = true;
                                }
                            }
                        }

                        if (chore.elements[c].hasActiveCamera && !modifiedCameras)
                        {
                            for (int t = 0; t < chore.elements[c].cameras.time.Length; t++)
                            {
                                if (chore.elements[c].cameras.time[t] > timeStart)
                                {
                                    chore.elements[c].cameras.time[t] += diff;
                                }
                            }
                        }

                        if (chore.elements[c].hasStyleGuide && !modifiedStyles)
                        {
                            for (int t = 0; t < chore.elements[c].styles.timeStyles.Length; t++)
                            {
                                if (chore.elements[c].styles.timeStyles[t] > timeStart)
                                {
                                    chore.elements[c].styles.timeStyles[t] += diff;
                                }
                            }
                        }
                    }
                }

                chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].elementTime = newValue;
            }
            catch
            {
                timeBlockTB.Text = Convert.ToString(chore.elements[chore.objects[objectNamesCB.SelectedIndex].elements[elementNamesCB.SelectedIndex]].elementTime);
            }
        }

        private void saveChoreBtn_Click(object sender, RoutedEventArgs e)
        {
            if(File.Exists(ChoreFilePath))
            {
                File.Delete(ChoreFilePath);

                FileStream fs = new FileStream(ChoreFilePath, FileMode.CreateNew);
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(chore.header);
                bw.Write(chore.blockLength);
                bw.Write(chore.someBytes);
                bw.Write(chore.engineCommandsCount);

                for (int i = 0; i < chore.engineCommandsCount; i++)
                {
                    bw.Write(chore.commands[i].nameCRC64);
                    bw.Write(chore.commands[i].value);
                }

                byte[] tmp = Encoding.ASCII.GetBytes(chore.fileName);
                int blockLen = tmp.Length + 8;
                int len = tmp.Length;
                bw.Write(blockLen);
                bw.Write(len);
                bw.Write(tmp);
                bw.Write(chore.someValue);
                bw.Write(chore.commonTime);
                bw.Write(chore.countElements);
                bw.Write(chore.countObjects);

                for(int i = 0; i < chore.countElements; i++)
                {
                    bw.Write(chore.elements[i].unknown1);
                    bw.Write(chore.elements[i].unknown2);
                    bw.Write(chore.elements[i].unknown3);
                    bw.Write(chore.elements[i].unknownLen1);
                    bw.Write(chore.elements[i].block1);
                    bw.Write(chore.elements[i].unknownLen2);
                    bw.Write(chore.elements[i].block2);

                    if (chore.elements[i].unknown3 == 0)
                    {
                        bw.Write(chore.elements[i].imports.unknownValue);
                        bw.Write(chore.elements[i].imports.blockSize1);
                        bw.Write(chore.elements[i].imports.block1);
                        bw.Write(chore.elements[i].imports.blockSize2);
                        bw.Write(chore.elements[i].imports.block2);
                        bw.Write(chore.elements[i].imports.val);
                    }

                    bw.Write(chore.elements[i].unknownLen3);
                    bw.Write(chore.elements[i].block3);

                    if (chore.elements[i].unknownPropBytes != null) bw.Write(chore.elements[i].unknownPropBytes);

                    bw.Write(chore.elements[i].value1);
                    bw.Write(chore.elements[i].crc64Name1);

                    if (chore.elements[i].hasTime)
                    {
                        using (MemoryStream ms = new MemoryStream(chore.elements[i].elementBlock))
                        {
                            using (BinaryWriter mbw = new BinaryWriter(ms))
                            {
                                for (int t = 0; t < chore.elements[i].timeElement.Length; t++)
                                {
                                    mbw.BaseStream.Seek(chore.elements[i].timeElement[t].Pos, SeekOrigin.Begin);
                                    mbw.Write(chore.elements[i].timeElement[t].timeElement);
                                }
                            }

                            //chore.elements[i].elementBlock = ms.ToArray();
                        }
                    }
                    
                    if (chore.elements[i].hasContribution)
                    {
                        using (MemoryStream ms = new MemoryStream(chore.elements[i].elementBlock))
                        {
                            using (BinaryWriter mbw = new BinaryWriter(ms))
                            {
                                for (int t = 0; t < chore.elements[i].contribElement.Length; t++)
                                {
                                    mbw.BaseStream.Seek(chore.elements[i].contribElement[t].Pos, SeekOrigin.Begin);
                                    mbw.Write(chore.elements[i].contribElement[t].timeContribution);
                                }
                            }

                            //chore.elements[i].elementBlock = ms.ToArray();
                        }
                    }
                    
                    if (chore.elements[i].hasActiveCamera)
                    {
                        using (MemoryStream ms = new MemoryStream(chore.elements[i].elementBlock))
                        {
                            using (BinaryWriter mbw = new BinaryWriter(ms))
                            {
                                for (int t = 0; t < chore.elements[i].cameras.time.Length; t++)
                                {
                                    mbw.BaseStream.Seek(chore.elements[i].cameras.Pos[t], SeekOrigin.Begin);
                                    mbw.Write(chore.elements[i].cameras.time[t]);
                                }
                            }

                            //chore.elements[i].elementBlock = ms.ToArray();
                        }
                    }
                    
                    if (chore.elements[i].hasStyleGuide)
                    {
                        using (MemoryStream ms = new MemoryStream(chore.elements[i].elementBlock))
                        {
                            using (BinaryWriter mbw = new BinaryWriter(ms))
                            {
                                for (int t = 0; t < chore.elements[i].styles.timeStyles.Length; t++)
                                {
                                    mbw.BaseStream.Seek(chore.elements[i].styles.Pos[t], SeekOrigin.Begin);
                                    mbw.Write(chore.elements[i].styles.timeStyles[t]);
                                }
                            }

                            //chore.elements[i].elementBlock = ms.ToArray();
                        }
                    }

                    bw.Write(chore.elements[i].elementTime);
                    bw.Write(chore.elements[i].value2);
                    bw.Write(chore.elements[i].value3);
                    bw.Write(chore.elements[i].unknownLen4);
                    bw.Write(chore.elements[i].block4);
                    bw.Write(chore.elements[i].blockLen);
                    bw.Write(chore.elements[i].blockName);

                    bw.Write(chore.elements[i].blockSize);
                    bw.Write(chore.elements[i].elementBlock);
                    bw.Write(chore.elements[i].subBlockSize);
                    bw.Write(chore.elements[i].subBlockElement);
                    bw.Write(chore.elements[i].logicValues);
                }

                bw.Write(chore.blockSize1);
                bw.Write(chore.block1);
                bw.Write(chore.blockSize2);
                bw.Write(chore.block2);
                bw.Write(chore.blockSize3);
                bw.Write(chore.block3);

                for (int i = 0; i < chore.countObjects; i++)
                {
                    tmp = Encoding.ASCII.GetBytes(chore.objects[i].name1);
                    bw.Write(chore.objects[i].blockNameLen1);
                    bw.Write(chore.objects[i].nameLen1);
                    bw.Write(tmp);
                    bw.Write(chore.objects[i].someValue);
                    bw.Write(chore.objects[i].blockElementLen);
                    bw.Write(chore.objects[i].elementsCount);

                    for (int c = 0; c < chore.objects[i].elementsCount; c++)
                    {
                        bw.Write(chore.objects[i].elements[c]);
                    }

                    bw.Write(chore.objects[i].blockElementSize);
                    bw.Write(chore.objects[i].blockElement);
                    bw.Write(chore.objects[i].someValue2);
                    bw.Write(chore.objects[i].blockNameLen2);
                    bw.Write(chore.objects[i].nameLen2);
                    tmp = Encoding.ASCII.GetBytes(chore.objects[i].name2);
                    bw.Write(tmp);
                    bw.Write(chore.objects[i].blockSize);
                    bw.Write(chore.objects[i].block);
                }

                if (chore.endFileBlock != null) bw.Write(chore.endFileBlock);

                bw.Close();
                fs.Close();
            }
        }
    }
}
