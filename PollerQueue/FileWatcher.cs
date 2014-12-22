using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PollerQueue
{
    public abstract class FileWatcher : QueueProcessor<string>
    {
        #region Variables

        FileSystemWatcher watcher;

        #endregion

        #region Constructor

        public FileWatcher(string watchPath)
        {
            WatchPath = watchPath;
        }

        public FileWatcher()
        {
            Filter = "*";
            //NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName; //The default is the bitwise OR combination of LastWrite, FileName, and DirectoryName
            IncludeSubdirectories = false;
        }

        #endregion

        #region Properties

        public string WatchPath { get; set; }
        public string Filter { get; set; }
        public NotifyFilters NotifyFilter { get; set; }
        public bool IncludeSubdirectories { get; set; }
        public bool HandleCreatedEvent { get; set; }
        public bool HandleDeletedEvent { get; set; }
        public bool HandleRenamedEvent { get; set; }
        public bool HandleChangedEvent { get; set; }
        public bool HandleErrorEvent { get; set; }

        #endregion

        #region QueueProcessor overriden methods

        protected override void OnStart()
        {
            watcher = new FileSystemWatcher();
            watcher.Path = WatchPath;
            watcher.NotifyFilter = NotifyFilter;
            watcher.Filter = Filter;
            watcher.IncludeSubdirectories = IncludeSubdirectories;
            if (HandleCreatedEvent)
                watcher.Created += new FileSystemEventHandler(fileSystem_Event);
            if (HandleDeletedEvent)
                watcher.Deleted += new FileSystemEventHandler(fileSystem_Event);
            if (HandleRenamedEvent)
                watcher.Renamed += new RenamedEventHandler(renamed_Event);
            if (HandleChangedEvent)
                watcher.Changed += new FileSystemEventHandler(fileSystem_Event);
            if (HandleErrorEvent)
                watcher.Error += new ErrorEventHandler(error_Event);
            watcher.EnableRaisingEvents = true;

            var files = Directory.GetFiles(WatchPath, Filter, IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            foreach (var file in files)
                BlockingCollection.Add(file);
        }

        protected override void OnStop()
        {
            if (watcher != null)
            {
                if (HandleCreatedEvent)
                    watcher.Created -= new FileSystemEventHandler(fileSystem_Event);
                if (HandleDeletedEvent)
                    watcher.Deleted -= new FileSystemEventHandler(fileSystem_Event);
                if (HandleRenamedEvent)
                    watcher.Renamed -= new RenamedEventHandler(renamed_Event);
                if (HandleChangedEvent)
                    watcher.Changed -= new FileSystemEventHandler(fileSystem_Event);
                if (HandleErrorEvent)
                    watcher.Error -= new ErrorEventHandler(error_Event);
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
            }
        }
        
        #endregion

        #region Events

        void renamed_Event(object sender, RenamedEventArgs e)
        {
            BlockingCollection.Add(e.FullPath);
        }

        void fileSystem_Event(object sender, FileSystemEventArgs e)
        {
            BlockingCollection.Add(e.FullPath);
        }

        void error_Event(object sender, ErrorEventArgs e)
        {
            Stop();
            Start();
        }

        #endregion
    }
}
