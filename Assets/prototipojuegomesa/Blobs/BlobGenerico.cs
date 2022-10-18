using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobGenerico 
{
    protected GrafoDeContornos.Contorno _contorno;

    public Vector2 Centroide => _contorno.Centroide;

    public Vector2 CentroBBox => _contorno.CentroBBox;
    public OpenCvSharp.Point CentroBBoxCV => _contorno.CentroBBoxCV;

    public BlobGenerico(GrafoDeContornos.Contorno contorno) {
        _contorno = contorno;
    }
}
