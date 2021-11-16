using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

public class VerTexturaSola : EditorWindow
{
    public static VerTexturaSola Mostrar(Texture2D txt, bool autoDestruir = true, bool instanciar = false)
    {
        var win = instanciar ? EditorWindow.CreateWindow<VerTexturaSola>() : GetWindow<VerTexturaSola>(true);
        if (win.textura && win.autoDestruir)
        {
            DestroyImmediate(win.textura);
        }
        win.textura = txt;
        win.autoDestruir = autoDestruir;
        return win;
    }

    public List<Vector3> data;

    Vector2Int dataCoords;
    float _imagenScale = 1f;
    bool autoDestruir = false;
    bool tamOriginal = false;
    Texture2D textura;
    public Texture2D Textura
    {
        get => textura;
        set
        {
            if (textura && autoDestruir) DestroyImmediate(textura);
            textura = value;
            this.Repaint();
        }
    }
    Vector2 scroll;

    void OnDestroy()
    {
        if (textura && autoDestruir) DestroyImmediate(textura);
    }

    void OnGUI()
    {
        if (!textura)
        {
            this.Close();
            return;
        }

        if (Event.current.type == EventType.MouseUp && position.Contains(Event.current.mousePosition+position.position) && Event.current.button == 2)
        {
            this.Close();
            return;
        }

        _imagenScale = EditorGUILayout.Slider("zoom", _imagenScale, .1f, 4f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        Rect rectDePreview;
        if (tamOriginal)
        {
            rectDePreview = GUILayoutUtility.GetRect((textura.width), (textura.height),GUILayout.Width(textura.width), GUILayout.Height(textura.height));
            EditorGUI.DrawPreviewTexture(rectDePreview, textura);
        }
        else
        {
            var escala = Mathf.Min(position.width / textura.width, position.height / textura.height) * _imagenScale;
            rectDePreview = GUILayoutUtility.GetRect((textura.width * escala), (textura.height * escala), GUILayout.Width(textura.width * escala), GUILayout.Height(textura.height * escala));
            EditorGUI.DrawPreviewTexture(rectDePreview, textura);
        }
        var mp = Event.current.mousePosition;
        EditorGUILayout.EndScrollView();

        //if (data != null && data.Count == textura.width*textura.height) {
        var normMousePos = Rect.PointToNormalized(rectDePreview, mp);
        if (normMousePos.x >= 0f && normMousePos.y >= 0f && normMousePos.x < 1f && normMousePos.y < 1f)
        {
            dataCoords.x = Mathf.FloorToInt(normMousePos.x * textura.width);
            dataCoords.y = Mathf.FloorToInt(normMousePos.y * textura.height);
        }
        int dataX = dataCoords.x;
        int dataY = dataCoords.y;
        int index = dataX + dataY * textura.width;
        if (data != null && data.Count == textura.width * textura.height)
        {
            EditorGUILayout.Vector3Field($"({dataX},{dataY}) - {index}", data[index]);
        }
        else
        {
            GUILayout.Label("No Data");
        }
        normMousePos.y = 1f - normMousePos.y;
        var winSize = 80f;
        var rectSubdata = GUILayoutUtility.GetRect(winSize,winSize,GUILayout.Width(winSize), GUILayout.Height(winSize));
        var tam = 10f * textura.texelSize;
        GUI.DrawTextureWithTexCoords(rectSubdata, textura, Rect.MinMaxRect(normMousePos.x - tam.x, normMousePos.y - tam.y, normMousePos.x + tam.x, normMousePos.y + tam.y));
        //}
    }
}