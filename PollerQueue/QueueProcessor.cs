using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Poller
{
    public abstract class QueueProcessor<T>
         where T : class
    {
        #region Variables

        CancellationTokenSource token;

        #endregion

        #region Constructor

        public QueueProcessor()
        {
            BlockingCollection = new BlockingCollection<T>();
            QueueProcessingInterval = 1000;
            MaxDegreeOfParallelism = System.Environment.ProcessorCount;
            MaxExceptionCountForCurrentItem = 5;
            MaxExponentialSleepTimeoutWhenExceptionForCurrentItem = 5 * 60000; //5min
        }

        #endregion

        #region Properties

        public int MaxDegreeOfParallelism { get; set; }
        public int QueueProcessingInterval { get; set; }
        public int MaxExceptionCountForCurrentItem { get; set; }
        public int MaxExponentialSleepTimeoutWhenExceptionForCurrentItem { get; set; }
        protected BlockingCollection<T> BlockingCollection { get; private set; }
        protected bool Started { get; private set; }

        #endregion

        #region Abstract Methods

        protected abstract void OnStart();
        protected abstract void OnStop();
        protected abstract Task<bool> ProcessCurrentItem(T currentItem);
        protected abstract void LogException(string message, Exception ex);

        #endregion

        #region Methods
        
        public void Start()
        {
            Stop();

            Started = true;

            OnStart();

            token = new CancellationTokenSource();

            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        foreach (var item in BlockingCollection.GetConsumingEnumerable(token.Token))
                        {
                            if (!Started)
                                return;

                            await DoCurrentItem(item);
                        }
                    }
                    catch (OperationCanceledException)
                    {}
                }, token.Token);
            }
        }

        public void Stop()
        {
            Started = false;

            if (token != null)
                token.Cancel();

            OnStop();        
        }

        async Task DoCurrentItem(T currentItem)
        {
            var exceptionCountForCurrentItem = 0;
            var success = false;
            var exponentialSleepTimeout = 2000;

            while (!success)
            {
                try
                {
                    success = await ProcessCurrentItem(currentItem);
                }
                catch (Exception ex)
                {
                    exceptionCountForCurrentItem++;
                    if (exceptionCountForCurrentItem > MaxExceptionCountForCurrentItem)
                    {
                        LogException("Cannot process current item", ex);
                        success = true;
                    }
                }

                if (!success)
                {
                    await Task.Delay(exponentialSleepTimeout);

                    if (exponentialSleepTimeout >= MaxExponentialSleepTimeoutWhenExceptionForCurrentItem) 
                    {
                        var message = string.Format("PollerQueue exponentialy failed for {0} minutes!", (int)(exponentialSleepTimeout / 60000));
                        LogException(message, new Exception(message));
                    }
                    else
                        exponentialSleepTimeout *= 2; 
                }
            }
        }

        #endregion
    }
}
