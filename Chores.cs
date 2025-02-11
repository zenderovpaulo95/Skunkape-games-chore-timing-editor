﻿
using System.Collections.Generic;
using System.Security.RightsManagement;
using System.Windows.Documents;

namespace ChoreTimingEditor
{
    class Chores
    {
        public struct Time
        {
            public int Pos;
            public float timeElement;
            public bool modifiedTime;
        }

        public struct Contribution
        {
            public int Pos;
            public float timeContribution;
            public bool modifiedTime;
        }

        public struct objectElements
        {
            public int blockNameLen1;
            public int nameLen1;
            public string name1;
            public int someValue;
            public int blockElementLen;
            public int elementsCount;
            public int[] elements;
            public int blockElementSize;
            public byte[] blockElement;
            public int someValue2;
            public int blockNameLen2;
            public int nameLen2;
            public string name2;
            public int blockSize;
            public byte[] block;
        }

        public struct importElements
        {
            public int unknownValue;
            public int blockSize1;
            public byte[] block1;
            public int blockSize2;
            public byte[] block2;
            public byte val;
        }

        public struct styleElement
        {
            public string[] actorNames;
            public string[] styles;
            public float[] timeStyles;
            public int[] Pos;
        }

        public struct cameraElement
        {
            public int[] Pos;
            public string[] cameraName;
            public float[] time;
        }

        public struct subBlockTimings
        {
            public int count;
            public float[] timeElements; //powered by two
            public int Pos;
        }

       /*public struct CameraChore
        {
            public int Pos;
            public float timeChore;
            public string nameCamChore;
        }*/

        public struct choreElements
        {
            public bool isLandb;
            public bool hasTime;
            public bool hasContribution;
            public bool hasActiveCamera;
            public bool hasStyleGuide;
            public bool isPropBlock;
            public int unknown1;
            public int unknown2;
            public int unknown3;
            public int unknownLen1;
            public byte[] block1;
            public int unknownLen2;
            public byte[] block2;
            public importElements imports;
            public int unknownLen3;
            public byte[] block3;
            public int value1;
            public ulong crc64Name1;
            public string name1;
            public float elementTime;
            public int value2;
            public int value3;
            public int unknownLen4;
            public byte[] block4;
            public int blockLen;
            public byte[] blockName;
            public ulong crc64Name2;
            public string name2;
            public byte[] unknownValue; //8 byte of something.
            public byte[] unknownPropBytes; //8 bytes by 2 times of some property value
            public int blockSize;
            public byte[] elementBlock;
            public int subBlockSize;
            public Time[] timeElement;
            public Contribution[] contribElement;
            public cameraElement cameras;
            public styleElement styles;
            public byte[] subBlockElement;
            public subBlockTimings subBlockTimings;
            public byte[] logicValues; //8 byte of zeros and ones
        }

        public struct engineCommands
        {
            public ulong nameCRC64;
            public uint value;
        }

        public class Chore
        {
            public byte[] header;
            public uint blockLength;
            public byte[] someBytes; //8 bytes of something (in font size of texture blocks)
            public int engineCommandsCount;
            public engineCommands[] commands;
            public string fileName;
            public int someValue;
            public float commonTime;
            public int countElements;
            public int countObjects;

            public choreElements[] elements;
            public int blockSize1;
            public byte[] block1;
            public int blockSize2;
            public byte[] block2;
            public int blockSize3;
            public byte[] block3;
            public byte[] unknownElement; //16 bytes of something in some chore files
            public objectElements[] objects;
            public byte[] endFileBlock; //If file will be store some data after needed

            public Chore()
            {
            }
        }

        /*public class Chore
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
        }*/
    }
}
