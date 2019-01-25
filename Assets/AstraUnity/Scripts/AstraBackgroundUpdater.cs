using Astra;
using System;
using System.Threading;

public sealed class BackgroundUpdaterTimings
{
    public float updateAvgMillis;
    public float lockWaitAvgMillis;
    public float updateUntilAvgMillis;
}

public sealed class AstraBackgroundUpdater
{
    public const int WaitIndefinitely = -1;

    private Thread _workerThread;
    private volatile bool _isStarted = false;
    private Mutex _updateUntilMutex = new Mutex();
    private AutoResetEvent _updateRequestedEvent = new AutoResetEvent(false);
    private AutoResetEvent _updateUntilConditionEvent = new AutoResetEvent(false);

    private bool _updateRequested = false;
    private bool _updateUntilConditionSatisfied = false;
    private Func<bool> _updateUntilCondition;

    private TimerHistory _updateTime = new TimerHistory();
    private TimerHistory _lockWaitTime = new TimerHistory();
    private TimerHistory _updateUntilTime = new TimerHistory();
    private BackgroundUpdaterTimings _timings = new BackgroundUpdaterTimings();

    public BackgroundUpdaterTimings Timings { get { return _timings; } }

    public bool IsUpdateAsyncComplete { get { return _updateUntilConditionSatisfied; } }
    public bool IsUpdateRequested { get { return _updateRequested; } }

    public AstraBackgroundUpdater()
    {
        _updateUntilCondition = () => { return true; };
    }

    ~AstraBackgroundUpdater()
    {

    }

    public void Start()
    {
        // Start the background thread that calls Context.Update() in a loop
        if (_isStarted)
        {
            Stop();
        }

        _workerThread = new Thread(new ThreadStart(ThreadFunc));

        _isStarted = true;
        _workerThread.Start();
    }

    public void Stop()
    {
        // Stop the background thread
        if (!_isStarted)
        {
            return;
        }

        // After setting _isStarted to false, the worker thread will timeout on the
        // WaitOne() and then exit the loop and thread func
        _isStarted = false;

        if (_workerThread != null && _workerThread.ThreadState != ThreadState.Unstarted)
        {
            // Wait for the worker thread to exit
            if (!_workerThread.Join(TimeSpan.FromMilliseconds(1000)))
            {
                // TODO handle faulty thread, log a message
                // _workerThread.Abort();  is a bad idea and could
                // make the SDK unusable
            }
        }

        _workerThread = null;
    }

    public void UpdateAsync(Func<bool> updateUntilCondition)
    {
        _lockWaitTime.Start();
        // _updateUntilMutex guarantees only one UpdateAsync can be in progress at a time
        _updateUntilMutex.WaitOne();
        _lockWaitTime.Stop();

        _updateUntilCondition = updateUntilCondition;
        _updateUntilConditionSatisfied = false;
        _updateRequested = true;

        UpdateTimings();

        _updateUntilMutex.ReleaseMutex();
        _updateRequestedEvent.Set();
    }

    public bool Wait(int timeoutMilliseconds)
    {
        // If we already have updated the condition, the worker thread is done for now so just return
        if (_updateUntilConditionSatisfied) { return true; }

        // If we haven't requested an update, we can't wait for the result
        if (!_updateRequested) { return true; }

        // Otherwise wait for the signal from the worker thread
        _updateUntilConditionEvent.WaitOne(timeoutMilliseconds);
        return _updateUntilConditionSatisfied;
    }

    private void ThreadFunc()
    {
        while(_isStarted)
        {
            // Wait for notification that an update was requested
            if (_updateRequestedEvent.WaitOne(100) && _isStarted)
            {
                _updateUntilMutex.WaitOne();

                _updateUntilTime.Start();

                // Inner update loop will repeat until _updateUntilCondition is true
                // (or if _updateUntilCondition is null, it will update just once)
                while (_isStarted)
                {
                    _updateTime.Start();
                    Context.Update();
                    _updateTime.Stop();

                    // If the caller did not specify an _updateUntilCondition,
                    // or that condition is now true
                    if (_updateUntilCondition == null || _updateUntilCondition())
                    {
                        // break the loop
                        break;
                    }
                }

                _updateUntilTime.Stop();

                UpdateTimings();

                // Allow the main thread to request another update
                _updateUntilConditionSatisfied = true;
                _updateRequested = false;

                _updateUntilMutex.ReleaseMutex();

                // Notify the main thread that the _updateUntilCondition has been satisfied.
                // This will unblock the main thread if it is waiting.
                _updateUntilConditionEvent.Set();
            }
        }
    }

    private void UpdateTimings()
    {
        _timings.updateAvgMillis = _updateTime.AverageMilliseconds;
        _timings.lockWaitAvgMillis = _lockWaitTime.AverageMilliseconds;
        _timings.updateUntilAvgMillis = _updateUntilTime.AverageMilliseconds;
    }
}
