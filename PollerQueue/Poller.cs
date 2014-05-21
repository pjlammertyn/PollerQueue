﻿using System;
using System.Collections;
using System.Timers;

namespace Poller
{
    public abstract class Poller<T> : QueueProcessor<T>
        where T : class
    {
        #region Variables

        Timer pollerTimer;

        #endregion

        #region Constructor

        public Poller()
        {
            PollingInterval = 5000;
            PollOnStart = true;
        }

        #endregion

        #region Properties

        public double PollingInterval { get; set; }
        public bool PollOnStart { get; set; }

        #endregion

        #region Abstract Methods

        protected abstract void Poll();

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

        void pollerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!Started || BlockingCollection.Count > 0)
                return;

                //pollerTimer.Enabled = false;

                try
                {
                    Poll();
                }
                catch (Exception ex)
                {
                    LogException("Polling failed", ex);
                }
                finally
                {
                    //pollerTimer.Enabled = true;
                }
        }

        #endregion
    }
}
