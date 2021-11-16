using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using System.Net;
using System.Threading;
using System.IO;
using System;
using System.Text;
using System.Text.RegularExpressions;

public class TestWebington : MonoBehaviour
{
    const string CONTENT_TYPE_MULTIPART = "multipart/form-data";

    public int PuertoDeServidor
    {
        get => PlayerPrefs.GetInt($"{name}.UsarPuerto", 8080);
        set => PlayerPrefs.SetInt($"{name}.UsarPuerto", value);
    }
    public string CarpetaWWW
    {
        get => PlayerPrefs.GetString($"{name}.CarpetaWWW", "www");
        set => PlayerPrefs.SetString($"{name}.CarpetaWWW", value);
    }

    static Dictionary<string, System.Action<HttpListenerContext, MultipartParser>> rutasYEfectos = new Dictionary<string, System.Action<HttpListenerContext, MultipartParser>>();
    public static void UsarRuta(string proceso, string ruta, System.Action<HttpListenerContext, MultipartParser> efecto)
    {
        rutasYEfectos.Add(proceso, efecto);
        PlayerPrefs.SetString($"{proceso}", CurarRuta(ruta));
    }
    static string GetRutaDeProcesoDefault(string proceso) => PlayerPrefs.GetString($"{proceso}", $"{proceso}");
    string GetRutaDeProceso(string proceso) => PlayerPrefs.GetString($"{name}.{proceso}", GetRutaDeProcesoDefault(proceso));
    void SetRutaDeProceso(string proceso, string nuevoValor)
    {
        PlayerPrefs.SetString($"{name}.{proceso}", CurarRuta(nuevoValor));
    }
    static string CurarRuta(string ruta)
    {
        if (ruta == null || ruta.Length == 0) ruta = "/";
        else
        {
            if (ruta.First() != '/') ruta = "/" + ruta;
            if (ruta.Last() == '/') ruta = ruta.Substring(0, ruta.Length - 1);
        }
        return ruta;
    }

    public bool HayRed => System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
    public static string DireccionIPLocal => Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();

    readonly object _servidorConectadoLock = new object();
    Thread _servidorConectado = null;
    Thread ServidorActual
    {
        get
        {
            Thread valor = null;
            lock (_servidorConectadoLock) valor = _servidorConectado;
            return valor;
        }
        set
        {
            lock (_servidorConectadoLock) _servidorConectado = value;
        }
    }
    public bool ServidorActualActivo
    {
        get
        {
            var _serv = ServidorActual;
            return _serv != null && _serv.IsAlive;
        }
    }
    void ActivarServidorActual()
    {
        if (ServidorActualActivo) return;
        var puerto = PuertoDeServidor;
        var carpetaWWW = Path.Combine(Application.dataPath, CarpetaWWW);
        Thread nuevoServidor = new Thread((yoMismo) => ProgramaDelServidor((Thread)yoMismo, puerto));
        nuevoServidor.Start(nuevoServidor);
        ServidorActual = nuevoServidor;
    }
    void DesactivarServidorActual()
    {
        if (!ServidorActualActivo) return;
        ServidorActual = null;
    }

    Vector2 logScroll;
    List<string> _unitySafeLog = new List<string>();
    readonly object _bridgeLogLock = new object();
    List<string> _bridgeLog = new List<string>();
    List<string> GetUpdatedSafeLog()
    {
        lock (_bridgeLogLock)
        {
            _unitySafeLog.AddRange(_bridgeLog.Skip(_unitySafeLog.Count));
        }
        return _unitySafeLog;
    }
    void ThreadSafeLog(string msj, bool debugTambien = false)
    {
        lock (_bridgeLogLock)
        {
            _bridgeLog.Add(msj);
        }
        if (debugTambien) Debug.Log(msj);
    }

    readonly object _colaDePedidosLock = new object();
    List<HttpListenerContext> _colaDePedidos = new List<HttpListenerContext>();
    Dictionary<HttpListenerContext, MultipartParser> _cargaDePedidos = new Dictionary<HttpListenerContext, MultipartParser>();
    void AgregarPedido(HttpListenerContext ctx, MultipartParser mem)
    {
        lock (_colaDePedidosLock)
        {
            _colaDePedidos.Add(ctx);
            if (mem != null) _cargaDePedidos.Add(ctx, mem);
        }
    }
    HttpListenerContext TomarPedido()
    {
        HttpListenerContext ctx = null;
        lock (_colaDePedidosLock)
        {
            if (_colaDePedidos.Count > 0)
            {
                ctx = _colaDePedidos[0];
                _colaDePedidos.RemoveAt(0);
            }
        }
        return ctx;
    }

    public void ResponderConArchivo(HttpListenerContext context, string pathAlArchivo)
    {
        var response = context.Response;
        var responseString = File.Exists(pathAlArchivo) ? File.ReadAllText(pathAlArchivo) : $"{pathAlArchivo} no encontrado";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        System.IO.Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
    }

