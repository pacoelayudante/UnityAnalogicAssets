using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenCvSharp;
using System.Linq;

public class TestMultiMatDisplay : EditorWindow
{
    [MenuItem("Lab/Test Multi Imagen")]
    public static void Abrir() => GetWindow<TestMultiMatDisplay>();

    public Texture2D[] _texturas = new Texture2D[0];
    public float _escalaMat = .3f;
    public int ancho = 300;
    public int alto = 300;
    public int cols = 3;

    SerializedObject serializedObject;
    private void OnEnable()
    {
        serializedObject = new SerializedObject(this);
    }

    void OnGUI()
    {
        serializedObject.Update();
        var iter = serializedObject.GetIterator();
        iter.Next(true);
        while (iter.NextVisible(false))
            EditorGUILayout.PropertyField(iter);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        EditorGUI.BeginDisabledGroup(_texturas.Length == 0);
        if (GUILayout.Button("Probar"))
        {
            var mats = _texturas.Select(t2d => OpenCvSharp.Unity.TextureToMat(t2d)).ToArray();
            var t2d = UtilidadesRuntime.GenerarTexturaMultiple(ancho, alto, mats, _escalaMat, cols);
            VerTexturaSola.Mostrar(t2d, true, true);
            foreach (var mat in mats)
                mat.Dispose();
        }
        EditorGUI.EndDisabledGroup();
    }
}
