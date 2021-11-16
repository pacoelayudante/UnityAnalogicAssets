using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;

public class BlobSalidas : BlobGenerico
{
    public List<GrafoDeContornos.Contorno> _contornosSalidas = new List<GrafoDeContornos.Contorno>();

    public List<Vector2> _salidasEstimados = new List<Vector2>();

    //public int Cantidad => _puntos.Count;
    public int Cantidad => _salidasEstimados.Count;

    public BlobSalidas(GrafoDeContornos.Contorno contorno, Mat _puntosNegros):base(contorno) {
        foreach(var cont in contorno._contenidos) {
            var punto = _puntosNegros.At<byte>(cont.CentroBBoxCV.X,cont.CentroBBoxCV.Y);

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
