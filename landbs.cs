using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ChoreTimingEditor
{
    class landbs
    {
        public class landb
        {
            public uint langid;
            public ulong someData;
            public string ActorName;
            public string ActorSpeech;

            public landb() { }

            public landb(uint langid, ulong someData, string ActorName, string ActorSpeech)
            {
                this.langid = langid;
                this.someData = someData;
                this.ActorName = ActorName;
                this.ActorSpeech = ActorSpeech;
            }
        }

        public static void ReadFiles(string fileName, ref List<landb> strs)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);
            int count = -1;
            string ActorName = "";
            string ActorSpeech = "";
            uint langid = 0;
            ulong someData = 0;

            //Пропускаю всякую инфу
            int poz = 16;
            br.BaseStream.Seek(poz, SeekOrigin.Begin);
            int tmp = br.ReadInt32();
            br.BaseStream.Seek((12 * tmp) + 20, SeekOrigin.Current);
            count = br.ReadInt32();
            byte[] tmpStr;

            for(int i = 0; i < count; i++)
            {
                langid = br.ReadUInt32();
                someData = br.ReadUInt64();
                br.BaseStream.Seek(52, SeekOrigin.Current);
                tmp = br.ReadInt32();
                tmpStr = br.ReadBytes(tmp);
                ActorName = Encoding.UTF8.GetString(tmpStr);
                br.BaseStream.Seek(4, SeekOrigin.Current);
                tmp = br.ReadInt32();
                tmpStr = br.ReadBytes(tmp);
                ActorSpeech = Encoding.UTF8.GetString(tmpStr);

                if(ActorName != "")
                {
                    strs.Add(new landb(langid, someData, ActorName, ActorSpeech));
                }

                br.BaseStream.Seek(20, SeekOrigin.Current);
            }

            br.Close();
            fs.Close();
        }
    }
}
