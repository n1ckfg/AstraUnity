using UnityEngine;
using UnityEngine.UI;

public class BodyMaskHitTester : MonoBehaviour
{
    private byte[] _bodyMask;

    public Toggle ToggleBodyMaskHitTest = null;

    private void Start()
    {
        _bodyMask = new byte[320 * 240];
        GetComponent<Renderer>().enabled = false;
    }

    public void OnNewFrame(Astra.BodyMask bodyMask)
    {
        if (ToggleBodyMaskHitTest == null || ToggleBodyMaskHitTest.isOn)
        {
            int width = bodyMask.Width;
            int height = bodyMask.Height;

            EnsureBuffers(width, height);
            bodyMask.CopyData(ref _bodyMask);

            bool isUserInSpot = IsUserInSpot(width, height);

            GetComponent<Renderer>().enabled = isUserInSpot;
        }
    }

    private void Update()
    {
        if (ToggleBodyMaskHitTest != null && !ToggleBodyMaskHitTest.isOn)
        {
            GetComponent<Renderer>().enabled = false;
        }
    }

    private void EnsureBuffers(int width, int height)
    {
        int length = width * height;

        if (_bodyMask.Length != length)
        {
            _bodyMask = new byte[length];
        }
    }

    private bool IsUserInSpot(int width, int height)
    {
        // Scale according to the ratio of 20 pixels in a 160 image
        int boxSize = width * 20 / 160;
        int minY = 0;
        int maxY = minY + boxSize;
        int minX = 0;
        int maxX = minX + boxSize;

        for (int y = minY; y < maxY; y++)
        {
            int yIndex = y * width;
            for (int x = minX; x < maxX; x++)
            {
                int index = x + yIndex;
                if (_bodyMask[index] != 0)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
