#if UNITY_ANDROID && !UNITY_EDITOR
#define ASTRA_UNITY_ANDROID_NATIVE
#endif

using Astra;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

[System.Serializable]
public class NewDepthFrameEvent : UnityEvent<DepthFrame> { }

[System.Serializable]
public class NewColorFrameEvent : UnityEvent<ColorFrame> { }

[System.Serializable]
public class NewBodyFrameEvent : UnityEvent<BodyStream, BodyFrame> { }

[System.Serializable]
public class NewMaskedColorFrameEvent : UnityEvent<MaskedColorFrame> { }

[System.Serializable]
public class NewColorizedBodyFrameEvent : UnityEvent<ColorizedBodyFrame> { }

[System.Serializable]
public class NewBodyMaskEvent : UnityEvent<BodyMask> { }

public class AstraController : MonoBehaviour
{
    public bool AutoRequestAndroidUsbPermission = true;

    private Astra.StreamSet _streamSet;
    private Astra.StreamReader _readerDepth;
    private Astra.StreamReader _readerColor;
    private Astra.StreamReader _readerBody;
    private Astra.StreamReader _readerMaskedColor;
    private Astra.StreamReader _readerColorizedBody;

    private DepthStream _depthStream;
    private ColorStream _colorStream;
    private BodyStream _bodyStream;
    private MaskedColorStream _maskedColorStream;
    private ColorizedBodyStream _colorizedBodyStream;

    bool _isDepthOn = false;
    bool _isColorOn = false;
    bool _isBodyOn = false;
    bool _isMaskedColorOn = false;
    bool _isColorizedBodyOn = false;

    private long _lastBodyFrameIndex = -1;
    private long _lastDepthFrameIndex = -1;
    private long _lastColorFrameIndex = -1;
    private long _lastMaskedColorFrameIndex = -1;
    private long _lastColorizedBodyFrameIndex = -1;

    private int _lastWidth = 0;
    private int _lastHeight = 0;
    private short[] _buffer;
    private int _frameCount = 0;
    private bool _areStreamsInitialized = false;

    private TimerHistory updateFramesTime = new TimerHistory();
    private TimerHistory astraUpdateTime = new TimerHistory();
    private TimerHistory totalFrameTime = new TimerHistory();

    public Text TimeText = null;
    public Toggle ToggleDebugText = null;
    private bool debugTextEnabled = false;

    public NewDepthFrameEvent NewDepthFrameEvent = new NewDepthFrameEvent();
    public NewColorFrameEvent NewColorFrameEvent = new NewColorFrameEvent();
    public NewBodyFrameEvent NewBodyFrameEvent = new NewBodyFrameEvent();
    public NewMaskedColorFrameEvent NewMaskedColorFrameEvent = new NewMaskedColorFrameEvent();
    public NewColorizedBodyFrameEvent NewColorizedBodyFrameEvent = new NewColorizedBodyFrameEvent();
    public NewBodyMaskEvent NewBodyMaskEvent = new NewBodyMaskEvent();

    public Toggle ToggleDepth = null;
    public Toggle ToggleColor = null;
    public Toggle ToggleBody = null;
    public Toggle ToggleMaskedColor = null;
    public Toggle ToggleColorizedBody = null;

    private void Awake()
    {
        Debug.Log("AstraUnityContext.Awake");
        AstraUnityContext.Instance.Initializing += OnAstraInitializing;
        AstraUnityContext.Instance.Terminating += OnAstraTerminating;
        AstraUnityContext.Instance.Initialize();
    }

    void Start()
    {
        if (TimeText != null)
        {
            TimeText.text = "";
        }

        if (ToggleDebugText != null)
        {
            debugTextEnabled = ToggleDebugText.isOn;
        }
    }

    private void OnAstraInitializing(object sender, AstraInitializingEventArgs e)
    {
        Debug.Log("AstraController is initializing");

#if ASTRA_UNITY_ANDROID_NATIVE
        if (AutoRequestAndroidUsbPermission)
        {
            Debug.Log("Auto-requesting usb device access.");
            AstraUnityContext.Instance.RequestUsbDeviceAccessFromAndroid();
        }
#endif
        InitializeStreams();
    }

