using System;
using System.Threading;
using System.Threading.Tasks;
using Dh.AppLauncher.CoreEnvironment;
using Dh.AppLauncher.Logging;

namespace Dh.AppLauncher.Update
{
    public sealed class UpdateBackoffTimer : IDisposable
    {
        private readonly AppEnvironment _env; private readonly UpdateOptions _options; private readonly int _intervalMinutes; private System.Threading.Timer _timer; private int _running;
        public event EventHandler<UpdateSummaryEventArgs> UpdateSummaryAvailable;
        public event EventHandler<ChangedFilesSummaryEventArgs> ChangedFilesSummaryAvailable;
        public event EventHandler<SummaryChangedFilesEventArgs> SummaryChangedFilesAvailable;
        public event EventHandler<UpdateCompletedEventArgs> UpdateCompleted;

        public UpdateBackoffTimer(AppEnvironment env, UpdateOptions options, int intervalMinutes){ _env=env; _options=options; _intervalMinutes = intervalMinutes>0 ? intervalMinutes : 30; }

        public void Start()
        {
            if (_timer!=null) return;
            _timer = new System.Threading.Timer(async _=> await TickAsync().ConfigureAwait(false), null, TimeSpan.FromMinutes(_intervalMinutes), TimeSpan.FromMinutes(_intervalMinutes));
            LogManager.Info("UpdateBackoffTimer started. Interval="+_intervalMinutes);
        }

        private async Task TickAsync()
        {
            if (System.Threading.Interlocked.Exchange(ref _running,1)==1) return;
            try
            {
                var updater = new UpdateManager(_env, _options);
                updater.UpdateSummaryAvailable += (s,e)=> UpdateSummaryAvailable?.Invoke(s,e);
                updater.ChangedFilesSummaryAvailable += (s,e)=> ChangedFilesSummaryAvailable?.Invoke(s,e);
                updater.SummaryChangedFilesAvailable += (s,e)=> SummaryChangedFilesAvailable?.Invoke(s,e);
                updater.UpdateCompleted += (s,e)=> UpdateCompleted?.Invoke(s,e);
                await updater.CheckAndUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            } catch (Exception ex){ LogManager.Error("Backoff tick failed.", ex); }
            finally { System.Threading.Interlocked.Exchange(ref _running,0); }
        }

        public void Dispose(){ try{_timer?.Dispose();}catch{} _timer=null; LogManager.Info("UpdateBackoffTimer disposed."); }
    }
}
