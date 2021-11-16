using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;

public class PastillaBicolor
{
    public GrafoDeContornos.Contorno _verde,_rosa;
    public float _distancia;

    public Vector2 CentroRosa => _rosa.CentroBBox;
    public Vector2 CentroVerde => _verde.CentroBBox;
    
    public Point CentroRosaCV => _rosa.CentroBBoxCV;
    public Point CentroVerdeCV => _verde.CentroBBoxCV;

    public Vector2 Centro => (CentroRosa + CentroVerde) /2f;
    public Point CentroCV => (CentroRosaCV + CentroVerdeCV) /2f;

    public Vector2 _direccion;
    public float _angulo;
    
    public PastillaBicolor(GrafoDeContornos.Contorno verde, GrafoDeContornos.Contorno rosa, float distancia) {
        _verde = verde;
        _rosa = rosa;
        _distancia = distancia;

        _direccion = (CentroRosa-CentroVerde).normalized;
        _angulo = Vector2.SignedAngle( _direccion , Vector2.right );
    }
}
