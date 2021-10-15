using System;

namespace DbService
{
    public class Log
    {
        public Log(string FilePath, string FileEvent)
        {
            time = DateTime.Now;
            this.FilePath = FilePath;
            this.FileEvent = FileEvent;
        }

        public DateTime time { private set; get; }

        public string FilePath { private set; get; }

        public string FileEvent { private set; get; }
    }
}
