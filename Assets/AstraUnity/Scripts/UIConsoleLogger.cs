using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class UIConsoleLogger : MonoBehaviour
{
    public int MaximumLogLines = 25;
    private readonly StringBuilder _outputString = new StringBuilder();
    private readonly Queue<string> _logMessageQueue = new Queue<string>();

    void OnEnable()
    {
        Application.logMessageReceived += AppendToLogBuffer;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= AppendToLogBuffer;
    }

    void AppendToLogBuffer(string logString, string stackTrace, LogType type)
    {
        string formattedLogMessage =
            type == LogType.Exception
                ? string.Format("[{0}] {1}\n{2}\n", type, logString, stackTrace)
                : string.Format("[{0}] {1}\n", type, logString);

        _logMessageQueue.Enqueue(formattedLogMessage);

        _outputString.Append(formattedLogMessage);
        while (_logMessageQueue.Count > MaximumLogLines)
        {
            string oldLogMessage = _logMessageQueue.Dequeue();
            _outputString.Remove(0, oldLogMessage.Length);
        }
    }

    void OnGUI()
    {
        GUILayout.Label(_outputString.ToString());
    }
}
