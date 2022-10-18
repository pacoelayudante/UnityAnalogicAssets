using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;

/// verde por fuera
/// negro y rosa por dentro
public class BlobAccionDisparo : BlobGenerico
{
    public List<GrafoDeContornos.Contorno> _contornosDisparos = new List<GrafoDeContornos.Contorno>();
    public List<GrafoDeContornos.Contorno> _contornosMarcas = new List<GrafoDeContornos.Contorno>();

    public List<Vector2> _disparosEstimados = new List<Vector2>();

    public Vector2 _marcaLejana;

    public int Cantidad => _disparosEstimados.Count;
    public int Indice => _contornosMarcas.Count;

    public bool EsSelector => Cantidad == 0;

    public BlobAccionDisparo _selector;

    public Vector2 PuntoBusqueda => _selector == null ? _contorno.CentroBBox : _selector._marcaLejana;

    public BlobAccionDisparo(GrafoDeContornos.Contorno contorno, Mat _puntosRosa, Mat _puntosNegros):base(contorno) {
        var limiteMenor = Mathf.Min( contorno.EllipseCV.Size.Width , contorno.EllipseCV.Size.Height );

        var distLejana = 0f;
        foreach(var cont in contorno._contenidos) {
            var minTam = Mathf.Min( cont.BBox.width , cont.BBox.height );

            var puntoNegro = _puntosNegros.At<byte>(cont.CentroBBoxCV.Y,cont.CentroBBoxCV.X);
            var puntoRosa = _puntosRosa.At<byte>(cont.CentroBBoxCV.Y,cont.CentroBBoxCV.X);

            if (minTam < limiteMenor/7f)
                continue;

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

        if (EsSelector) {
            //_puntoSelector = contorno._contornoUnity.OrderBy( (a,b)=> a);
            distLejana = 0f;
            _marcaLejana = contorno._contornoUnity[0];
            foreach(var pt in contorno._contornoUnity) {
                var dist = Vector2.Distance(pt, contorno.Centroide);
                if ( dist > distLejana ) {
                    distLejana = dist;
                    _marcaLejana = pt;
                }
            }

            _marcaLejana = Vector2.LerpUnclamped(contorno.Centroide, _marcaLejana, 2f);
        }
        else {
            
        /*if (_disparosEstimados.Count == 2) {// se puede decidir si el el centro o uno de los extremos en base al blob que contiene pero bue.. para despues?
            _disparosEstimados.Insert(1, (_disparosEstimados[0]+_disparosEstimados[1])/2f);
        }*/

        _disparosEstimados.Sort( (vecA, vecB) => Vector2.Distance(vecB,_marcaLejana).CompareTo(Vector2.Distance(vecA,_marcaLejana)) );
        }
    }
}
