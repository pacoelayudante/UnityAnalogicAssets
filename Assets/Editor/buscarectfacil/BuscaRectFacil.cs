using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenCvSharp;
using UnityEngine.Networking;
using System.Linq;

public class BuscaRectFacil : EditorWindow
{
    [MenuItem("Lab/Buscar Rect Facil")]
    static void Abrir() => GetWindow<BuscaRectFacil>();

    public Texture2D texturaDescargada;
    public string UrlDescarga {
        get=>EditorPrefs.GetString("BuscaRectFacil.UrlDescarga","");
        set=>EditorPrefs.SetString("BuscaRectFacil.UrlDescarga",value);
    }
    public bool urlDescargaValida = false;
    public UnityWebRequest descargaActual;

    public float SizeLimit {
        get => EditorPrefs.GetFloat("BuscaRectFacil.SizeLimit",480);
        set => EditorPrefs.SetFloat("BuscaRectFacil.SizeLimit",value);
    }
    public float CannyBajo {
        get => EditorPrefs.GetFloat("BuscaRectFacil.CannyBajo",100);
        set => EditorPrefs.SetFloat("BuscaRectFacil.CannyBajo",value);
    }
    public float CannyAlto {
        get => EditorPrefs.GetFloat("BuscaRectFacil.CannyAlto",200);
        set => EditorPrefs.SetFloat("BuscaRectFacil.CannyAlto",value);
    }
    public int ApertureCanny {
        get => EditorPrefs.GetInt("BuscaRectFacil.ApertureCanny",3);
        set => EditorPrefs.SetInt("BuscaRectFacil.ApertureCanny",value);
    }
    public bool CannyL2 {
        get => EditorPrefs.GetBool("BuscaRectFacil.CannyL2",false);
        set => EditorPrefs.SetBool("BuscaRectFacil.CannyL2",value);
    }
    public bool CannyDilate {
        get => EditorPrefs.GetBool("BuscaRectFacil.CannyDilate",false);
        set => EditorPrefs.SetBool("BuscaRectFacil.CannyDilate",value);
    }
    public float ApproxPolyEpsilon {
        get => EditorPrefs.GetFloat("BuscaRectFacil.ApproxPolyEpsilon",0.5f);
        set => EditorPrefs.SetFloat("BuscaRectFacil.ApproxPolyEpsilon",value);
    }

    public Vector2 posFloodFill;

    public VerTexturaSola verCanny,verFill,verTransformada;

    void OnGUI()
    {
        EditorGUI.BeginDisabledGroup(descargaActual != null && !descargaActual.isDone);
        UrlDescarga = EditorGUILayout.TextField(UrlDescarga);
        if (GUILayout.Button("Descargar")) DescargarTextura(UrlDescarga);
        EditorGUI.EndDisabledGroup();
        if (texturaDescargada)
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(100), GUILayout.Width(100 * texturaDescargada.width / (float)texturaDescargada.height));
            var mouse = Event.current;
            EditorGUI.DrawPreviewTexture(rect, texturaDescargada);
            if (mouse.type == EventType.MouseDown && rect.Contains(mouse.mousePosition)) {
                posFloodFill = (mouse.mousePosition-rect.position) / rect.size;
                this.Repaint();
            }
            EditorGUI.DrawRect(new UnityEngine.Rect(posFloodFill*rect.size+rect.position,Vector2.one*5),Color.red);

            SizeLimit = EditorGUILayout.IntSlider("sizeLimit",Mathf.FloorToInt( SizeLimit),32,2048);
            CannyBajo = EditorGUILayout.Slider("cannyBajo", CannyBajo, 0f, 255f);
            CannyAlto = EditorGUILayout.Slider("cannyAlto", CannyAlto, 0f, 255f);
            ApertureCanny = EditorGUILayout.IntSlider("apertureCanny", ApertureCanny, 3, 21);
            if (ApertureCanny % 2 == 0) ApertureCanny += 1;
            CannyL2 = EditorGUILayout.Toggle("cannyL2", CannyL2);
            CannyDilate = EditorGUILayout.Toggle("cannyDilate", CannyDilate);
            ApproxPolyEpsilon = EditorGUILayout.Slider("approxPolyEpsilon", ApproxPolyEpsilon, 0f, 1f);


