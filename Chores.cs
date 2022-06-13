using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoreTimingEditor
{
    class Chores
    {
        public struct AdditionalData
        {
            public int addPos;
            public float beginTime;
            public float endTime;
        }

        public struct Time
        {
            public int Pos;
            public float begTime;
            public float endTime;
            public bool hasTime;
        }

        public struct Contribution
        {
            public int Pos;
            public float begTime;
            public float endTime;
        }

        public class Chore
        {
            public int Pos;
            public bool isLandb;
            public byte[] someData;
            public float phraseTime;
            public int sizeBlock;
            public float previousPhraseTime;
            public Time time;
            public Contribution[] contribution;
            public AdditionalData info;

            public Chore() { }

            public Chore(int Pos, byte[] someData, float phraseTime, float previousPhraseTime, int block, Time time, Contribution[] contrib, AdditionalData info)
            {
                this.Pos = Pos;
                this.someData = someData;
                this.sizeBlock = block;
                this.phraseTime = phraseTime;
                this.previousPhraseTime = previousPhraseTime;
                this.time = time;
                this.contribution = contrib;
                this.info = info;
            }
        }

        public class CameraChore
        {
            public int Position;
            public string CameraName;
            public float time;
            public float NewTime;

            public CameraChore() { }
            public CameraChore(int Position, string CameraName, float time, float NewTime)
            {
                this.Position = Position;
                this.CameraName = CameraName;
                this.time = time;
                this.NewTime = NewTime;
            }
        }
    }
}
