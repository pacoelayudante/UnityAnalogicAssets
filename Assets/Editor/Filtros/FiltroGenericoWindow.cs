using UnityEngine;
using UnityEditor;

public class FiltroGenericoWindow : EditorWindow
{
    protected Editor editor;

    protected virtual void OnEnable()
    {
        if (editor == null)
            editor = Editor.CreateEditor(this);
        else
            Editor.CreateCachedEditor(this, editor.GetType(), ref editor);
    }

    protected virtual void OnDestroy()
    {
        if (editor != null)
            DestroyImmediate(editor);
    }

    protected void DrawDefaultInspector()
    {
        if (editor != null)
            editor.DrawDefaultInspector();
    }
}