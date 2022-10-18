using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;

public class PastillaBicolor
{
    public GrafoDeContornos.Contorno _templado,_calido;
    public float _distancia;

    public Vector2 CentroCalido => _calido.CentroBBox;
    public Vector2 CentroTemplado => _templado.CentroBBox;
    
    public Point CentroCalidoCV => _calido.CentroBBoxCV;
    public Point CentroTempladoCV => _templado.CentroBBoxCV;

    public Vector2 Centro => (CentroCalido + CentroTemplado) /2f;
    public Point CentroCV => (CentroCalidoCV + CentroTempladoCV) /2f;

    public Vector2 _direccion;
    public float _angulo;
    
    public PastillaBicolor(GrafoDeContornos.Contorno templado, GrafoDeContornos.Contorno calido, float distancia) {
        _templado = templado;
        _calido = calido;
        _distancia = distancia;

        _direccion = (CentroCalido-CentroTemplado).normalized;
        _angulo = Vector2.SignedAngle( _direccion , Vector2.right );
    }
}
