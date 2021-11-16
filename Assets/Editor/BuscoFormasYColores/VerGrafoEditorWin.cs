using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class VerGrafoEditorWin : EditorWindow
{
    GrafoDeContornos _grafo;
    Vector2 _scroll;

    [MenuItem("Lab/Grafo")]
    public static VerGrafoEditorWin Abrir() => GetWindow<VerGrafoEditorWin>();

    public static VerGrafoEditorWin Abrir(GrafoDeContornos grafo)
    {
        var win = VerGrafoEditorWin.CreateWindow<VerGrafoEditorWin>();
        win._grafo = grafo;
        return win;
    }

    void OnGUI()
    {
        if (Event.current.type == EventType.MouseUp && position.Contains(Event.current.mousePosition+position.position) && Event.current.button == 2)
        {
            this.Close();
            return;
        }

        using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = scrollScope.scrollPosition;
            if (_grafo == null)
            {
                EditorGUILayout.HelpBox("Grafo es null", MessageType.Warning);
                return;
            }
            Rect rect = GUILayoutUtility.GetRect(_grafo._tam.x, _grafo._tam.x, _grafo._tam.y, _grafo._tam.y);
            EditorGUI.DrawRect(rect,Color.black);
            GUI.BeginClip(rect);
            Handles.color = Color.yellow;
            foreach(var cont in _grafo._todos) {
                Handles.DrawPolyLine(cont._contornoUnity.ConvertAll(v2=>(Vector3)v2).ToArray());
                Handles.DrawLine(cont._contornoUnity[cont._contornoUnity.Count-1],cont._contornoUnity[0]);
            }
            GUI.EndClip();
        }
    }
}
