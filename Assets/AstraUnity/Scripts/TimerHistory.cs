using System.Diagnostics;
using System.Collections.Generic;

class TimerHistory
{
    private Stopwatch _timer = new Stopwatch();
    private long[] _tickHistory;
    private int _head = 0;
    private int _tickCount = 0;
    private int _maxTickCount = 60;
    private float _averageMilliseconds = 0;
    private bool _isPaused = false;

    public TimerHistory()
    {
        _tickHistory = new long[_maxTickCount];
    }

    public void Start()
    {
        if (_isPaused)
        {
            _timer.Start();
            _isPaused = false;
        }
        else
        {
            _timer.Reset();
            _timer.Start();
        }
    }

    public void Pause()
    {
        _timer.Stop();
        _isPaused = true;
    }

    public void Stop()
    {
        _timer.Stop();
        UpdateEventTimeMeasurement();
        _isPaused = false;
    }

    public float AverageMilliseconds
    {
        get
        {
            return _averageMilliseconds;
        }
    }

    private void EnqueueTick(long tick)
    {
        // Make sure there are a max of _maxTickCount in the queue,
        // overwriting the oldest measurements
        _tickHistory[_head] = tick;
        _head = (_head + 1) % _maxTickCount;
        _tickCount++;
        if (_tickCount > _maxTickCount)
        {
            _tickCount = _maxTickCount;
        }
    }

    private void UpdateEventTimeMeasurement()
    {
        long ticks = _timer.ElapsedTicks;
        EnqueueTick(ticks);

        // Total and then average the ticks in the queue
        long totalTicks = 0;

        // Iterate up to the number of ticks we have recorded
        for (int i = 0; i < _tickCount; i++)
        {
            totalTicks += _tickHistory[i];
        }

        float averageTicks = 0.0f;
        if (_tickCount > 0)
        {
            averageTicks = totalTicks / (float)_tickCount;
        }

        _averageMilliseconds = 1000 * averageTicks / (float)Stopwatch.Frequency;
    }
}
