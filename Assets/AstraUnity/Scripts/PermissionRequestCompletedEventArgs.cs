using System;

public class PermissionRequestCompletedEventArgs : EventArgs
{
    public bool PermissionGranted { get; private set; }

    public PermissionRequestCompletedEventArgs(bool permissionGranted)
    {
        PermissionGranted = permissionGranted;
    }
}