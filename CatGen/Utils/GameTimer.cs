using System.Diagnostics;

namespace CatGen;

/// <summary>
///     Часы для рендеринга
/// </summary>
public class GameTimer
{
    private readonly double _secondsPerCount;
    private double _deltaTime;

    private long _baseTime;
    private long _pausedTime;
    private long _stopTime;
    private long _prevTime;
    private long _currTime;

    private bool _stopped;

    /// <summary>
    /// Конструктор часов
    /// </summary>
    public GameTimer()
    {
        Debug.Assert(Stopwatch.IsHighResolution, "System does not support high-resolution performance counter");

        _secondsPerCount = 0.0;
        _deltaTime = -1.0;
        _baseTime = 0;
        _pausedTime = 0;
        _prevTime = 0;
        _currTime = 0;
        _stopped = false;

        var countsPerSec = Stopwatch.Frequency;
        _secondsPerCount = 1.0 / countsPerSec;
    }

    /// <summary>
    /// Время, пока часы работали
    /// </summary>
    public float TotalTime
    {
        get {
            if (_stopped)
                return (float)((_stopTime - _pausedTime - _baseTime) * _secondsPerCount);

            return (float)((_currTime - _pausedTime - _baseTime) * _secondsPerCount);
        }
    }

    /// <summary>
    ///     Время между кадрами
    /// </summary>
    public float DeltaTime => (float)_deltaTime;

    /// <summary>
    ///     Сброс часов
    /// </summary>
    public void Reset()
    {
        var curTime = Stopwatch.GetTimestamp();
        _baseTime = curTime;
        _prevTime = curTime;
        _stopTime = 0;
        _stopped = false;
    }

    /// <summary>
    ///     Запустить часы
    /// </summary>
    public void Start()
    {
        var startTime = Stopwatch.GetTimestamp();
        if (_stopped)
        {
            _pausedTime += startTime - _stopTime;
            _prevTime = startTime;
            _stopTime = 0;
            _stopped = false;
        }
    }

    /// <summary>
    ///     Остановить часы
    /// </summary>
    public void Stop()
    {
        if (!_stopped)
        {
            var curTime = Stopwatch.GetTimestamp();
            _stopTime = curTime;
            _stopped = true;
        }
    }

    /// <summary>
    ///     Сделать такт
    /// </summary>
    public void Tick()
    {
        if (_stopped)
        {
            _deltaTime = 0.0;
            return;
        }

        var curTime = Stopwatch.GetTimestamp();
        _currTime = curTime;
        _deltaTime = (_currTime - _prevTime) * _secondsPerCount;

        _prevTime = _currTime;
        if (_deltaTime < 0.0)
            _deltaTime = 0.0;
    }
}
