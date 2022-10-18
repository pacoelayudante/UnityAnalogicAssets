using System.Collections;
using System.Collections.Generic;
using OpenCvSharp;
using UnityEngine;

public class GrafoColibriRosa
{
    public class Config {

    }

    public enum Color {
        Negro, Rosa, Verde
    }
    public const Color CALIDO = Color.Rosa;
    public const Color TEMPLADO = Color.Verde;

    public class ContornoColibri
    {
        GrafoDeContornos.Contorno _contorno;
        Color _color = Color.Negro;

        public static implicit operator GrafoDeContornos.Contorno(ContornoColibri c) => c._contorno;
    }

    public List<PastillaBicolor> _pastillasBicolor = new List<PastillaBicolor>();

    public List<GrafoDeContornos.Contorno> _contornosPrimerNivel = new List<GrafoDeContornos.Contorno>();

    public Dictionary<Color, List<GrafoDeContornos.Contorno>> _contornosPrimerNivelPorColor = new Dictionary<Color, List<GrafoDeContornos.Contorno>>(){
         {Color.Verde, new List<GrafoDeContornos.Contorno>()},
         {Color.Rosa, new List<GrafoDeContornos.Contorno>()}
    };

    public GrafoColibriRosa(GrafoDeContornos grafVerde, GrafoDeContornos grafRosa, Mat matRosa, Mat matVerde, Config config) {
        foreach (var verde in grafVerde._primerNivel) {
         
            foreach (var cont in verde._contenidos) {

                foreach (var rosa in grafRosa._primerNivel) {

                    

                }

            }

        }
    }
}
