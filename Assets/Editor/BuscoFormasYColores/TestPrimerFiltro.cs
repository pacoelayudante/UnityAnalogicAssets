using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using OpenCvSharp;
using UnityCV = OpenCvSharp.Unity;
using Rect = UnityEngine.Rect;

public class TestPrimerFiltro : EditorWindow
{
    public PrimerFiltro.Config _config = new PrimerFiltro.Config();
    public Texture2D _textura;

    GrafoDeContornos _grafoRosa, _grafoVerde;

    [MenuItem("Lab/Test Primer Filtro")]
    public static void Abrir() => GetWindow<TestPrimerFiltro>();

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

        EditorGUI.BeginDisabledGroup(_textura == null || !_textura.isReadable);
        if (GUILayout.Button("Test"))
        {
            _config.showDebugPreviews = true;
            var filtroActual = new PrimerFiltro(_config, _textura);
            filtroActual.Procesar();
            _grafoRosa = filtroActual._grafoRosa;
            _grafoVerde = filtroActual._grafoVerde;
            VerTexturaSola.Mostrar(Instantiate(filtroActual.debugPreviewT2D), true, true);
        }
        EditorGUI.EndDisabledGroup();

        using (new GUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledGroupScope(_grafoRosa == null))
            {
                if (GUILayout.Button("Ver Grafo Rosa"))
                {
                    VerGrafoEditorWin.Abrir(_grafoRosa).name = "Grafo Rosa";
                }
            }
            using (new EditorGUI.DisabledGroupScope(_grafoVerde == null))
            {
                if (GUILayout.Button("Ver Grafo Verde"))
                {
                    VerGrafoEditorWin.Abrir(_grafoVerde).name = "Grafo Verde";
                }
            }
        }
    }
}