    private void InitializeStreams()
    {
        try
        {
            AstraUnityContext.Instance.WaitForUpdate(AstraBackgroundUpdater.WaitIndefinitely);

            _streamSet = Astra.StreamSet.Open();

            _readerDepth = _streamSet.CreateReader();
            _readerColor = _streamSet.CreateReader();
            _readerBody = _streamSet.CreateReader();
            _readerMaskedColor = _streamSet.CreateReader();
            _readerColorizedBody = _streamSet.CreateReader();

            _depthStream = _readerDepth.GetStream<DepthStream>();

            var depthModes = _depthStream.AvailableModes;
            ImageMode selectedDepthMode = depthModes[0];

    #if ASTRA_UNITY_ANDROID_NATIVE
            int targetDepthWidth = 160;
            int targetDepthHeight = 120;
            int targetDepthFps = 30;
    #else
            int targetDepthWidth = 320;
            int targetDepthHeight = 240;
            int targetDepthFps = 30;
    #endif

            foreach (var m in depthModes)
            {
                if (m.Width == targetDepthWidth &&
                    m.Height == targetDepthHeight &&
                    m.FramesPerSecond == targetDepthFps)
                {
                    selectedDepthMode = m;
                    break;
                }
            }

            _depthStream.SetMode(selectedDepthMode);

            _colorStream = _readerColor.GetStream<ColorStream>();

            var colorModes = _colorStream.AvailableModes;
            ImageMode selectedColorMode = colorModes[0];

    #if ASTRA_UNITY_ANDROID_NATIVE
            int targetColorWidth = 320;
            int targetColorHeight = 240;
            int targetColorFps = 30;
    #else
            int targetColorWidth = 640;
            int targetColorHeight = 480;
            int targetColorFps = 30;
    #endif

            foreach (var m in colorModes)
            {
                if (m.Width == targetColorWidth &&
                    m.Height == targetColorHeight &&
                    m.FramesPerSecond == targetColorFps)
                {
                    selectedColorMode = m;
                    break;
                }
            }

            _colorStream.SetMode(selectedColorMode);

            _bodyStream = _readerBody.GetStream<BodyStream>();

            _maskedColorStream = _readerMaskedColor.GetStream<MaskedColorStream>();

            _colorizedBodyStream = _readerColorizedBody.GetStream<ColorizedBodyStream>();

            _areStreamsInitialized = true;
        }
        catch (AstraException e)
        {
            Debug.Log("AstraController: Couldn't initialize streams: " + e.ToString());
            UninitializeStreams();
        }
    }

    private void OnAstraTerminating(object sender, AstraTerminatingEventArgs e)
    {
        Debug.Log("AstraController is tearing down");
        UninitializeStreams();
    }

    private void UninitializeStreams()
    {
        AstraUnityContext.Instance.WaitForUpdate(AstraBackgroundUpdater.WaitIndefinitely);

        Debug.Log("AstraController: Uninitializing streams");
        if (_readerDepth != null)
        {
            _readerDepth.Dispose();
            _readerColor.Dispose();
            _readerBody.Dispose();
            _readerMaskedColor.Dispose();
            _readerColorizedBody.Dispose();
            _readerDepth = null;
            _readerColor = null;
            _readerBody = null;
            _readerMaskedColor = null;
            _readerColorizedBody = null;
        }

        if (_streamSet != null)
        {
            _streamSet.Dispose();
            _streamSet = null;
        }
    }

    private void CheckDepthReader()
    {
        // Assumes AstraUnityContext.Instance.IsUpdateAsyncComplete is already true

        ReaderFrame frame;
        if (_readerDepth.TryOpenFrame(0, out frame))
        {
            using (frame)
            {
                DepthFrame depthFrame = frame.GetFrame<DepthFrame>();

                if (depthFrame != null)
                {
                    if(_lastDepthFrameIndex != depthFrame.FrameIndex)
                    {
                        _lastDepthFrameIndex = depthFrame.FrameIndex;

                        NewDepthFrameEvent.Invoke(depthFrame);
                    }
                }
            }
        }
    }

