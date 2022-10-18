using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

[System.Serializable]
public class MatUnityHandle : ISerializationCallbackReceiver
{
    private const string RANDOM_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    [SerializeField]
    private string _name = "NULL!";

    public Mat Mat => _mat;
    private Mat _mat;

    ~MatUnityHandle()
    {
        UnityEngine.Debug.Log($"avisar el destroy sucedio: {_name}");
        Clear(setNameToNull: false);
    }

    public Mat AttachMat(Mat attached)
    {
        if (_mat != attached)
        {
            _name = string.Empty;
            for (int i = 0; i < 6; i++)
            {
                _name += RANDOM_CHARS[Random.Range(0, RANDOM_CHARS.Length)];
                if (i == 3)
                    _name += "_";
            }
        }
        return _mat = attached;
    }

    private void Clear(bool setNameToNull = true)
    {
        if (_mat != null)
        {
            _mat.Dispose();
            _mat = null;
        }

        if (setNameToNull)
            _name = "NULL!";
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        if (_mat == null)
            _name = "NULL!";
    }

    // ~MatUnityHandle() // esto no porque que se yo puede ser que el Mat este agarrado por alguna otra cosa ponele
    // {
    //     if (_mat != null)
    //         _mat.Dispose();
    // }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MatUnityHandle))]
    private class MatUnityHandleDrawer : PropertyDrawer
    {
        private const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string MAT_HANDLE_KEY = "MatHandle";

        private MatUnityHandle GetMatUnityHandle(string path, UnityEngine.Object targetObject)
        {
            var type = targetObject.GetType();
            var fieldInfo = type.GetField(path, BINDING_FLAGS);
            return (MatUnityHandle)fieldInfo.GetValue(targetObject);
        }

        private void SetMatUnityHandle(string path, UnityEngine.Object targetObject, MatUnityHandle matHandle)
        {
            var type = targetObject.GetType();
            var fieldInfo = type.GetField(path, BINDING_FLAGS);
            fieldInfo.SetValue(targetObject, matHandle);
        }

        public override void OnGUI(UnityEngine.Rect position, SerializedProperty property, GUIContent label)
        {
            if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition))
            {
                var matHandle = DragAndDrop.GetGenericData(MAT_HANDLE_KEY) as MatUnityHandle;
                DragAndDrop.visualMode = matHandle == null ? DragAndDropVisualMode.Rejected : DragAndDropVisualMode.Link;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform && position.Contains(Event.current.mousePosition))
            {
                var matHandle = DragAndDrop.GetGenericData(MAT_HANDLE_KEY) as MatUnityHandle;
                if (matHandle != null)
                {
                    SetMatUnityHandle(property.propertyPath, property.serializedObject.targetObject, matHandle);
                    DragAndDrop.AcceptDrag();
                }
                Event.current.Use();
            }

            float buttonWidth = EditorGUIUtility.singleLineHeight * 2f;
            position.width -= buttonWidth * 2f;

            var nameProperty = property.FindPropertyRelative(nameof(MatUnityHandle._name));
            EditorGUI.TextField(position, label, nameProperty.stringValue);

            position.x = position.xMax;
            position.width = buttonWidth;
            if (Event.current.type == EventType.MouseDrag && position.Contains(Event.current.mousePosition))
            {
                var matHandle = GetMatUnityHandle(property.propertyPath, property.serializedObject.targetObject);
                if (matHandle != null)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData(MAT_HANDLE_KEY, matHandle);
                    DragAndDrop.StartDrag("Move Mat Handle");
                }
            }
            GUI.Label(position, "ʘ", "button");

            position.x = position.xMax;
            if (GUI.Button(position, "X"))
            {
                var matHandle = GetMatUnityHandle(property.propertyPath, property.serializedObject.targetObject);
                if (matHandle != null)
                    matHandle.Clear();
            }
        }
    }
#endif
}
