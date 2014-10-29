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
            StartDelayInMillisecondsWhenExceptionForCurrentItem = 2000; //2sec
        }

        #endregion

        #region Properties

        public int MaxDegreeOfParallelism { get; set; }
        public int QueueProcessingInterval { get; set; }
        public int MaxExceptionCountForCurrentItem { get; set; }
        public int StartDelayInMillisecondsWhenExceptionForCurrentItem { get; set; }
        protected BlockingCollection<T> BlockingCollection { get; private set; }
        protected bool Started { get; private set; }

        #endregion

        #region Abstract Methods

        protected abstract void OnStart();
        protected abstract void OnStop();
        protected abstract Task<bool> ProcessCurrentItem(T currentItem);
        protected abstract void LogException(string message, Exception ex, T currentItem);

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
                }, token.Token).ConfigureAwait(false);
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
            var millisecondsDelay = StartDelayInMillisecondsWhenExceptionForCurrentItem;

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
                        LogException(string.Format("Cannot process current item after {0} retries.", MaxExceptionCountForCurrentItem), ex, currentItem);
                        success = true;
                    }
                }

                if (!success)
                {
                    await Task.Delay(millisecondsDelay);

                    millisecondsDelay *= 2; 
                }
            }
        }

        #endregion
    }
}
