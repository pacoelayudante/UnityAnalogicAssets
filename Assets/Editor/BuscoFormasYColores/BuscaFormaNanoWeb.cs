using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Guazu.NanoWeb;

public static class BuscaFormaNanoWeb
{
    [InitializeOnLoadMethod]
    static void Init()
    {
        NanoWebEditorWindow.UsarRuta("recibir imagen para buscar forma", "buscarforma", (ctx, parser) =>
        {
            var textura = new Texture2D(8, 8);
            textura.LoadImage(parser.FileContents);
            NanoWebEditorWindow.ResponderString(ctx.Response, "imagen subida", true);

            var win = VerTexturaSola.Mostrar(textura, true, true);
        });
    }
}
