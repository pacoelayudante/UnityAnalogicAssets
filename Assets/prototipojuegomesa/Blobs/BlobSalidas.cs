using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;

/// rosa por fuera
public class BlobSalidas : BlobGenerico
{
    public List<GrafoDeContornos.Contorno> _contornosSalidas = new List<GrafoDeContornos.Contorno>();

    public List<Vector2> _salidasEstimados = new List<Vector2>();

    //public int Cantidad => _puntos.Count;
    public int Cantidad => _salidasEstimados.Count;

    public BlobSalidas(GrafoDeContornos.Contorno contorno, Mat _puntosNegros):base(contorno) {
        var limiteMenor = Mathf.Min( contorno.EllipseCV.Size.Width , contorno.EllipseCV.Size.Height );

        foreach(var cont in contorno._contenidos) {
            //if (cont.PointCount < 5)
            //    continue;
            //var minTam = Mathf.Min( cont.EllipseCV.Size.Width , cont.EllipseCV.Size.Height );

            var minTam = Mathf.Min( cont.BBox.width , cont.BBox.height );

            if (minTam < limiteMenor/5f)
                continue;

            var punto = _puntosNegros.At<byte>(cont.CentroBBoxCV.Y,cont.CentroBBoxCV.X);

            if (punto == 0) {
                _contornosSalidas.Add(cont);
                _salidasEstimados.Add(cont.CentroBBox);
            }
        }

        if (_contornosSalidas.Count == 2) {// se puede decidir si el el centro o uno de los extremos en base al blob que contiene pero bue.. para despues?
            _salidasEstimados.Insert(1, (_salidasEstimados[0]+_salidasEstimados[1])/2f);
        }
    }
}