    private void CheckColorReader()
    {
        // Assumes AstraUnityContext.Instance.IsUpdateAsyncComplete is already true

        ReaderFrame frame;
        if (_readerColor.TryOpenFrame(0, out frame))
        {
            using (frame)
            {
                ColorFrame colorFrame = frame.GetFrame<ColorFrame>();

                if (colorFrame != null)
                {
                    if(_lastColorFrameIndex != colorFrame.FrameIndex)
                    {
                        _lastColorFrameIndex = colorFrame.FrameIndex;

                        NewColorFrameEvent.Invoke(colorFrame);
                    }
                }
            }
        }
    }

    private void CheckBodyReader()
    {
        // Assumes AstraUnityContext.Instance.IsUpdateAsyncComplete is already true

        ReaderFrame frame;
        if (_readerBody.TryOpenFrame(0, out frame))
        {
            using (frame)
            {
                BodyFrame bodyFrame = frame.GetFrame<BodyFrame>();

                if (bodyFrame != null)
                {
                    if(_lastBodyFrameIndex != bodyFrame.FrameIndex)
                    {
                        _lastBodyFrameIndex = bodyFrame.FrameIndex;

                        NewBodyFrameEvent.Invoke(_bodyStream, bodyFrame);
                        NewBodyMaskEvent.Invoke(bodyFrame.BodyMask);
                    }
                }
            }
        }
    }

    private void CheckMaskedColorReader()
    {
        // Assumes AstraUnityContext.Instance.IsUpdateAsyncComplete is already true

        ReaderFrame frame;
        if (_readerMaskedColor.TryOpenFrame(0, out frame))
        {
            using (frame)
            {
                MaskedColorFrame maskedColorFrame = frame.GetFrame<MaskedColorFrame>();

                if (maskedColorFrame != null)
                {
                    if(_lastMaskedColorFrameIndex != maskedColorFrame.FrameIndex)
                    {
                        _lastMaskedColorFrameIndex = maskedColorFrame.FrameIndex;

                        NewMaskedColorFrameEvent.Invoke(maskedColorFrame);
                    }
                }
            }
        }
    }

    private void CheckColorizedBodyReader()
    {
        // Assumes AstraUnityContext.Instance.IsUpdateAsyncComplete is already true

        ReaderFrame frame;
        if (_readerColorizedBody.TryOpenFrame(0, out frame))
        {
            using (frame)
            {
                ColorizedBodyFrame colorizedBodyFrame = frame.GetFrame<ColorizedBodyFrame>();

                if (colorizedBodyFrame != null)
                {
                    if(_lastColorizedBodyFrameIndex != colorizedBodyFrame.FrameIndex)
                    {
                        _lastColorizedBodyFrameIndex = colorizedBodyFrame.FrameIndex;

                        NewColorizedBodyFrameEvent.Invoke(colorizedBodyFrame);
                    }
                }
            }
        }
    }

