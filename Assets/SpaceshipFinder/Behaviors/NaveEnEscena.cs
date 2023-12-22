using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rect = UnityEngine.Rect;
using CvRect = OpenCvSharp.Rect;
using OpenCvSharp;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NaveEnEscena : MonoBehaviour
{
    public class StaticMat
    {
        private Material _m;
        public Material Material
        {
            get
            {
                if (_m == null)
                {
                    _m = new Material(Shader.Find("Hidden/Internal-Colored"));
                    _m.hideFlags = HideFlags.DontSave;
                }
                return _m;
            }
        }

        ~StaticMat()
        {
            if (_m)
                DestroyImmediate(_m);
        }

        public static implicit operator Material(StaticMat stMat) => stMat == null ? null : stMat.Material;
    }

    private static StaticMat MatDefault = new StaticMat();
    private static RaycastHit2D[] Hits = new RaycastHit2D[3];

    private static readonly Color[] colors = new Color[] { Color.green, Color.blue };

    public int equipo;
    public Vector2[] armas;
    public int armaCentral;

    public bool DisparoPreparado = false;

    private List<Ray2D> rayos = new();
    private Collider2D _colliders;

    private Vector2[] apuntadores = new Vector2[0];

    public void Inicializar(TokenDetector.TokenEncontrado encontrado)
    {
        if (encontrado.areaRect <= 4)
        {
            Debug.Log($"area cuatro o menos {this}", this);
            return;
        }

        var meshcol = gameObject.AddComponent<PolygonCollider2D>();
        var path = new Vector2[encontrado.contorno.Length];

        equipo = encontrado.equipo;

        for (int i = 0; i < path.Length; i++)
        {
            var p = encontrado.contorno[i];
            path[i] = new Vector2(p.X, p.Y);
        }
        meshcol.SetPath(0, path);

        _colliders = meshcol;

        gameObject.AddComponent<MeshFilter>().sharedMesh = meshcol.CreateMesh(useBodyPosition: false, useBodyRotation: false);
        var mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = MatDefault;

        MaterialPropertyBlock materialProperty = new MaterialPropertyBlock();
        materialProperty.SetColor("_Color", colors[equipo]);
        mr.SetPropertyBlock(materialProperty);

        armas = new Vector2[encontrado.puntosArmas.Count];
        for (int i = 0; i < armas.Length; i++)
        {
            var p = encontrado.puntosArmas[i];
            armas[i] = new Vector2((float)p.X, (float)p.Y);
        }

        armaCentral = encontrado.indiceArmaCentral;

        // if (armas.Length == 0)
        // {
        //     DisparoPreparado = true;
        //     var centroide = new Vector2((float)encontrado.centroideContorno.X, (float)encontrado.centroideContorno.Y);
        //     var centroideHull = new Vector2((float)encontrado.centroideHull.X, (float)encontrado.centroideHull.Y);

        //     rayos.Add(new Ray2D(centroide, centroideHull - centroide));
        // }
    }

    public void ApuntarDisparo(TokenDetector.TokenDisparador disparo)
    {
        DisparoPreparado = true;

        apuntadores = new Vector2[disparo.localMaximas.Length];
        for (int i = 0; i < apuntadores.Length; i++)
        {
            var p = disparo.localMaximas[i];
            apuntadores[i] = new Vector2((float)p.X, (float)p.Y);
        }
        var apuntadorCentral = apuntadores[disparo.indiceCentral];

        rayos.Add(new Ray2D(armas[armaCentral], armas[armaCentral] - apuntadorCentral));

        if (armas.Length > 1)
        {

        }
    }

    void OnDrawGizmosSelected()
    {
        if (apuntadores == null)
            return;

        foreach (var p in apuntadores)
        {
            Gizmos.DrawSphere(p, 2f);
            Gizmos.DrawLine(p, armas[armaCentral]);
        }
    }

    public void CalcularRayos()
    {
        if (!DisparoPreparado)
            return;

        _colliders.enabled = false;

        foreach (var rayo in rayos)
        {
            var hitCant = Physics2D.RaycastNonAlloc(rayo.origin, rayo.direction, Hits);

            var hitDist = hitCant > 0 ? Hits[0].distance : 400f;

            var dibujoRayo = new GameObject("Rayo Mini");
            dibujoRayo.hideFlags = HideFlags.DontSave;
            dibujoRayo.transform.SetParent(transform);
            var line = dibujoRayo.AddComponent<LineRenderer>();
            line.SetPosition(0, rayo.origin);
            line.SetPosition(1, rayo.GetPoint(hitDist));
            line.sharedMaterial = MatDefault;
            line.startColor = line.endColor = Color.red;
        }

        _colliders.enabled = true;
    }
}
