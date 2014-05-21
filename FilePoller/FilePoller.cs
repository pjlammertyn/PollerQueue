using System.Collections;
using System.IO;
using System.Threading.Tasks;

namespace Poller
{
    public abstract class FilePoller : Poller<string>
    {
        #region Variables

        #endregion

        #region Constructor

        public FilePoller(string pollerPath)
            : this()
        {
            PollerPath = pollerPath;
        }

        public FilePoller()
        {
            SearchPattern = "*";
            SearchOption = SearchOption.TopDirectoryOnly;
        }

        #endregion

        #region Properties

        public string PollerPath { get; set; }
        public string SearchPattern { get; set; }
        public SearchOption SearchOption { get; set; }

        #endregion

        #region Poller implementation

        protected override void Poll()
        {
            foreach (var file in Directory.EnumerateFiles(PollerPath, SearchPattern, SearchOption))
                BlockingCollection.Add(file);
        }

        protected override async Task<bool> ProcessCurrentItem(string nextItem)
        {
            var normalizedPath = nextItem.Replace("/", "\\").ToLower();

            if (!File.Exists(normalizedPath))
                return true;

            return await ProcessFile(normalizedPath);
        }

        #endregion

        #region Abstract methods

        protected abstract Task<bool> ProcessFile(string path);

        #endregion
    }
}