    void OnGUI()
    {
        var servidorActualActivo = ServidorActualActivo; // para no llamar locks incesantemente
        GUILayout.Toggle(HayRed, "Hay Red");

        // GUI.BeginDisabledGroup(servidorActualActivo);
        GUILayout.TextField($"IP Local: {DireccionIPLocal}");
        //GUI.EndDisabledGroup();

        GUILayout.TextField($"Carpeta: {Path.Combine(Application.dataPath, CarpetaWWW)}");

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Puerto Para Usar");
            PuertoDeServidor = int.Parse(GUILayout.TextField(PuertoDeServidor.ToString()));
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Carpeta WWW");
            GUILayout.TextField(CarpetaWWW);
        }

        using (new GUILayout.HorizontalScope())
        {
            //GUI.BeginDisabledGroup(servidorActualActivo);
            if (servidorActualActivo) GUI.color = Color.green;
            if (GUILayout.Button(servidorActualActivo ? "Conectado" : "Conectar", GUILayout.Width(0), GUILayout.ExpandWidth(true)))
            {
                ActivarServidorActual();
            }
            GUI.color = Color.white;
            //EditorGUI.EndDisabledGroup();
            //EditorGUI.BeginDisabledGroup(!servidorActualActivo);
            if (!servidorActualActivo) GUI.color = Color.red;
            if (GUILayout.Button(servidorActualActivo ? "Desconectar" : "Desconectado", GUILayout.Width(0), GUILayout.ExpandWidth(true)))
            {
                DesactivarServidorActual();
            }
            GUI.color = Color.white;
            //EditorGUI.EndDisabledGroup();
        }

        logScroll = GUILayout.BeginScrollView(logScroll, GUILayout.ExpandHeight(true));
        var log = GetUpdatedSafeLog();
        for (int i = log.Count - 1; i >= 0; i--)
        {
            GUILayout.Label(log[i]);
        }
        GUILayout.EndScrollView();

