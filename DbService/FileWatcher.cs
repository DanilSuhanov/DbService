using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Configuration;
using Npgsql;

namespace DbService
{
    public partial class FileWatcher : ServiceBase
    {
        Logger logger;
        public FileWatcher()
        {
            InitializeComponent();
            CanStop = true;
            CanPauseAndContinue = true;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            logger = new Logger();
            Thread thread = new Thread(new ThreadStart(logger.Start));
            thread.Start();
        }

        protected override void OnStop()
        {
            logger.Stop();
            Thread.Sleep(1000);
        }
    }

    class Logger
    {
        DateTime date;
        FileSystemWatcher watcher;
        bool enabled = true;
        List<Log> logs;

        public Logger()
        {
            date = DateTime.Now;
            logs = new List<Log>();

            watcher = new FileSystemWatcher(ConfigurationManager.AppSettings.Get("directory"));

            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
        }

        public void Start()
        {
            watcher.EnableRaisingEvents = true;
            while (enabled)
            {
                Thread.Sleep(1000);

                if (DateTime.Now >= date.AddMinutes(30))
                {
                    date = DateTime.Now;
                    Log[] _logs = logs.ToArray();
                    if (_logs.Length != 0)
                    {
                        using (NpgsqlConnection db = new NpgsqlConnection(ConfigurationManager.AppSettings.Get("db")))
                        {
                            db.Open();
                            for (int i = 0; i < _logs.Length; i++)
                            {
                                NpgsqlCommand command = new NpgsqlCommand(String.Format("INSERT INTO logs (action, file, log_date) VALUES('{0}', '{1}', '{2}');", _logs[i].FileEvent, _logs[i].FilePath, _logs[i].time.ToString()), db);
                                command.ExecuteNonQuery();
                            }
                            db.Close();
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            watcher.EnableRaisingEvents = false;
            enabled = false;
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            PrintLog("Переименование", e.Name);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            PrintLog("Удаление", e.Name);
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            PrintLog("Создание", e.Name);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            PrintLog("Изменение", e.Name);
        }

        private void PrintLog(string fileEvent, string filePath) 
        {
            logs.Add(new Log(filePath, fileEvent));
        }
    }
}
