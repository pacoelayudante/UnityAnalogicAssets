using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenCvSharp;

public class AdaptiveThresholdingWindow : FiltroGenericoWindow
{
    public MatUnityHandle inputMatHandle = new MatUnityHandle();

    public Texture2D input;
    public Texture2D output;
    public double maxValue = 255d;
    public AdaptiveThresholdTypes adaptiveThresholdTypes = AdaptiveThresholdTypes.GaussianC;
    public ThresholdTypes thresholdTypes = ThresholdTypes.Binary;
    public int blockSize = 3;
    public double c = 0;

    public MatUnityHandle outputMatHandle = new MatUnityHandle();

    [MenuItem("OpenCV/Adaptive Threshold")]
    private static void Open() => GetWindow<AdaptiveThresholdingWindow>();

    public void Procesar()
    {
        Mat inputMat = inputMatHandle == null ? null : inputMatHandle.Mat;

        using var inputMatFromTexture = inputMat == null ? OpenCvSharp.Unity.TextureToMat(input) : new Mat();
        if (inputMat == null)
            inputMat = inputMatFromTexture;

        if (inputMat.Channels() > 1)
            Cv2.CvtColor(inputMat, inputMat, ColorConversionCodes.BGR2GRAY);

        var outputMat = outputMatHandle == null ? null : outputMatHandle.Mat;
        if (outputMat == null)
        {
            outputMatHandle.AttachMat(outputMat = new Mat());
        }

        if (blockSize < 3)
            blockSize = 3;
        else if (blockSize % 2 == 0)
            blockSize++;

        Cv2.AdaptiveThreshold(inputMat, outputMat, maxValue, adaptiveThresholdTypes, thresholdTypes, blockSize, c);

        if (output != null)
            DestroyImmediate(output);

        output = OpenCvSharp.Unity.MatToTexture(outputMat);
    }

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
