using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenCvSharp;

public class HoughLinesWindow : FiltroGenericoWindow
{
    public MatUnityHandle inputMatHandle = new MatUnityHandle();

    public Texture2D input;
    public double rho = 1d;
    public double thetaEnGrados = 1d;
    public int threshold = 15;
    public double minLineLength = 50d;
    public double maxLineGap = 20d;

    public Texture2D output;

    [MenuItem("OpenCV/Hough Lines")]
    private static void Open() => GetWindow<HoughLinesWindow>();

    public void Procesar()
    {
        Mat inputMat = inputMatHandle == null ? null : inputMatHandle.Mat;

        using var inputMatFromTexture = inputMat == null ? OpenCvSharp.Unity.TextureToMat(input) : new Mat();
        if (inputMat == null)
            inputMat = inputMatFromTexture;

        if (inputMat.Channels() > 1)
            Cv2.CvtColor(inputMat, inputMat, ColorConversionCodes.BGR2GRAY);

        var theta = System.Math.PI * thetaEnGrados / 180d;
        var segments = Cv2.HoughLinesP(inputMat, rho, theta, threshold, minLineLength, maxLineGap);

        using var inputClone = inputMat.Clone();
        using var outputMat = new Mat();
        if (inputClone.Channels() == 1)
            Cv2.CvtColor(inputClone, inputClone, ColorConversionCodes.GRAY2BGR);

        using var outputLines = inputClone.Clone();
        outputLines.SetTo(Scalar.Black);
        foreach (var line in segments)
        {
            Cv2.Line(outputLines, line.P1.X, line.P1.Y, line.P2.X, line.P2.Y, Scalar.Blue, 5);
        }

        Cv2.AddWeighted(inputClone, 0.8d, outputLines, 1d, 0d, outputMat);

        // var outputMat = outputMatHandle == null ? null : outputMatHandle.Mat;
        // if (outputMat == null)
        // {
        //     outputMatHandle.AttachMat(outputMat = new Mat());
        // }

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
