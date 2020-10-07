using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenCvSharp;

public class DisplayMatWindow : EditorWindow
{
    static Dictionary<string, DisplayMatWindow> wins = new Dictionary<string, DisplayMatWindow>();
    Texture2D text;

    MatType matType;
    string id;

    void OnDestroy()
    {
        if (id!=null && wins.ContainsKey(id) && wins[id]==this) wins.Remove(id);
        if (text) DestroyImmediate(text);
    }

    public static void Mostrar(Mat mat, string id = "")
    {
        DisplayMatWindow win = null;
        if (wins.ContainsKey(id)) win = wins[id];
        else
        {
            win = CreateWindow<DisplayMatWindow>(id);
            win.id = id;
            wins.Add(id, win);
            // win.position = new UnityEngine.Rect(win.position.position, new Vector2(mat.Width, mat.Height));
        }
        if (win.text) DestroyImmediate(win.text);
        win.text = OpenCvSharp.Unity.MatToTexture(mat);
        win.matType = mat.Type();
    }

    void OnGUI()
    {
        GUILayout.Label($"mat type = {matType}");
        var rect = GUILayoutUtility.GetAspectRect(text.width / (float)text.height);
        EditorGUI.DrawTextureTransparent(rect, text);
    }
}
