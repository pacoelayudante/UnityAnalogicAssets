using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BlobNave : BlobGenerico
{
    List<PastillaBicolor> _pastillas = new List<PastillaBicolor>();

    public int Nivel => _pastillas.Count;

    public OpenCvSharp.Point2f DireccionCV => new OpenCvSharp.Point2f(_direccion.x, _direccion.y);

    public OpenCvSharp.RotatedRect Ellipse => _contorno.EllipseCV;

    public Vector2 CentroBBox => _contorno.CentroBBox;
    public OpenCvSharp.Point CentroBBoxCV => _contorno.CentroBBoxCV;

    public Vector2 _direccion;
    public float _angulo;

    // babor angulo position
    public List<Vector2> _salidasBabor = new List<Vector2>();
    // estribor angulo negativo
    public List<Vector2> _salidasEstribor = new List<Vector2>();

    public List<BlobSalidas> _blobsSalidas = new List<BlobSalidas>();

    public BlobNave(GrafoDeContornos.Contorno contorno, List<PastillaBicolor> todasLasPastillas):base(contorno)
    {
        _direccion = Vector2.zero;
        foreach (var pastilla in todasLasPastillas)
        {
            if (_contorno.PointInside(pastilla.CentroRosa)
            || _contorno.PointInside(pastilla.CentroVerde)
            || _contorno.PointInside(pastilla.Centro)) {
                _pastillas.Add(pastilla);
                _direccion += pastilla._direccion;
            }
        }

        _direccion.Normalize();
        _angulo = Vector2.SignedAngle( _direccion , Vector2.right );
    }

    public void Add(BlobSalidas nuevasSalidas) {
        _blobsSalidas.Add(nuevasSalidas);

        foreach(var pt in nuevasSalidas._salidasEstimados) {
            var angulo = Vector2.SignedAngle(pt-CentroBBox, _direccion);
            (angulo > 0 ? _salidasBabor : _salidasEstribor).Add(pt);
        }

        _salidasBabor.Sort( (vecA,vecB)=>{
            var anguloA = Vector2.SignedAngle(vecA-CentroBBox, _direccion);
            var anguloB = Vector2.SignedAngle(vecB-CentroBBox, _direccion);
            return anguloA.CompareTo(anguloB);
        } );
        
        _salidasEstribor.Sort( (vecA,vecB)=>{
            var anguloA = Vector2.SignedAngle(vecA-CentroBBox, _direccion);
            var anguloB = Vector2.SignedAngle(vecB-CentroBBox, _direccion);
            return anguloB.CompareTo(anguloA);
        } );
    }
}
