using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;

public class BlobAccionDisparo : BlobGenerico
{
    public List<GrafoDeContornos.Contorno> _contornosDisparos = new List<GrafoDeContornos.Contorno>();
    public List<GrafoDeContornos.Contorno> _contornosMarcas = new List<GrafoDeContornos.Contorno>();

    public List<Vector2> _disparosEstimados = new List<Vector2>();

    public int Cantidad => _disparosEstimados.Count;

    public Vector2 _marcaLejana;

    public BlobAccionDisparo(GrafoDeContornos.Contorno contorno, Mat _puntosRosa, Mat _puntosNegros):base(contorno) {
        var distLejana = 0f;
        foreach(var cont in contorno._contenidos) {
            var puntoNegro = _puntosNegros.At<byte>(cont.CentroBBoxCV.X,cont.CentroBBoxCV.Y);
            var puntoRosa = _puntosRosa.At<byte>(cont.CentroBBoxCV.X,cont.CentroBBoxCV.Y);
                Debug.Log($"negro : {puntoNegro} - rosa : {puntoRosa}");

            if (puntoNegro == 0) {
                _contornosMarcas.Add(cont);

                var dist = Vector2.Distance( contorno.CentroBBox , cont.CentroBBox );
                if (dist > distLejana) {
                    distLejana = dist;
                    _marcaLejana = cont.CentroBBox;
                }
            }
            if (puntoRosa > 0) {
                _contornosDisparos.Add(cont);
                _disparosEstimados.Add(cont.CentroBBox);
            }
        }

        if (_disparosEstimados.Count == 2) {// se puede decidir si el el centro o uno de los extremos en base al blob que contiene pero bue.. para despues?
            _disparosEstimados.Insert(1, (_disparosEstimados[0]+_disparosEstimados[1])/2f);
        }

        _disparosEstimados.Sort( (vecA, vecB) => Vector2.Distance(vecA,_marcaLejana).CompareTo(Vector2.Distance(vecB,_marcaLejana)) );
    }
}
