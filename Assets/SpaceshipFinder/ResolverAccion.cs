using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rect = UnityEngine.Rect;
using CvRect = OpenCvSharp.Rect;
using OpenCvSharp;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu]
public class ResolverAccion : ScriptableObject
{
    public TokenDetector _tokenDetector;

    public TokenTemplates TokenTemplates => _tokenDetector._tokenTemplates;

    public class TextureDisposer
    {
        public Texture2D textura;

        ~TextureDisposer()
        {
            if (textura)
                DestroyImmediate(textura);
        }
    }

    private TextureDisposer texturaGenerada;

    public class Escudo
    {
        public Point2f centro;
        public float radio;

        public bool activo = true;
        public float fase = Random.value * Mathf.PI;
        public float porcentaje;

        public Escudo(Point2f centro, float radio, float porcentaje)
        {
            this.centro = centro;
            this.radio = radio;
            this.porcentaje = porcentaje;
        }
    }

    public void Resolver(Texture2D input)
    {
        using Mat inputMat = OpenCvSharp.Unity.TextureToMat(input);
        Cv2.CvtColor(inputMat, inputMat, _tokenDetector._tipoHue == TipoHue.HSV ? ColorConversionCodes.BGR2HSV : ColorConversionCodes.BGR2HLS);
        _tokenDetector.Detectar(inputMat, _tokenDetector._tipoHue, out TokenDetector.Resultados resultados);

        var disparoRealizado = new List<TokenDetector.TokenEncontrado>();

        Dictionary<TokenDetector.TokenEncontrado, TokenDetector.TokenDisparador> _disparos = new();

        foreach (var disparador in resultados.tokensDisparadores)
        {
            var tokenCercano = AsignarDisparadores(disparador, resultados, disparoRealizado);
            if (tokenCercano != null)
            {
                disparoRealizado.Add(tokenCercano);
                _disparos[tokenCercano] = disparador;
            }
        }

        Dictionary<int, List<TokenDetector.TokenEncontrado>> tokensPorOrdenDisparo = new();

        List<Escudo> escudosEnJuego = new();
        Dictionary<TokenDetector.TokenEncontrado, List<Escudo>> escudosPorNave = new();

        foreach (var token in resultados.todosLosTokens)
        {
            if (token.TemplateMasPosible == null)
                continue;

            escudosPorNave.TryAdd(token, new());
            foreach (var vecEscudo in token.TemplateMasPosible.escudos)
            {
                var nuevoEscudo = new Escudo(token.centroCirculo, token.radioCirculo * vecEscudo.y, vecEscudo.x);
                escudosEnJuego.Add(nuevoEscudo);
                escudosPorNave[token].Add(nuevoEscudo);
            }

            if (!tokensPorOrdenDisparo.ContainsKey(token.OrdenDeDisparo))
                tokensPorOrdenDisparo.TryAdd(token.OrdenDeDisparo, new());

            tokensPorOrdenDisparo[token.OrdenDeDisparo].Add(token);
        }

        // preparar escudos

        // rayos vs escudos +
        // marcar hits (y origen de hit)
        var numerosDeOrdenDeDisparo = new List<int>(tokensPorOrdenDisparo.Keys);
        numerosDeOrdenDeDisparo.Sort();
        foreach (var numeroDeOrden in numerosDeOrdenDeDisparo)
        {
            var tokensDisparando = tokensPorOrdenDisparo[numeroDeOrden];
            while (tokensDisparando.Count > 0)
            {
                var sel = tokensDisparando[Random.Range(0, tokensDisparando.Count)];
                tokensDisparando.Remove(sel);


            }
        }
    }

    private TokenDetector.TokenEncontrado AsignarDisparadores(TokenDetector.TokenDisparador disparador, TokenDetector.Resultados resultados, List<TokenDetector.TokenEncontrado> disparoRealizado)
    {
        TokenDetector.TokenEncontrado resultado = null;

        var minDist = double.MaxValue;
        var disparadorCentro = disparador.localMaximas[disparador.indiceCentral];
        foreach (var posibleFichaActiva in resultados.todosLosTokens)
        {
            if (posibleFichaActiva.puntosArmas.Count == 0 || disparoRealizado.Contains(posibleFichaActiva))
                continue;

            var dist = disparadorCentro.DistanceTo(posibleFichaActiva.puntosArmas[posibleFichaActiva.indiceArmaCentral]);
            if (dist < minDist)
            {
                minDist = dist;
                resultado = posibleFichaActiva;
            }
        }

        return resultado;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ResolverAccion))]
    public class ResolverAccionEditor : Editor
    {
        RenderTexture _renderTexture;
        private Texture2D _texturaInput;
        Texture2D _testResultado;

        private Material material;
        ResolverAccion resolverAccion;

        Vector2 scroll;

        public void OnEnable()
        {
            resolverAccion = (ResolverAccion)target;

            if (material == null)
                // Find the "Hidden/Internal-Colored" shader, and cache it for use.
                material = new Material(Shader.Find("Hidden/Internal-Colored"));
        }

        void OnDisable()
        {
            if (_texturaInput != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_texturaInput)))
                DestroyImmediate(_texturaInput);
            if (_testResultado != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_testResultado)))
                DestroyImmediate(_testResultado);
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                _renderTexture = EditorGUILayout.ObjectField("Render Texture", _renderTexture, typeof(RenderTexture), allowSceneObjects: false) as RenderTexture;
                if (changed.changed && _renderTexture)
                {
                    if (_texturaInput != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_texturaInput)))
                        DestroyImmediate(_texturaInput);

                    _texturaInput = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGBA32, false, true);
                    RenderTexture.active = _renderTexture;
                    _texturaInput.ReadPixels(new Rect(0, 0, _texturaInput.width, _texturaInput.height), 0, 0, false);
                    _texturaInput.Apply(false);
                }
            }
        }
    }
#endif
}