    private bool UpdateUntilDelegate()
    {
        return true;
        // Check if any readers have new frames.
        // StreamReader.HasNewFrame() is thread-safe and can be called
        // from any thread.
        bool hasNewFrameDepth = _readerDepth != null && _readerDepth.HasNewFrame();
        bool hasNewFrameColor = _readerColor != null && _readerColor.HasNewFrame();
        bool hasNewFrameBody = _readerBody != null && _readerBody.HasNewFrame();
        bool hasNewFrameMaskedColor = _readerMaskedColor != null && _readerMaskedColor.HasNewFrame();
        bool hasNewFrameColorizedBody = _readerColorizedBody != null && _readerColorizedBody.HasNewFrame();

        Debug.Log("ND: " + hasNewFrameDepth +
                  " NC: " + hasNewFrameColor +
                  " NB: " + hasNewFrameBody +
                  " NMC: " + hasNewFrameMaskedColor +
                  " NCB: " + hasNewFrameColorizedBody);
        Debug.Log("DO: " + _isDepthOn +
                  " CO: " + _isColorOn +
                  " BO: " + _isBodyOn +
                  " MCO: " + _isMaskedColorOn +
                  " CBO: " + _isColorizedBodyOn);
        bool hasNewFrame = true;
        if (_isColorizedBodyOn)
        {
            hasNewFrame = hasNewFrameColorizedBody;
        }
        else if (_isMaskedColorOn)
        {
            hasNewFrame = hasNewFrameMaskedColor;
        }
        else if (_isBodyOn)
        {
            hasNewFrame = hasNewFrameBody;
        }
        else if (_isDepthOn)
        {
            hasNewFrame = hasNewFrameDepth;
        }

        if (_isColorOn)
        {
            hasNewFrame = hasNewFrame && hasNewFrameColor;
        }

        // If no streams are started (during start up or shutdown)
        // then allow updateUntil to be complete
        bool noStreamsStarted = !_isDepthOn &&
                                !_isColorOn &&
                                !_isBodyOn &&
                                !_isMaskedColorOn &&
                                !_isColorizedBodyOn;

        return hasNewFrame || noStreamsStarted;
    }

    private void CheckForNewFrames()
    {
        if (AstraUnityContext.Instance.WaitForUpdate(5) && AstraUnityContext.Instance.IsUpdateAsyncComplete)
        {
            // Inside this block until UpdateAsync() call below, we can use the Astra API safely
            updateFramesTime.Start();

            CheckDepthReader();
            CheckColorReader();
            CheckBodyReader();
            CheckMaskedColorReader();
            CheckColorizedBodyReader();

            _frameCount++;

            updateFramesTime.Stop();
        }

        if (!AstraUnityContext.Instance.IsUpdateRequested)
        {
            UpdateStreamStartStop();
            // After calling UpdateAsync() the Astra API will be called from a background thread
            AstraUnityContext.Instance.UpdateAsync(UpdateUntilDelegate);
        }
    }

    void PrintBody(Astra.BodyFrame bodyFrame)
    {
        if (bodyFrame != null)
        {
            Body[] bodies = { };
            bodyFrame.CopyBodyData(ref bodies);
            foreach (Body body in bodies)
            {
                Astra.Joint headJoint = body.Joints[(int)JointType.Head];

                Debug.Log("Body " + body.Id + " COM " + body.CenterOfMass +
                    " Head Depth: " + headJoint.DepthPosition.X + "," + headJoint.DepthPosition.Y +
                    " World: " + headJoint.WorldPosition.X + "," + headJoint.WorldPosition.Y + "," + headJoint.WorldPosition.Z +
                    " Status: " + headJoint.Status.ToString());
            }
        }
    }

    void PrintDepth(Astra.DepthFrame depthFrame,
                    Astra.CoordinateMapper mapper)
    {
        if (depthFrame != null)
        {
            int width = depthFrame.Width;
            int height = depthFrame.Height;
            long frameIndex = depthFrame.FrameIndex;

            //determine if buffer needs to be reallocated
            if (width != _lastWidth || height != _lastHeight)
            {
                _buffer = new short[width * height];
                _lastWidth = width;
                _lastHeight = height;
            }
            depthFrame.CopyData(ref _buffer);

            int index = (int)((width * (height / 2.0f)) + (width / 2.0f));
            short middleDepth = _buffer[index];

            Vector3D worldPoint = mapper.MapDepthPointToWorldSpace(new Vector3D(width / 2.0f, height / 2.0f, middleDepth));
            Vector3D depthPoint = mapper.MapWorldPointToDepthSpace(worldPoint);

            Debug.Log("depth frameIndex: " + frameIndex
                      + " width: " + width
                      + " height: " + height
                      + " middleDepth: " + middleDepth
                      + " wX: " + worldPoint.X
                      + " wY: " + worldPoint.Y
                      + " wZ: " + worldPoint.Z
                      + " dX: " + depthPoint.X
                      + " dY: " + depthPoint.Y
                      + " dZ: " + depthPoint.Z + " frameCount: " + _frameCount);
        }
    }

