using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Webber : MonoBehaviour
{
    RawImage _image;
    RawImage Image => _image ? _image : _image = GetComponent<RawImage>();
    // Start is called before the first frame update
    void Start()
    {
        TestWebington.UsarRuta("recibir imagen", "buscarforma", (ctx, parser) =>
        {
            Debug.Log("se disparo la accion");
            var textura = new Texture2D(8, 8);
            textura.LoadImage(parser.FileContents);

            if (Image)
            {
                if (Image.texture)
                    Destroy(Image.texture);

                Image.texture = (Texture)textura;
            }
            TestWebington.ResponderString(ctx.Response, "imagen subida", true);

            Debug.Log("se proceso la accion");

            //var win = CapturadorSprites.AbrirCon(textura, new GUIContent($"Imagen Recibida {System.DateTime.Now}", textura));
        });
    }

}
