using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenCvSharp;

public class LineSegmentDetectorWindow : FiltroGenericoWindow
{
    public MatUnityHandle inputMatHandle = new MatUnityHandle();
    public Texture2D input;
    
    public LineSegmentDetectorModes mode = LineSegmentDetectorModes.RefineNone;
    
    public Texture2D output;

    [MenuItem("OpenCV/Line Segment Detector")]
    private static void Open() => GetWindow<LineSegmentDetectorWindow>();

    public void Procesar()
    {
        Mat inputMat = inputMatHandle == null ? null : inputMatHandle.Mat;

        using var inputMatFromTexture = inputMat == null ? OpenCvSharp.Unity.TextureToMat(input) : new Mat();
        if (inputMat == null)
            inputMat = inputMatFromTexture;

        using var inputGray = new Mat();
        Cv2.CvtColor(inputMat, inputGray, ColorConversionCodes.BGR2GRAY);

        using var detector = LineSegmentDetector.Create(mode);
        // using var linesDetected = new Mat();
        //detector.Detect(inputGray, linesDetected);
        detector.Detect(inputGray, out Vec4f[] lines, out double[] width, out double[] prec, out double[] nfa);

        using var outputMat = inputMat.Clone();
        foreach(var line in lines)
        {
            int x1 = Mathf.FloorToInt(line[0]);
            int y1 = Mathf.FloorToInt(line[1]);
            int x2 = Mathf.FloorToInt(line[2]);
            int y2 = Mathf.FloorToInt(line[3]);
            Cv2.Line(outputMat, x1, y1, x2, y2, Scalar.Blue, 5);
        }
        // detector.DrawSegments(outputMat, linesDetected);
        // Debug.Log($"r{linesDetected.Rows} - c{linesDetected.Cols} - d{linesDetected.Depth()}");
        // Debug.Log($"r{linesDetected.getf}");

        output = OpenCvSharp.Unity.MatToTexture(outputMat);
        
    }

    public MatUnityHandle outputMatHandle = new MatUnityHandle();

    private void OnGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Procesar"))
        {
            Procesar();
        }

        if (output != null)
        {
            var rect = GUILayoutUtility.GetAspectRect(output.width / (float)output.height);
            EditorGUI.DrawPreviewTexture(rect, output, null, ScaleMode.ScaleToFit);
        }
    }
}
