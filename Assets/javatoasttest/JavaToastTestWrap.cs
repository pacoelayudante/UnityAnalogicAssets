using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JavaToastTestWrap : MonoBehaviour
{
    [field: SerializeField]
    public string message { get; set; }

    public void ShowAndroidToastMessage()
    {
#if UNITY_EDITOR
        if (Application.isEditor) return;
#endif

        using (var javaUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (var androidPlugin = new AndroidJavaObject("com.RSG.AndroidPlugin.AndroidPlugin", currentActivity))
                {
                    var batlvl = androidPlugin.Call<float>("GetBatteryPct").ToString("0.00");
                    message += $" y la bateria es : {batlvl}";
                }
            }
        }

#if UNITY_ANDROID
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        if (unityActivity != null)
        {
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, message, 0);
                toastObject.Call("show");
            }));
        }

        Debug.Log($"mostrando un toast ficticio de {message}");
#endif
    }
}
