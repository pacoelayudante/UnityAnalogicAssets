using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenCvSharp;
using Rect = UnityEngine.Rect;

public class TestFindCountoursFloodFill : EditorWindow
{
    Texture2D _tex2d;
    Mat _mat;
    Point[][] points;

    int hierarchySize = 0;
    int justDraw = 0;
        public int umbralSaturacion = 30;
        public int umbralBrillo = 140;
        public Vector2Int limitesRosa = new Vector2Int(135, 165), limitesVerde = new Vector2Int(35, 85);
    
    [MenuItem("Lab/Contorno Labels")]
    static void Abrir() => GetWindow<TestFindCountoursFloodFill>();
    
    public static void AbrirCon(Texture2D tex2d)
    {
        var win = CreateWindow<TestFindCountoursFloodFill>();
        win._tex2d = tex2d;
        win._mat = OpenCvSharp.Unity.TextureToMat(tex2d);
    }

    
    private void OnGUI()
    {
        EditorGUILayout.IntField("hierarchySize",hierarchySize);
        justDraw = EditorGUILayout.IntField("hierarchySize",justDraw);
        umbralSaturacion = EditorGUILayout.IntSlider("umbralSaturacion",umbralSaturacion,0,255);
        umbralBrillo = EditorGUILayout.IntSlider("umbralBrillo",umbralBrillo,0,255);

using (new EditorGUILayout.HorizontalScope())
{
    limitesRosa.x = EditorGUILayout.IntSlider("limitesRosa min",limitesRosa.x,0,255);
    limitesRosa.y = EditorGUILayout.IntSlider("max",limitesRosa.y,0,255);
}
using (new EditorGUILayout.HorizontalScope())
{
    limitesVerde.x = EditorGUILayout.IntSlider("limitesVerde min",limitesVerde.x,0,255);
    limitesVerde.y = EditorGUILayout.IntSlider("max",limitesVerde.y,0,255);
}
        // float min=limitesRosa.x,max=limitesRosa.y;
        // EditorGUILayout.MinMaxSlider("limitesRosa", ref min, ref max, 0, 255);
        // limitesRosa.x = Mathf.FloorToInt(min);
        // limitesRosa.y = Mathf.FloorToInt(max);

        //  min=limitesVerde.x;
        //  max=limitesVerde.y;
        // EditorGUILayout.MinMaxSlider("limitesVerde", ref min, ref max, 0, 255);
        // limitesVerde.x = Mathf.FloorToInt(min);
        // limitesVerde.y = Mathf.FloorToInt(max);

        if (justDraw < 0) justDraw = 0;

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
        
        if (GUILayout.Button("Contornos"))
        {
            using (Mat dst = new Mat())
            {
                Cv2.CvtColor(_mat, dst, ColorConversionCodes.BGR2GRAY);
                dst.ConvertTo(dst, MatType.CV_32SC1);
                Cv2.FindContours(dst, out points, out HierarchyIndex[] hierarchy, RetrievalModes.FloodFill, ContourApproximationModes.ApproxTC89KCOS);
                hierarchySize = hierarchy.Length;
                dst.ConvertTo(dst, MatType.CV_8UC1);
                Cv2.CvtColor(dst, dst, ColorConversionCodes.GRAY2BGR);
                //Cv2.DrawContours(dst, points, -1, new Scalar(255,0,0),1,LineTypes.Link8,hierarchy,1);
                Cv2.DrawContours(dst,new []{points[justDraw%points.Length]}, -1, new Scalar(255,0,0),1);
                var win = VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(dst), true, true);
            }
        }
        if (GUILayout.Button("WaterShed"))
        {
            using (Mat dst = new Mat(_mat.Rows,_mat.Cols,MatType.CV_32SC1), _matHSV = new Mat())
            {
                 Cv2.CvtColor(_mat, _matHSV, ColorConversionCodes.BGR2HSV);
                // Cv2.CvtColor(_mat, dst, ColorConversionCodes.BGR2GRAY);
                dst.SetTo(new Scalar(30f));
                Cv2.Split(_matHSV, out Mat[] canales);
                using (Mat _matHue = canales[0], _matSat = canales[1], _matVal = canales[2], _matSatMask = new Mat())
                {
                    Cv2.Threshold(_matSat, _matSatMask, umbralSaturacion, 255f, ThresholdTypes.Binary);
                    // Debug.Log($"{_matSatMask.Type()} {_matSatMask.Size()} {dst.Size()} {dst.Type()}");
                    // dst.SetTo(new Scalar(50),_matSatMask);
                    Cv2.Add(dst, dst, dst, _matSatMask);
                    using (Mat _matRosaMask = new Mat(), _matVerdeMask = new Mat())
                    {
                        Cv2.InRange(_matHue, limitesRosa[0], limitesRosa[1], _matRosaMask);
                        Cv2.InRange(_matHue, limitesVerde[0], limitesVerde[1], _matVerdeMask);
                    Cv2.Add(dst, dst, dst, _matRosaMask);
                    Cv2.Add(dst, dst, dst, _matVerdeMask);
                    Cv2.Add(dst, dst, dst, _matVerdeMask);
                            //  dst.SetTo(new Scalar(120), _matRosaMask);
                            // dst.SetTo(new Scalar(240), _matVerdeMask);
                // var win = VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(_matSatMask), true, true);
                    }
                }
                // Cv2.CvtColor(dst, dst, ColorConversionCodes.GRAY2BGR);
                // // dst.ConvertTo(dst, MatType.CV_32SC1);
                
                // hierarchySize = hierarchy.Length;
                 dst.ConvertTo(dst, MatType.CV_8UC1);
                 Cv2.CvtColor(dst, dst, ColorConversionCodes.GRAY2BGR);
                // //Cv2.DrawContours(dst, points, -1, new Scalar(255,0,0),1,LineTypes.Link8,hierarchy,1);
                // Cv2.DrawContours(dst,new []{points[justDraw%points.Length]}, -1, new Scalar(255,0,0),1);
                 var win = VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(dst), true, true);
            }
        }
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
