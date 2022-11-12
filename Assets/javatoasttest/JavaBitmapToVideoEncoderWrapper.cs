using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class JavaBitmapToVideoEncoderWrapper
{
    private static List<string> _frames = new();

    public static void AddFrame(string add)
    {
        _frames.Add(add);
    }

    public static void ClearFrames()
    {
        _frames.Clear();
    }

    public static void Encode(int width, int height, int frameRate, int repeats = 1, System.Action<string> onSuccess = null, System.Action<string> onError = null)
    {
#if UNITY_ANDROID
        if (Application.isEditor) 
        {
            onSuccess?.Invoke("mentira todo fake");
            return;
        }

        using (var javaUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (var androidPlugin = new AndroidJavaObject("com.paco.bitmaptovideoencoder.BitmapToVideoEncoder", new BitmapEncoderCallback(onSuccess, onError)))
                {
                    androidPlugin.Call("startEncoding", width, height, frameRate, $"{Application.persistentDataPath}/savevid.mp4");
                    for (int i = 0; i < repeats; i++)
                    {
                        foreach (var frame in _frames)
                            androidPlugin.Call("queueFrame", frame);
                    }
                    androidPlugin.Call("stopEncoding");
                }
            }
        }
#endif
    }
}
