using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class BitmapEncoderCallback : AndroidJavaProxy
{
    private event System.Action<string> _onSuccess;
    private event System.Action<string> _onEncodingComplete;

    public BitmapEncoderCallback(System.Action<string> onSuccess, System.Action<string> onEncodingComplete) : base("com.paco.bitmaptovideoencoder.BitmapToVideoEncoder$PluginCallback")
    {
        _onSuccess = onSuccess == null ? (string _) => { } : onSuccess;
        _onEncodingComplete = onEncodingComplete == null ? (string _) => { } : onEncodingComplete;
    }

    public void onSuccess(string videoPath)
    {
        Debug.Log("ENTER callback onSuccess: " + videoPath);
        _onSuccess?.Invoke(videoPath);
    }
    public void onEncodingComplete(string errorMessage)
    {
        Debug.Log("ENTER callback onError: " + errorMessage);
        _onEncodingComplete?.Invoke(errorMessage);
    }
}