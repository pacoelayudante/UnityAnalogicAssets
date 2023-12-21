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
public class ColorBlobs : ScriptableObject
{
    [SerializeField, Range(0, 255)]
    int _hue = 130;
    [SerializeField, Range(1, 60)]
    int _hueTolerance = 12;
    [SerializeField, MinMaxSlider(0, 255)]
    Vector2Int _saturacionValida = new Vector2Int(0, 255);
    [SerializeField, MinMaxSlider(0, 255)]
    Vector2Int _brilloValido = new Vector2Int(0, 255);

    [Space]
    [SerializeField]
    private RetrievalModes _retrievalModes = RetrievalModes.External;
    [SerializeField]
    private ContourApproximationModes _contourApproximationModes = ContourApproximationModes.ApproxTC89KCOS;

    public void FromHueMat(Mat hueInputMat, TipoHue tipoHue, Mat resultadoBinario, out Point[][] resultadoContornos, out HierarchyIndex[] jerarquia, Mat mask = null)
    {
        int hueMin = _hue - _hueTolerance;
        int hueMax = _hue + _hueTolerance;

        // cuando HSV: es hue, saturacion y valor (brillo)
        // cuando HLS: es hue, brillo y saturacion
        var segundoComponente = tipoHue == TipoHue.HSV ? _saturacionValida : _brilloValido;
        var tercerComponente = tipoHue == TipoHue.HLS ? _saturacionValida : _brilloValido;

        var scalarMinimo = new Scalar(hueMin, segundoComponente[0], tercerComponente[0]);
        var scalarMaximo = new Scalar(hueMax, segundoComponente[1], tercerComponente[1]);

        Cv2.InRange(hueInputMat, scalarMinimo, scalarMaximo, resultadoBinario);
        if (mask != null)
            Cv2.Multiply(resultadoBinario, mask, resultadoBinario);

        // contours en esta version destruye la informacion fuente, asique necesito clonarla
        using (Mat clonePrimerFiltro = resultadoBinario.Clone())
        {
            Cv2.FindContours(clonePrimerFiltro, out resultadoContornos, out jerarquia, _retrievalModes, _contourApproximationModes);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ColorBlobs))]
    public class ColorBlobsEditor : Editor
    {
        RenderTexture _renderTexture;

        Texture2D _testTexture;
        Texture2D _testResultado;
        ColorBlobs _colorBlob;
        TipoHue _tipoHue = TipoHue.HSV;

        Rect[] _contornoUV;
        CvRect[] _cvRects;
        Point[][] _contornos;

        private Material material;

        public void OnEnable()
        {
            _colorBlob = (ColorBlobs)target;

            if (material == null)
                // Find the "Hidden/Internal-Colored" shader, and cache it for use.
                material = new Material(Shader.Find("Hidden/Internal-Colored"));
        }

        public void OnDisable()
        {
            DestroyImmediate(_testResultado);
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                using (var changed2 = new EditorGUI.ChangeCheckScope())
                {
                    _renderTexture = EditorGUILayout.ObjectField("Render Texture", _renderTexture, typeof(RenderTexture), allowSceneObjects: false) as RenderTexture;
                    if (changed2.changed && _renderTexture)
                    {
                        if (_testTexture != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_testTexture)))
                            DestroyImmediate(_testTexture);

                        _testTexture = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGBA32, false, true);
                        RenderTexture.active = _renderTexture;
                        _testTexture.ReadPixels(new Rect(0, 0, _testTexture.width, _testTexture.height), 0, 0, false);
                        _testTexture.Apply(false);
                    }
                }
                _testTexture = EditorGUILayout.ObjectField("Input", _testTexture, typeof(Texture2D), allowSceneObjects: false) as Texture2D;
                EditorGUILayout.ObjectField("Resultado", _testResultado, typeof(Texture2D), allowSceneObjects: false);
                _tipoHue = (TipoHue)EditorGUILayout.EnumPopup(_tipoHue);

                DrawDefaultInspector();

                if (changed.changed)
                {
                    if (_testTexture != null)
                    {
                        using (Mat outBlobMat = new Mat())
                        using (Mat testMat = OpenCvSharp.Unity.TextureToMat(_testTexture))
                        {
                            Cv2.CvtColor(testMat, testMat, _tipoHue == TipoHue.HSV ? ColorConversionCodes.BGR2HSV : ColorConversionCodes.BGR2HLS);
                            _colorBlob.FromHueMat(testMat, _tipoHue, outBlobMat, out _contornos, out HierarchyIndex[] jerarquias);

                            _testResultado = OpenCvSharp.Unity.MatToTexture(outBlobMat, _testResultado);

                            _contornoUV = new Rect[_contornos.Length];
                            _cvRects = new CvRect[_contornos.Length];
                            for (int i = 0, count = _contornoUV.Length; i < count; i++)
                            {
                                var cvrect = Cv2.BoundingRect(_contornos[i]);
                                _cvRects[i] = cvrect;
                                _contornoUV[i] = new Rect(cvrect.Left, testMat.Height - cvrect.Bottom, cvrect.Width, cvrect.Height);
                            }

                        }
                    }
                }
            }

            if (_contornoUV != null)
            {
                var queryTextureSize = _testTexture.texelSize;
                // EditorGUILayout.BeginHorizontal();
                //foreach (var uvRect in _contornoUV)
                for (int i = 0; i < _contornoUV.Length; i++)
                {
                    var uvRect = _contornoUV[i];
                    var cvRect = _cvRects[i];

                    var guirect = GUILayoutUtility.GetRect(uvRect.width, uvRect.height, GUILayout.ExpandWidth(false));
                    //if (guirect.xMax > EditorGUIUtility.currentViewWidth - 5f)
                    // {
                    //     EditorGUILayout.EndHorizontal();
                    //     EditorGUILayout.BeginHorizontal();
                    //     guirect = GUILayoutUtility.GetRect(uvRect.width, uvRect.height, GUILayout.ExpandWidth(false));
                    // }
                    GUI.DrawTextureWithTexCoords(guirect, _testTexture, new Rect(uvRect.position * queryTextureSize, uvRect.size * queryTextureSize));
                    DibujarContorno(guirect, -cvRect.TopLeft, Color.magenta, _contornos[i]);
                }
                // EditorGUILayout.EndHorizontal();
            }
        }

        private void DibujarContorno(Rect rect, Point offset, Color color, Point[] contorno)
        {
            if (Event.current.type == EventType.Repaint)
            {
                using (new GUI.ClipScope(rect))
                {
                    GL.PushMatrix();

                    // Clear the current render buffer, setting a new background colour, and set our
                    // material for rendering.
                    GL.Clear(true, false, Color.black);
                    material.SetPass(0);

                    // Start drawing in OpenGL Lines, to draw the lines of the grid.
                    GL.Begin(GL.LINE_STRIP);

                    GL.Color(color);
                    foreach (var p in contorno)
                        GL.Vertex3(p.X + offset.X, p.Y + offset.Y, 0f);
                    GL.End();

                    // Pop the current matrix for rendering, and end the drawing clip.
                    GL.PopMatrix();

                }
            }
        }
    }
#endif
}
