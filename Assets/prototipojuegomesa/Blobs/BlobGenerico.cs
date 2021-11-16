using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobGenerico 
{
    protected GrafoDeContornos.Contorno _contorno;

    public BlobGenerico(GrafoDeContornos.Contorno contorno) {
        _contorno = contorno;
    }
}
