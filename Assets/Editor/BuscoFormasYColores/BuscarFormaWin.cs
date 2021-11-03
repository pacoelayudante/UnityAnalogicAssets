using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenCvSharp;
using Rect = UnityEngine.Rect;

public class BuscarFormaWin : EditorWindow
{
    Texture2D _tex2d;
    Mat _mat;

    Vector2 _winScroll;
    Vector2 _imagenScroll;
    float _imagenScale = 1f;

    [MenuItem("Lab/Buscar Forma")]
    static void Abrir() => GetWindow<BuscarFormaWin>();

    public static void AbrirCon(Texture2D tex2d)
    {
        var win = CreateWindow<BuscarFormaWin>();
        win._tex2d = tex2d;
        win._mat = OpenCvSharp.Unity.TextureToMat(tex2d);
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        var newtex = (Texture2D)EditorGUILayout.ObjectField(_tex2d, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck())
        {
            ClearMem();
            _tex2d = newtex;
        }
        if (_tex2d == null)
            return;

        if (_mat == null || _mat.IsDisposed)
        {
            _mat = OpenCvSharp.Unity.TextureToMat(_tex2d);
        }

        _winScroll = EditorGUILayout.BeginScrollView(_winScroll);

        _imagenScale = EditorGUILayout.Slider("zoom", _imagenScale, .1f, 4f);
        _imagenScroll = EditorGUILayout.BeginScrollView(_imagenScroll, GUILayout.MinHeight(position.height / 3f));
        Vector2 tam = new Vector2(_tex2d.width, _tex2d.height) * _imagenScale;
        var imgRect = GUILayoutUtility.GetRect(tam.x, tam.y, GUILayout.Width(tam.x), GUILayout.Height(tam.y));
        EditorGUI.DrawPreviewTexture(imgRect, _tex2d);
        EditorGUILayout.EndScrollView();

        DoThreshold();
        DoCanny();
        DoAdaptiveThresh();
        DoHueAlPalo();
        DoMeanShift();
        EditorGUILayout.EndScrollView();
    }

