using System;
using UnityEngine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Object = UnityEngine.Object;

//This allows us to keep Debug logs inside of our Release builds
//So we don't have to pepper logging with constant if(Debug.*...) on all logging calls.
//DO NOTE this hide all of the extra Debug utilities underlying
//See: https://answers.unity.com/questions/126315/debuglog-in-build.html?childToView=1008283#answer-1008283
//for what's missing
public static class Debug
{
    // Summary:
    //     Opens or closes developer console.
    public static bool developerConsoleVisible { get { return UnityEngine.Debug.developerConsoleVisible; } set { UnityEngine.Debug.developerConsoleVisible = value; } }

    public static ILogger unityLogger
    {
        get { return UnityEngine.Debug.unityLogger; }
    }

    // Summary:
    //     In the Build Settings dialog there is a check box called "Development Build".
    public static bool isDebugBuild { get { return UnityEngine.Debug.isDebugBuild; } }
    
    public static void Break()
    {
        UnityEngine.Debug.Break();
    }
    public static void ClearDeveloperConsole()
    {
        UnityEngine.Debug.ClearDeveloperConsole();
    }
    public static void DebugBreak()
    {
        UnityEngine.Debug.DebugBreak();
    }
    
    [Conditional("DEBUG")]
    public static void Log(string message, Object obj = null)
    {
        UnityEngine.Debug.Log(message, obj);
    }

    [Conditional("DEBUG")]
    public static void LogWarning(string message, Object obj = null)
    {
        UnityEngine.Debug.LogWarning(message, obj);
    }

    public static void LogError(string message, Object obj = null)
    {
        UnityEngine.Debug.LogError(message, obj);
    }

    public static void LogException(System.Exception e)
    {
        UnityEngine.Debug.LogError(e.Message);
    }
}

namespace UrbUtility
{
    //Had big ideas about scoped logging but uh
    //It didn't work the way I expected... Will revisit later.
    public class UrbLogger : Logger
    {
        public UrbLogger(ILogHandler logHandler) : base(logHandler)
        {
        }

        public void ToggleDebug()
        {
            logEnabled = !logEnabled;
        }

        public void Log(string message, Object context)
        {
            logHandler.LogFormat(LogType.Log, context, message);
        }
        
        public void Error(string message, Object context)
        {
            logHandler.LogFormat(LogType.Error, context, "{0}: {1}", (object) "Error", (object) message);
        }
    }
    
}