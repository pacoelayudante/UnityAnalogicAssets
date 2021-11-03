using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using UnityCV = OpenCvSharp.Unity;
using Rect = UnityEngine.Rect;

public class PrimeroFiltro
{
    public class Config
    {
        public int umbralSaturacion;
        public Vector2Int limitesRosa, limitesVerde;
    }

    Config _config;
    Texture2D _textura;
    Mat _mat;
    bool _disposeBaseMat;

    Mat _matGray, _matHSV;
    Mat _matHue, _matSat, _matVal;
    Mat _matSatMask,_matRosaMask,_matVerdeMask,_matRosaVerdeMask;

    List<DisposableObject> _disposearEsto = new List<DisposableObject>();

    public void PrimerFiltro(Config config, Texture2D textura, Mat mat = null)
    {
        _mat = mat;
        _textura = textura;
        _config = config;
        _disposearEsto.Add(_matGray = new Mat());
        _disposearEsto.Add(_matHSV = new Mat());
    }

    public void Procesar()
    {
        if (_mat == null)
        {
            _disposeBaseMat = true;
            _mat = UnityCV.TextureToMat(_textura);
        }

        Cv2.CvtColor(_mat, _matGray, ColorConversionCodes.BGR2GRAY);

        Cv2.CvtColor(_mat, _matHSV, ColorConversionCodes.BGR2HSV);

        Disposear(_matHue, _matSat, _matVal);
        Cv2.Split(_matHSV, out Mat[] canales);
        (_matHue, _matSat, _matVal) = (canales[0], canales[1], canales[2]);

        Cv2.Threshold(_matSat, _matSatMask, _config.umbralSaturacion, 255f, ThresholdTypes.Binary);

        Cv2.InRange(_matSat, _config.limitesRosa[0], _config.limitesRosa[1], _matRosaMask);
        Cv2.InRange(_matSat, _config.limitesVerde[0], _config.limitesVerde[1], _matVerdeMask);

        Cv2.BitwiseAnd(_matSatMask, _matRosaMask, _matRosaMask);
        Cv2.BitwiseAnd(_matSatMask, _matVerdeMask, _matVerdeMask);
        Cv2.BitwiseOr(_matRosaMask, _matVerdeMask, _matRosaVerdeMask);

        //resize y despues copyTo.. para armar la matriz de imagenes
    }

    ~PrimeroFiltro()
    {
        if (_disposeBaseMat && _mat != null && !_mat.IsDisposed)
            _mat.Dispose();

        Disposear(_matHue, _matSat, _matVal);
        Disposear(_disposearEsto.ToArray());
    }

    void Disposear(params DisposableObject[] disposear)
    {
        foreach (var disp in disposear)
        {
            if (disp != null && !disp.IsDisposed)
                disp.Dispose();
        }
    }
}