    private void UpdateStreamStartStop()
    {
        // This methods assumes it is called from a safe location to call Astra API
        _isDepthOn = ToggleDepth == null || ToggleDepth.isOn;
        _isColorOn = ToggleColor == null || ToggleColor.isOn;
        _isBodyOn = ToggleBody == null || ToggleBody.isOn;
        _isMaskedColorOn = ToggleMaskedColor == null || ToggleMaskedColor.isOn;
        _isColorizedBodyOn = ToggleColorizedBody == null || ToggleColorizedBody.isOn;

        if (_isDepthOn)
        {
            _depthStream.Start();
        }
        else
        {
            _depthStream.Stop();
        }

        if (_isColorOn)
        {
            _colorStream.Start();
        }
        else
        {
            _colorStream.Stop();
        }

        if (_isBodyOn)
        {
            _bodyStream.Start();
        }
        else
        {
            _bodyStream.Stop();
        }

        if (_isMaskedColorOn)
        {
            _maskedColorStream.Start();
        }
        else
        {
            _maskedColorStream.Stop();
        }

        if (_isColorizedBodyOn)
        {
            _colorizedBodyStream.Start();
        }
        else
        {
            _colorizedBodyStream.Stop();
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (!_areStreamsInitialized)
        {
            InitializeStreams();
        }

        totalFrameTime.Stop();
        totalFrameTime.Start();

        if (_areStreamsInitialized)
        {
            CheckForNewFrames();
        }

        astraUpdateTime.Start();
        astraUpdateTime.Stop();

        if (ToggleDebugText != null)
        {
            bool newDebugTextEnabled = ToggleDebugText.isOn;

            if (debugTextEnabled && !newDebugTextEnabled)
            {
                // Clear TimeText once if ToggleDebugText was just turned off
                TimeText.text = "";
            }

            debugTextEnabled = newDebugTextEnabled;
        }

        if (TimeText != null && debugTextEnabled)
        {
            BackgroundUpdaterTimings backgroundTimings = AstraUnityContext.Instance.BackgroundTimings;
            float totalFrameMs = totalFrameTime.AverageMilliseconds;
            float astraUpdateMs = backgroundTimings.updateAvgMillis;
            float lockWaitMs = backgroundTimings.lockWaitAvgMillis;
            float updateUntilMs = backgroundTimings.updateUntilAvgMillis;
            float updateFrameMs = updateFramesTime.AverageMilliseconds;
            TimeText.text = "Tot: " + totalFrameMs.ToString("0.0") + " ms\n" +
                            "AU: " + astraUpdateMs.ToString("0.0") + " ms\n" +
                            "LockWait: " + lockWaitMs.ToString("0.0") + " ms\n" +
                            "UpdateUntil: " + updateUntilMs.ToString("0.0") + " ms\n" +
                            "UpdateFr: " + updateFrameMs.ToString("0.0") + " ms\n";
        }
    }

    void OnDestroy()
    {
        Debug.Log("AstraController.OnDestroy");

        AstraUnityContext.Instance.WaitForUpdate(AstraBackgroundUpdater.WaitIndefinitely);

        if (_depthStream != null)
        {
            _depthStream.Stop();
        }

        if (_colorStream != null)
        {
            _colorStream.Stop();
        }

        if (_bodyStream != null)
        {
            _bodyStream.Stop();
        }

        if (_maskedColorStream != null)
        {
            _maskedColorStream.Stop();
        }

        if (_colorizedBodyStream != null)
        {
            _colorizedBodyStream.Stop();
        }

        UninitializeStreams();

        AstraUnityContext.Instance.Initializing -= OnAstraInitializing;
        AstraUnityContext.Instance.Terminating -= OnAstraTerminating;

        AstraUnityContext.Instance.Terminate();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("AstraController handling OnApplicationQuit");
        AstraUnityContext.Instance.Terminate();
    }
}
