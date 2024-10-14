namespace ChoreTimingEditor
{
    class anm
    {
        public struct animation
        {
            public byte oneValue; //Вижу только 0x31
            public int one; //По крайней мере, вижу только значение 1
            public int length; //Длина данных
            public byte[] SomeData; //Какие-то координаты
            public float time; //Время смены координат
        }
    }
}