    float _thresh = 127f;
    float _maxVal = 255f;
    ThresholdTypes _threshType;
    private void DoThreshold()
    {
        if (GUILayout.Button("Threshold"))
        {
            using (Mat dst = new Mat())
            {
                Cv2.CvtColor(_mat, dst, ColorConversionCodes.BGR2GRAY);
                Cv2.Threshold(dst, dst, _thresh, _maxVal, _threshType);
                var win = VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(dst), true, true);

                var data = new List<Vector3>();
                for (int y = 0; y < dst.Height; y++)
                    for (int x = 0; x < dst.Width; x++)
                    {
                        var dataAt = dst.At<ushort>(y,x);
                        data.Add(Vector3.one * (dataAt / (float)ushort.MaxValue));
                    }

                win.data = data;
            }
        }
        _thresh = EditorGUILayout.Slider("Threshold", _thresh, 0f, 255f);
        _maxVal = EditorGUILayout.Slider("Max Value", _maxVal, 0f, 255f);
        _threshType = (ThresholdTypes)EditorGUILayout.EnumPopup("Type", _threshType);
    }

    Vector2 _cannyTresh = new Vector2(255f / 3, 255f * 2f / 3f);
    int _cannySize = 3;
    bool _cannyL2 = false;
    MorphTypes _morphType;
    int _morphSize;
    private void DoCanny()
    {
        if (GUILayout.Button("Canny"))
        {
            using (Mat dst = new Mat(), _morphStruct = _morphSize > 0 ? Cv2.GetStructuringElement(MorphShapes.Rect, new Size(_morphSize, _morphSize)) : null)
            {
                Cv2.CvtColor(_mat, dst, ColorConversionCodes.BGR2GRAY);
                if (_cannySize < 3) _cannySize = 3;
                else if (_cannySize % 2 == 0) _cannySize--;
                Cv2.Canny(dst, dst, _cannyTresh.x, _cannyTresh.y, _cannySize, _cannyL2);
                if (_morphSize > 0)
                {
                    Cv2.MorphologyEx(dst, dst, _morphType, _morphStruct);
                }
                VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(dst), true, true);
            }
        }
        _cannyTresh.x = EditorGUILayout.Slider("Threshold 1", _cannyTresh.x, 0f, 255f);
        _cannyTresh.y = EditorGUILayout.Slider("Threshold 2", _cannyTresh.y, 0f, 255f);
        _cannySize = EditorGUILayout.IntSlider("Size", _cannySize, 3, 63);
        _cannyL2 = EditorGUILayout.Toggle("L2 Gradient", _cannyL2);
        _morphType = (MorphTypes)EditorGUILayout.EnumPopup("After Dilate", _morphType);
        _morphSize = EditorGUILayout.IntSlider("Morphology Size", _morphSize, 0, 9);
    }

    AdaptiveThresholdTypes _adaptiveType;
    float _adaptiveConstant;
    private void DoAdaptiveThresh()
    {
        if (GUILayout.Button("Adapive Thresh"))
        {
            using (Mat dst = new Mat())
            {
                Cv2.CvtColor(_mat, dst, ColorConversionCodes.BGR2GRAY);
                if (_cannySize < 3) _cannySize = 3;
                else if (_cannySize % 2 == 0) _cannySize--;
                Cv2.AdaptiveThreshold(dst, dst, _maxVal, _adaptiveType, _threshType, _cannySize, _adaptiveConstant);
                VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(dst), true, true);
            }
        }
        _maxVal = EditorGUILayout.Slider("Max Value", _maxVal, 0f, 255f);
        _adaptiveConstant = EditorGUILayout.Slider("Constant", _adaptiveConstant, 0f, 255f);
        _adaptiveType = (AdaptiveThresholdTypes)EditorGUILayout.EnumPopup("Type", _adaptiveType);
        _threshType = (ThresholdTypes)EditorGUILayout.EnumPopup("Thresh Type", _threshType);
        _cannySize = EditorGUILayout.IntSlider("Size", _cannySize, 3, 63);
    }

    public enum TipoColor
    {
        HSV, HLS
    }
    TipoColor _tipoColor;
    bool _setValueMax;
    Vector2 _rangeMagica;
    private void DoHueAlPalo()
    {
        if (GUILayout.Button("Hue Al Palo"))
        {
            using (Mat dst = new Mat())
            {
                Cv2.CvtColor(_mat, dst, _tipoColor == TipoColor.HSV ? ColorConversionCodes.BGR2HSV : ColorConversionCodes.BGR2HLS);
                var splits = dst.Split();

                var data = new List<Vector3>();
                for (int y = 0; y < dst.Height; y++)
                    for (int x = 0; x < dst.Width; x++)
                    {
                        var dataAt = splits[0].At<char>(y,x);
                        //data.Add(Vector3.one * (dataAt / (float)ushort.MaxValue));
                        data.Add(Vector3.one * dataAt);
                    }

                var sat = _tipoColor == TipoColor.HSV ? splits[1] : splits[2];
                var brillo = _tipoColor == TipoColor.HSV ? splits[2] : splits[1];
                Cv2.Threshold(sat, sat, _thresh, 255f, _threshType);
                if (_setValueMax)
                    Cv2.Threshold(sat, brillo, _thresh, _maxVal, ThresholdTypes.Binary);
                //brillo.SetTo(_maxVal);

                Mat laplacian = new Mat();
                if (_rangeMagica.x != _rangeMagica.y)
                    Cv2.InRange(splits[0], _rangeMagica.x, _rangeMagica.y, laplacian);

                Cv2.Merge(splits, dst);
                Cv2.CvtColor(dst, dst, _tipoColor == TipoColor.HSV ? ColorConversionCodes.HSV2BGR : ColorConversionCodes.HLS2BGR);
                var win = VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(dst), true, true);
                win.data = data;

                /*Cv2.Laplacian(splits[0], laplacian, MatType.CV_16S);

                data.Clear();
                for (int y = 0; y < dst.Height; y++)
                    for (int x = 0; x < dst.Width; x++)
                    {
                        var dataAt = splits[0].At<short>(y,x);
                        //data.Add(Vector3.one * (dataAt / (float)ushort.MaxValue));
                        data.Add(Vector3.one * dataAt);
                    }*/

                //else
                //    Cv2.ConvertScaleAbs(laplacian, splits[0]);
                win = VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(laplacian), true, true);
                //win.data = data;

                laplacian.Dispose();
                foreach (var split in splits)
                    split.Dispose();
            }
        }
        _tipoColor = (TipoColor)EditorGUILayout.EnumPopup("Type", _tipoColor);
        _setValueMax = EditorGUILayout.Toggle("Set Value Max", _setValueMax);
        _rangeMagica = EditorGUILayout.Vector2Field("_rangeMagica", _rangeMagica);
    }

    float sigmaA, sigmaB;
    int maxLevelPyr;
    TermCriteria termCriteria;
    private void DoMeanShift()
    {
        if (GUILayout.Button("Mean Shift"))
        {
            using (Mat dst = new Mat())
            {
                Cv2.PyrMeanShiftFiltering(_mat, dst, sigmaA, sigmaB, maxLevelPyr);
                VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(dst), true, true);
            }
        }
        sigmaA = EditorGUILayout.Slider("sigmaA", sigmaA, 0f, 15f);
        sigmaB = EditorGUILayout.Slider("sigmaB", sigmaB, 0f, 100f);
        maxLevelPyr = EditorGUILayout.IntSlider("maxLevelPyr", maxLevelPyr, 0, 4);
    }

    private void OnDestroy() => ClearMem();
    private void ClearMem()
    {
        if (_tex2d && !AssetDatabase.IsMainAsset(_tex2d))
            DestroyImmediate(_tex2d);

        if (_mat != null)
            _mat.Dispose();
    }
}
