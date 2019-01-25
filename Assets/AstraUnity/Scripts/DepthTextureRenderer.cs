using UnityEngine;
using UnityEngine.UI;

public class DepthTextureRenderer : MonoBehaviour
{
    private Texture2D _texture;
    private Color[] _textureBuffer;

    private long _lastFrameIndex = -1;
    private short[] _depthFrameData;

    private void Start()
    {
        _textureBuffer = new Color[320 * 240];
        _depthFrameData = new short[320 * 240];
        _texture = new Texture2D(320, 240);
        GetComponent<Renderer>().material.mainTexture = _texture;
    }

    public void OnNewFrame(Astra.DepthFrame frame)
    {
        if (frame.Width == 0 ||
            frame.Height == 0)
        {
            return;
        }

        if (_lastFrameIndex == frame.FrameIndex)
        {
            return;
        }

        _lastFrameIndex = frame.FrameIndex;

        EnsureBuffers(frame.Width, frame.Height);
        frame.CopyData(ref _depthFrameData);

        MapDepthToTexture(_depthFrameData);
    }

    private void EnsureBuffers(int width, int height)
    {
        int length = width * height;
        if (_textureBuffer.Length != length)
        {
            _textureBuffer = new Color[length];
        }

        if (_depthFrameData.Length != length)
        {
            _depthFrameData = new short[length];
        }

        if (_texture != null)
        {
            if (_texture.width != width ||
                _texture.height != height)
            {
                _texture.Resize(width, height);
            }
        }
    }

    void MapDepthToTexture(short[] depthPixels)
    {
        int length = depthPixels.Length;
        for (int i = 0; i < length; i++)
        {
            short depth = depthPixels[i];

            float depthScaled = 0.0f;
            if (depth != 0)
            {
                depthScaled = 1.0f - (depth / 10000.0f);
            }

            _textureBuffer[i].r = depthScaled;
            _textureBuffer[i].g = depthScaled;
            _textureBuffer[i].b = depthScaled;
            _textureBuffer[i].a = 1.0f;
        }

        _texture.SetPixels(_textureBuffer);
        _texture.Apply();
    }
}