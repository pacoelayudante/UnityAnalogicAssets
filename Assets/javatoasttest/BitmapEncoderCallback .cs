using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class BitmapEncoderCallback : AndroidJavaProxy
{
    private event System.Action<string> _onEncodingComplete;
    private event System.Action<string> _onError;

    public BitmapEncoderCallback(System.Action<string> onEncodingComplete, System.Action<string> onError) : base("com.paco.bitmaptovideoencoder.BitmapToVideoEncoder$IPluginCallback")
    {
        _onEncodingComplete = onEncodingComplete == null ? (string _) => { } : onEncodingComplete;
        _onError = onError == null ? (string _) => { } : onError;
    }

    public void onEncodingComplete(string videoPath)
    {
        Debug.Log("ENTER callback onSuccess: " + videoPath);
        _onEncodingComplete?.Invoke(videoPath);
    }
    public void onError(string errorMessage)
    {
        Debug.Log("ENTER callback onError: " + errorMessage);
        _onError?.Invoke(errorMessage);
    }
}