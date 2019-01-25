#if UNITY_ANDROID && !UNITY_EDITOR
#define ASTRA_UNITY_ANDROID_NATIVE
#endif

using System;
using UnityEngine;

public class AndroidButtonController : MonoBehaviour
{
    private bool _scheduledForTermination = false;

    public bool AndroidQuitOnPause = true;
    public bool AndroidQuitOnBack = true;

#if ASTRA_UNITY_ANDROID_NATIVE

    private const KeyCode AndroidBackButton = KeyCode.Escape;

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused && AndroidQuitOnPause)
        {
            Debug.Log("Scheduling shutdown because AndroidQuitOnPause is enabled");
            _scheduledForTermination = true;
        }
    }
    
    private bool IsBackButtonPressed()
    {
        return Input.GetKey(AndroidBackButton);
    }

    private bool IsQuitRequested()
    {
        return IsBackButtonPressed() && AndroidQuitOnBack;
    }

#else

    private bool IsQuitRequested()
    {
        return false;
    }

#endif

    private void Update()
    {
        if (_scheduledForTermination)
        {
            Application.Quit();
            return;
        }

        if (IsQuitRequested())
        {
            Debug.Log("Scheduling shutdown in response user request");
            _scheduledForTermination = true;
        }
    }
}
