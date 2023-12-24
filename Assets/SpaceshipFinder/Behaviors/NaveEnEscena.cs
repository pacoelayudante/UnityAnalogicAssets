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
    public int indiceArmaCentral;

    public float distLaserMiss = 800f;

    public bool DisparoPreparado = false;

    private List<Ray2D> rayos = new();
    private Collider2D _colliders;

    private Vector2[] apuntadores = new Vector2[0];
    public int indiceApuntadoreCentral;

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

        indiceArmaCentral = encontrado.indiceArmaCentral;

        if (armas.Length == 0)
        {
            DisparoPreparado = true;
            var centroide = new Vector2((float)encontrado.centroideContorno.X, (float)encontrado.centroideContorno.Y);
            var centroideHull = new Vector2((float)encontrado.centroideHull.X, (float)encontrado.centroideHull.Y);

            rayos.Add(new Ray2D(centroide, centroideHull - centroide));
        }
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
        var apuntadorCentral = apuntadores[indiceApuntadoreCentral = disparo.indiceCentral];

        rayos.Add(new Ray2D(armas[indiceArmaCentral], armas[indiceArmaCentral] - apuntadorCentral));

        if (armas.Length > 1)
        {
            var armasLadoIzq = new List<Vector2>();
            var armasLadoDer = new List<Vector2>();

            var armaCentral = armas[indiceArmaCentral];
            foreach (var arma in armas)
            {
                if (arma == armaCentral)
                    continue;

                bool alIzq = (apuntadorCentral.x - armaCentral.x) * (arma.y - armaCentral.y) - (apuntadorCentral.y - armaCentral.y) * (arma.x - armaCentral.x) > 0;
                if (alIzq)
                    armasLadoIzq.Add(arma);
                else
                    armasLadoDer.Add(arma);
            }

            armasLadoIzq.Sort(CompararDistanciasConArmaCentral);
            armasLadoDer.Sort(CompararDistanciasConArmaCentral);

            var apuntadoresLadoIzq = new List<Vector2>();
            var apuntadoresLadoDer = new List<Vector2>();

            foreach (var apuntador in apuntadores)
            {
                if (apuntador == apuntadorCentral)
                    continue;

                bool alIzq = (apuntadorCentral.x - armaCentral.x) * (apuntador.y - armaCentral.y) - (apuntadorCentral.y - armaCentral.y) * (apuntador.x - armaCentral.x) > 0;
                if (alIzq)
                    apuntadoresLadoIzq.Add(apuntador);
                else
                    apuntadoresLadoDer.Add(apuntador);
            }

            apuntadoresLadoIzq.Sort(CompararDistanciasConApuntadorCentral);
            apuntadoresLadoDer.Sort(CompararDistanciasConApuntadorCentral);

            int loopCount = Mathf.Max(armasLadoIzq.Count, armasLadoDer.Count, apuntadoresLadoIzq.Count, apuntadoresLadoDer.Count);
            for (int i = 0; i < loopCount; i++)
            {
                if (i < armasLadoIzq.Count && i < apuntadoresLadoIzq.Count)
                    rayos.Add(new Ray2D(armasLadoIzq[i], armasLadoIzq[i] - apuntadoresLadoIzq[i]));
                if (i < armasLadoDer.Count && i < apuntadoresLadoDer.Count)
                    rayos.Add(new Ray2D(armasLadoDer[i], armasLadoDer[i] - apuntadoresLadoDer[i]));
            }
        }
    }

    int CompararDistanciasConArmaCentral(Vector2 a, Vector2 b)
        => (a - armas[indiceArmaCentral]).sqrMagnitude.CompareTo((b - armas[indiceArmaCentral]).sqrMagnitude);
    int CompararDistanciasConApuntadorCentral(Vector2 a, Vector2 b)
        => (a - apuntadores[indiceApuntadoreCentral]).sqrMagnitude.CompareTo((b - apuntadores[indiceApuntadoreCentral]).sqrMagnitude);

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        if (apuntadores != null)
        {
            foreach (var p in apuntadores)
            {
                Gizmos.DrawSphere(p, 2f);
            }
        }

        if (armas != null)
        {
            foreach (var p in armas)
            {
                Gizmos.DrawCube(p, Vector2.one * 2f);
            }
        }

        if (rayos != null)
        {
            foreach (var r in rayos)
            {
                Gizmos.DrawLine(r.origin, r.GetPoint(-120f));


                Gizmos.color = Color.magenta;
                var hitCant = Physics2D.RaycastNonAlloc(r.origin, r.direction, Hits);
                for (int i = 0; i < hitCant; i++)
                {
                    var hit = Hits[i];
                    
                    Gizmos.DrawLine(r.origin, r.GetPoint(hit.distance));
                    Gizmos.DrawSphere(hit.point, 2f);

                }
                Gizmos.color = Color.white;
            }
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

            var hitDist = hitCant > 0 ? Hits[0].distance : distLaserMiss;

            var dibujoRayo = new GameObject("Rayo Mini");
            dibujoRayo.hideFlags = HideFlags.DontSave;
            dibujoRayo.transform.SetParent(transform, worldPositionStays: false);
            var line = dibujoRayo.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.SetPosition(0, rayo.origin);
            line.SetPosition(1, rayo.GetPoint(hitDist));
            line.sharedMaterial = MatDefault;
            line.startColor = line.endColor = hitCant == 0 ? Color.black : Color.red;
        }

        _colliders.enabled = true;
    }
}