            if (GUILayout.Button("Procesar")) {
                Procesar(CannyBajo,CannyAlto,ApertureCanny,CannyL2);
            }
        }
    }
    void OnEnable()
    {
        if (urlDescargaValida && !texturaDescargada)
        {
            //DescargarTextura(urlDescarga);
        }
    }

    void OnDestroy()
    {
        if (texturaDescargada) DestroyImmediate(texturaDescargada);
    }

    void DescargarTextura(string url)
    {
        if (descargaActual != null && !descargaActual.isDone) Debug.LogWarning("Ya habia una descarga en progreso pero bueno");
        var request = descargaActual = UnityWebRequestTexture.GetTexture(UrlDescarga);
        request.SendWebRequest().completed += (resultado) =>
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                urlDescargaValida = true;
                if (texturaDescargada) DestroyImmediate(texturaDescargada);
                texturaDescargada = DownloadHandlerTexture.GetContent(request);
            }
            else
            {
                urlDescargaValida = false;
            }
        };
    }

    void Procesar(float cannythreshBajo = 100f, float cannythreshAlto = 200f, int apertureSize = 3, bool L2gradient = false)
    {
        if (texturaDescargada)
        {
            var matDescargadoOriginal = OpenCvSharp.Unity.TextureToMat(texturaDescargada);
            var matDescargadoAProcesar = new Mat();
            var escalaReduccionImagen = 1f;
            if (texturaDescargada.width>SizeLimit || texturaDescargada.height>SizeLimit) {
                escalaReduccionImagen = Mathf.Min(SizeLimit/texturaDescargada.width,SizeLimit/texturaDescargada.height);
                Cv2.Resize( matDescargadoOriginal,matDescargadoAProcesar,new Size(texturaDescargada.width*escalaReduccionImagen,texturaDescargada.height*escalaReduccionImagen));
            }
            else matDescargadoAProcesar = matDescargadoOriginal.Clone();
            var pos = new Point(posFloodFill.x*matDescargadoAProcesar.Width,posFloodFill.y*matDescargadoAProcesar.Height);
            var matCanny = matDescargadoAProcesar.Canny(cannythreshBajo, cannythreshAlto, apertureSize, L2gradient);
            matCanny.Circle(pos,9,new Scalar(0),-1);
            var matMask = new Mat(matCanny.Rows+2,matCanny.Cols+2,matCanny.Type(),new Scalar(0));
            if (CannyDilate) Cv2.Dilate(matCanny,matCanny,new Mat());
            if (verCanny) verCanny.Textura = OpenCvSharp.Unity.MatToTexture(matCanny);
            else verCanny = VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(matCanny),true,true);
            matCanny.FloodFill(matMask, pos, new Scalar(255) );
            matMask.Rectangle(new OpenCvSharp.Rect(0,0,matMask.Width,matMask.Height),new Scalar(0));
            matMask*=255;
            Mat[] contorno;
            Mat hierarchyIndex = new Mat();
            Cv2.FindContours(matMask,out contorno,hierarchyIndex,RetrievalModes.External,ContourApproximationModes.ApproxNone);
            Cv2.ConvexHull(contorno[0],contorno[0]);
            contorno[0] /= escalaReduccionImagen;
            //contorno[0] = Cv2.ApproxPolyDP(contorno[0],hullLength*ApproxPolyEpsilon,true);
            if (contorno[0].Rows > 4) {
                var hullLength = Cv2.ArcLength(contorno[0],true);
                var approxPolyStep = hullLength*0.001d;
                for (double epsilon=approxPolyStep; epsilon<hullLength; epsilon+=approxPolyStep) {
                    Cv2.ApproxPolyDP(contorno[0],hierarchyIndex,epsilon,true);
                    if (hierarchyIndex.Rows == 4) {
                        contorno[0] = hierarchyIndex;
                        ApproxPolyEpsilon = (float) (epsilon/hullLength);
                        break;
                    }
                    else if (hierarchyIndex.Rows < 4) {
                        Debug.LogError($"Me pase al reducir poligonos? epsilon {epsilon} nuevoPolyLen {hierarchyIndex.Rows}");
                    }
                }
            }

            /*var posx = new Mat();
            var posy = new Mat();
            Cv2.ExtractChannel(contorno[0],posx,0);
            Cv2.ExtractChannel(contorno[0],posy,1);
            Cv2.Add(posx,posy,posx);
            var sortedSum = new Mat();
            var sortedDiff = new Mat();
            Cv2.SortIdx(posx,sortedSum,SortFlags.Ascending|SortFlags.EveryColumn);
            Cv2.Subtract(posx,posy,posx);
            Cv2.Subtract(posy,posx,posx);
            Cv2.SortIdx(posy,sortedDiff,SortFlags.Ascending|SortFlags.EveryColumn);
            
            var contornoPts = new[]{contorno[0].At<Point>(sortedSum.At<int>(0)),contorno[0].At<Point>(sortedDiff.At<int>(0)),contorno[0].At<Point>(sortedSum.At<int>(3)),contorno[0].At<Point>(sortedDiff.At<int>(3))};
            contorno[0].Set(0,sortedSum.At<Point>(0));
            contorno[0].Set(1,sortedDiff.At<Point>(0));
            contorno[0].Set(2,sortedSum.At<Point>(3));
            contorno[0].Set(3,sortedDiff.At<Point>(3));
            */
            var contornoPts = new[]{contorno[0].At<Point>(0),contorno[0].At<Point>(1),contorno[0].At<Point>(2),contorno[0].At<Point>(3)};

            var sum = contornoPts.OrderBy(p=>p.X+p.Y);
            var diff = contornoPts.OrderBy(p=>(p.Y-p.X));

            contornoPts = new[]{sum.FirstOrDefault(),diff.FirstOrDefault(),sum.LastOrDefault(),diff.LastOrDefault()};
            
            //top left, top right, bottom right, bottom left
            var (tl,tr,br,bl) = (contornoPts[0],contornoPts[1],contornoPts[2],contornoPts[3]);//okey... si esto anda...
            var maxWidth = System.Math.Floor(System.Math.Max( tl.DistanceTo(tr),bl.DistanceTo(br)));
            var maxHeight = System.Math.Floor(System.Math.Max( bl.DistanceTo(tl),br.DistanceTo(tr)));
            var rectDestino = new[]{new Point(0,0),new Point(maxWidth-1,0),new Point(maxWidth-1,maxHeight-1),new Point(0,maxHeight-1)};
            var matrizTransform = Cv2.GetPerspectiveTransform(contornoPts.Select(p=>(Point2f)p),rectDestino.Select(p=>(Point2f)p));
            Mat matAcomodado = new Mat();
             Cv2.WarpPerspective(matDescargadoOriginal,matAcomodado,matrizTransform,new Size((int)maxWidth,(int)maxHeight));
             
            if (verTransformada) verTransformada.Textura = OpenCvSharp.Unity.MatToTexture(matAcomodado);
            else verTransformada = VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(matAcomodado),true,true);

            Cv2.CvtColor(matMask,matMask,ColorConversionCodes.GRAY2BGR);
            //matMask.DrawContours(contorno.Select(c=>c.Select(p=>p*escalaReduccionImagen)),-1,new Scalar(100,50,255),2);
            for (int i=0; i<contornoPts.Length; i++) {
                matMask.Circle(contornoPts[i]*escalaReduccionImagen,6,new Scalar(50,250,100),-1);
                for(int j=0; j<=i; j++) matMask.Circle(contornoPts[i]*escalaReduccionImagen+new Point(6*j+9,i*3),3,new Scalar(50,250,100),-1);
            }

            if (verFill) verFill.Textura = OpenCvSharp.Unity.MatToTexture(matMask);
            else verFill = VerTexturaSola.Mostrar(OpenCvSharp.Unity.MatToTexture(matMask),true,true);

            matMask.Dispose();
            matCanny.Dispose();
            matDescargadoOriginal.Dispose();
            matDescargadoAProcesar.Dispose();
            matAcomodado.Dispose();
        }
    }
}
