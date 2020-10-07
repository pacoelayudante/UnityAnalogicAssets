using UnityEngine;
using UnityEditor;
using Guazu.NanoWeb;

public static class NanoWebParaCapturador {
    [InitializeOnLoadMethod]
    static void Init()
    {
        NanoWebEditorWindow.UsarRuta("recibir imagen", "subir", (ctx, parser) =>
        {
            var textura = new Texture2D(8, 8);
            textura.LoadImage(parser.FileContents);
            NanoWebEditorWindow.ResponderString(ctx.Response, "imagen subida", true);

            var win = CapturadorSprites.AbrirCon(textura, new GUIContent($"Imagen Recibida {System.DateTime.Now}", textura));
        });
    }
}