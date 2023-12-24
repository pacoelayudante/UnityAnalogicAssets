using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rect = UnityEngine.Rect;
using CvRect = OpenCvSharp.Rect;
using OpenCvSharp;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu]
public class PlasmarEscenario : ScriptableObject
{
    public TokenDetector _tokenDetector;

    public TokenTemplates TokenTemplates => _tokenDetector._tokenTemplates;

    public float defaultDistLaserMiss = 800f;
    public Vector3 rootSceneScale = new Vector3(1f,-1f,1f);

    public void PrepararEscenario(Texture2D input)
    {
        _tokenDetector.Detectar(input, out TokenDetector.Resultados resultados);

        var nuevoEscenario = new GameObject("Escenario Creado");
        nuevoEscenario.hideFlags = HideFlags.DontSave;

        Dictionary<TokenDetector.TokenEncontrado, NaveEnEscena> navesEnEscena = new();

        //foreach (var token in resultados.todosLosTokens)
        for (int i = 0; i < resultados.todosLosTokens.Count; i++)
        {
            var nuevoToken = new GameObject($"{resultados.todosLosTokens[i].TemplateMasPosible.Nombre} {i}");
            nuevoToken.transform.SetParent(nuevoEscenario.transform, worldPositionStays: false);
            nuevoToken.hideFlags = HideFlags.DontSave;
            var naveEnEscena = nuevoToken.AddComponent<NaveEnEscena>();
            naveEnEscena.distLaserMiss = defaultDistLaserMiss;
            naveEnEscena.Inicializar(resultados.todosLosTokens[i]);

            navesEnEscena[resultados.todosLosTokens[i]] = (naveEnEscena);
        }

        foreach (var disparo in resultados.tokensDisparadores)
        {
            var primerTokenValido = disparo.tokenConArmasCercanos.Find(el => !navesEnEscena[el].DisparoPreparado);
            if (primerTokenValido != null)
                navesEnEscena[primerTokenValido].ApuntarDisparo(disparo);
        }

        foreach (var nave in navesEnEscena.Values)
        {
            nave.CalcularRayos();
        }
        
        // el CreateMesh del collider tiene que suceder antes que el flip, asique...
        // mismo los rayos? como que andan mas o menos?
        nuevoEscenario.transform.localScale = rootSceneScale;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PlasmarEscenario))]
    public class PlasmarEscenarioEditor : Editor
    {
        RenderTexture _renderTexture;
        private Texture2D _texturaInput;

        void OnDisable()
        {
            if (_texturaInput != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_texturaInput)))
                DestroyImmediate(_texturaInput);
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                _renderTexture = EditorGUILayout.ObjectField("Render Texture", _renderTexture, typeof(RenderTexture), allowSceneObjects: false) as RenderTexture;
                if (changed.changed && _renderTexture)
                {
                    if (_texturaInput != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_texturaInput)))
                        DestroyImmediate(_texturaInput);

                    _texturaInput = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGBA32, false, true);
                    RenderTexture.active = _renderTexture;
                    _texturaInput.ReadPixels(new Rect(0, 0, _texturaInput.width, _texturaInput.height), 0, 0, false);
                    _texturaInput.Apply(false);
                }
            }
            _texturaInput = (Texture2D)EditorGUILayout.ObjectField("Templates Image", _texturaInput, typeof(Texture2D), allowSceneObjects: false);

            DrawDefaultInspector();
            if (GUILayout.Button("Generar Escena") && _texturaInput)
            {
                ((PlasmarEscenario)target).PrepararEscenario(_texturaInput);
            }
        }
    }
#endif
}
