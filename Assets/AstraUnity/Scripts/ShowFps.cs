using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Text))]
public class ShowFps : MonoBehaviour
{
    private Text _fpsTxt = null;

    float deltaTime = 0.0f;
    private int _fps;
    private int _lastfps;

    float bodyUpdateDeltaTime = 0.0f;
    float lastBodyUpdateTime = 0.0f;
    private int _bodyUpdateFps;
    private int _lastBodyUpdateFps;

    public Toggle ToggleDebugText = null;
    private bool debugTextEnabled = false;

    void Start()
    {
        _fpsTxt = GetComponent<Text>();
        _fpsTxt.text = "";
        lastBodyUpdateTime = Time.realtimeSinceStartup;

        if (ToggleDebugText != null)
        {
            debugTextEnabled = ToggleDebugText.isOn;
        }
    }

    void Update()
    {
        deltaTime = (0.1f * Time.deltaTime + 0.9f * deltaTime);

        if (ToggleDebugText != null)
        {
            bool newDebugTextEnabled = ToggleDebugText.isOn;

            if (debugTextEnabled && !newDebugTextEnabled)
            {
                // Clear _fpsTxt once if ToggleDebugText was just turned off
                _fpsTxt.text = "";
            }

            debugTextEnabled = newDebugTextEnabled;
        }

        _fps = (int)(1.0f / deltaTime);
        if (_lastfps != _fps || _bodyUpdateFps != _lastBodyUpdateFps)
        {
            _lastfps = _fps;
            _lastBodyUpdateFps = _bodyUpdateFps;

            if (debugTextEnabled)
            {
                _fpsTxt.text = "Game FPS: " + (_lastfps).ToString("###") +
                               "\nBody FPS: " + (_lastBodyUpdateFps).ToString("###");
            }
        }
    }

    public void OnNewFrame(Astra.BodyStream bodyStream, Astra.BodyFrame frame)
    {
        if (frame.Width == 0 ||
            frame.Height == 0)
        {
            return;
        }
        // Measure time since last body update
        float time = Time.realtimeSinceStartup;
        float delta = time - lastBodyUpdateTime;
        lastBodyUpdateTime = time;
        bodyUpdateDeltaTime = (0.1f * delta + 0.9f * bodyUpdateDeltaTime);

        _bodyUpdateFps = (int)(1.0f / bodyUpdateDeltaTime);
    }
}
