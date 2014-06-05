using System;
using System.Collections;
using System.Threading.Tasks;
using System.Timers;

namespace Poller
{
    public abstract class Poller<T> : QueueProcessor<T>
        where T : class
    {
        #region Variables

        Timer pollerTimer;
        bool isPolling;

        #endregion

        #region Constructor

        public Poller()
        {
            PollingInterval = 5000;
            PollOnStart = true;
            OnlyPollOnEmptyQueue = true;
        }

        #endregion

        #region Properties

        public double PollingInterval { get; set; }
        public bool PollOnStart { get; set; }
        public bool OnlyPollOnEmptyQueue { get; set; }

        #endregion

        #region Abstract Methods

        protected abstract Task Poll();

        #endregion

        #region QueueProcessor overriden methods

        protected override void OnStart()
        {
            if (PollOnStart)
                Poll();

            pollerTimer = new System.Timers.Timer();
            pollerTimer.Interval = PollingInterval;
            pollerTimer.Elapsed += new ElapsedEventHandler(pollerTimer_Elapsed);
            pollerTimer.Start();
        }

        protected override void OnStop()
        {
            if (pollerTimer != null)
            {
                if (pollerTimer.Enabled)
                    pollerTimer.Stop();
                pollerTimer.Dispose();
                pollerTimer = null;
            }
        }
        
        #endregion

        #region Events

        async void pollerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!Started || (OnlyPollOnEmptyQueue && BlockingCollection.Count > 0) || isPolling)
                return;

                try
                {
                    //pollerTimer.Enabled = false;
                    isPolling = true;
                    await Poll();
                }
                catch (Exception ex)
                {
                    LogException("Polling failed", ex);
                }
                finally
                {
                    //pollerTimer.Enabled = true;
                    isPolling = false;
                }
        }

        #endregion
    }
}
