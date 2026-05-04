using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DeadlockDetector
{
    private static string logPath;
    private static bool initialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        logPath = Application.persistentDataPath + "/heartbeat.txt";
        File.WriteAllText(logPath, "Heartbeat started\n");
        initialized = true;
    }

    public static void Log(string message)
    {
        if (!initialized) return;
        File.AppendAllText(logPath, $"{Time.frameCount}: {message}\n");
    }
}