        foreach (var efecto in rutasYEfectos.Keys)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(efecto);
            GUILayout.TextField(GetRutaDeProceso(efecto));
            GUILayout.EndHorizontal();
        }
    }

    protected virtual void ProcesarPedidoEnMainThread(HttpListenerContext ctx)
    {
        var efecto = rutasYEfectos.Keys.Select(k => new { efecto = k, ruta = GetRutaDeProceso(k) }).OrderByDescending(r => r.ruta.Length)
            .FirstOrDefault(r => ctx.Request.Url.AbsolutePath.IndexOf(r.ruta) == 0);
        if (efecto != null)
        {
            ThreadSafeLog($"proceso utilizado = {efecto}");
            var streamCarga = _cargaDePedidos.ContainsKey(ctx) ? _cargaDePedidos[ctx] : null;
            rutasYEfectos[efecto.efecto]?.Invoke(ctx, streamCarga);
        }
        else
        {
            var posibleArchivo = ctx.Request.Url.Segments.LastOrDefault();
            var indexUrl = Path.Combine(Application.dataPath, CarpetaWWW);
            indexUrl += ctx.Request.Url.AbsolutePath;
            if (!posibleArchivo.Contains('.')) indexUrl = Path.Combine(indexUrl, "index.html");
            var hayIndex = File.Exists(indexUrl);
            ThreadSafeLog($"enviando archivo como respuesta = {indexUrl}");
            if (hayIndex)
            {
                ResponderConArchivo(ctx, indexUrl);
            }
            else
            {
                ResponderQueNoHayIndex(ctx.Response);
            }
        }
    }

    protected virtual void Update()
    {
        var ctxAProcesar = TomarPedido();
        if (ctxAProcesar != null)
        {
            ProcesarPedidoEnMainThread(ctxAProcesar);
            ctxAProcesar.Response.OutputStream.Close();// cierre extra por las dudas
        }
    }

    void OnEnable()
    {
        //EditorApplication.update -= Update;
        //EditorApplication.update += Update;
        //ActivarServidorActual();

    }
    void OnDisable()
    {
        //EditorApplication.update -= Update;
        DesactivarServidorActual();
    }

    // ACORDARSE ACA NADA DE ACCIONES DE UNITY OK?!
    void ProgramaDelServidor(Thread yoMismo, int puerto)
    {
        var url = $"http://+:{puerto}/";

        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        ThreadSafeLog($"Servidor iniciado en {url.Replace("+", DireccionIPLocal)}\n=== === === === ===", true);

        System.AsyncCallback alTenerContexto = null;
        alTenerContexto = (resultado) =>
        {
            try
            {
                var ctx = listener.EndGetContext(resultado);
                listener.BeginGetContext(alTenerContexto, null);

                var request = ctx.Request;
                ThreadSafeLog($"Pedido recibido de {request.RemoteEndPoint.ToString()} hacia {request.Url.ToString()}\nruta : {request.Url.AbsolutePath}\n{request.ContentType}");

                var carga = new MultipartParser(request.InputStream, request.ContentEncoding);
                Debug.Log($"carga.Success {carga.Success}");

                AgregarPedido(ctx, carga);
            }
            catch (System.ObjectDisposedException) { }
        };

        // este contexto arranca la cadena de eventos
        listener.BeginGetContext(alTenerContexto, null);
        while (yoMismo == ServidorActual)
        {
            Thread.Sleep(100);
        }

        listener.Stop();
        listener.Close();
        ThreadSafeLog($"=== === === === ===\nServidor en {url} terminado", true);
    }

    static void ResponderQueNoHayIndex(HttpListenerResponse response)
    {
        string responseString = "<HTML><BODY><H3>No hay un 'index.html' en la carpeta WWW para presentar, ni una respuesta asignada a esta ruta...</H3></BODY></HTML>";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        // Get a response stream and write the response to it.
        response.ContentLength64 = buffer.Length;
        System.IO.Stream output = response.OutputStream;
        response.StatusCode = (int)HttpStatusCode.OK;
        output.Write(buffer, 0, buffer.Length);
        // You must close the output stream.
        output.Close();
    }
    public static void ResponderString(HttpListenerResponse response, string respuesta, bool cerrarAlTerminar = true)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(respuesta);
        // Get a response stream and write the response to it.
        response.ContentLength64 = buffer.Length;
        System.IO.Stream output = response.OutputStream;
        response.StatusCode = (int)HttpStatusCode.OK;
        output.Write(buffer, 0, buffer.Length);
        // You must close the output stream.
        if (cerrarAlTerminar) output.Close();
    }
    /// <summary>
    /// MultipartParser http://multipartparser.codeplex.com
    /// Reads a multipart form data stream and returns the filename, content type and contents as a stream.
    /// 2009 Anthony Super http://antscode.blogspot.com
    /// <remarks>This expects a single file, multifiles not supported (yet...)</remarks>
    /// </summary>

    public class MultipartParser
    {
        public MultipartParser(Stream stream)
        {
            this.Parse(stream, Encoding.UTF8);
        }

        public MultipartParser(Stream stream, Encoding encoding)
        {
            this.Parse(stream, encoding);
        }

        private void Parse(Stream stream, Encoding encoding)
        {
            this.Success = false;

            // Read the stream into a byte array
            byte[] data = ToByteArray(stream);

            // Copy to a string for header parsing
            string content = encoding.GetString(data);

            // The first line should contain the delimiter
            int delimiterEndIndex = content.IndexOf("\r\n");

            if (delimiterEndIndex > -1)
            {
                string delimiter = content.Substring(0, content.IndexOf("\r\n"));

                // Look for Content-Type
                Regex re = new Regex(@"(?<=Content\-Type:)(.*?)(?=\r\n\r\n)");
                Match contentTypeMatch = re.Match(content);

                // Look for filename
                re = new Regex(@"(?<=filename\=\"")(.*?)(?=\"")");
                Match filenameMatch = re.Match(content);

                // Did we find the required values?
                if (contentTypeMatch.Success && filenameMatch.Success)
                {
                    // Set properties
                    this.ContentType = contentTypeMatch.Value.Trim();
                    this.Filename = filenameMatch.Value.Trim();

                    // Get the start & end indexes of the file contents
                    int startIndex = contentTypeMatch.Index + contentTypeMatch.Length + "\r\n\r\n".Length;

                    byte[] delimiterBytes = encoding.GetBytes("\r\n" + delimiter);
                    int endIndex = IndexOf(data, delimiterBytes, startIndex);

                    int contentLength = endIndex - startIndex;

                    // Extract the file contents from the byte array
                    byte[] fileData = new byte[contentLength];

                    Buffer.BlockCopy(data, startIndex, fileData, 0, contentLength);

                    this.FileContents = fileData;
                    this.Success = true;
                }
            }
        }

        private int IndexOf(byte[] searchWithin, byte[] serachFor, int startIndex)
        {
            int index = 0;
            int startPos = Array.IndexOf(searchWithin, serachFor[0], startIndex);

            if (startPos != -1)
            {
                while ((startPos + index) < searchWithin.Length)
                {
                    if (searchWithin[startPos + index] == serachFor[index])
                    {
                        index++;
                        if (index == serachFor.Length)
                        {
                            return startPos;
                        }
                    }
                    else
                    {
                        startPos = Array.IndexOf<byte>(searchWithin, serachFor[0], startPos + index);
                        if (startPos == -1)
                        {
                            return -1;
                        }
                        index = 0;
                    }
                }
            }

            return -1;
        }

        private byte[] ToByteArray(Stream stream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }

        public bool Success
        {
            get;
            private set;
        }

        public string ContentType
        {
            get;
            private set;
        }

        public string Filename
        {
            get;
            private set;
        }

        public byte[] FileContents
        {
            get;
            private set;
        }
    }
}