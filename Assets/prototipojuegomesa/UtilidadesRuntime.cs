using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using UnityCV = OpenCvSharp.Unity;
using Rect = UnityEngine.Rect;

public static class UtilidadesRuntime
{
    public static Texture2D GenerarTexturaMultiple(int ancho, int alto, Mat[] mats, float escalaMats, int columnas)
    {
        Mat imagenFinal = new Mat(alto, ancho, MatType.CV_8UC3);
        Mat matEscalado = new Mat();

        int filas = Mathf.FloorToInt(mats.Length / (float)columnas);
        int saltoX = Mathf.FloorToInt(ancho / (float)columnas);
        int saltoY = Mathf.FloorToInt(alto / (float)filas);

        int x = 0;
        int y = 0;

        for (int i = 0, count = mats.Length; i < count; i++)
        {
            var tam = new Size(mats[i].Width * escalaMats, mats[i].Height * escalaMats);
            Cv2.Resize(mats[i], matEscalado, tam);
            matEscalado.CopyTo(new Mat(imagenFinal, new OpenCvSharp.Rect(x, y, tam.Width, tam.Height)));
            if ((i + 1) % columnas == 0)
            {
                x = 0;
                y += saltoY;
            }
            else
            {
                x += saltoX;
            }
        }

        var texturaSalida = UnityCV.MatToTexture(imagenFinal);
        imagenFinal.Dispose();
        matEscalado.Dispose();
        return texturaSalida;
    }
}
