using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;

namespace MiniAnim
{
    [System.Serializable]
    public struct RawImageAspectRatio
    {
        public Texture2D Image => (Texture2D)_rawImage.texture;

        [SerializeField]
        RawImage _rawImage;
        [SerializeField]
        GameObject _parentControl;

        AspectRatioFitter _aspectRatioFitter;

        public void SetActive(bool active)
        {
            _parentControl.SetActive(active);
        }

        public void SetMaterial(Material mat)
        {
            _rawImage.material = mat;
        }

        public void SetImage(Texture2D newImage)
        {
            _rawImage.texture = newImage;
            FitAspectRatio();
        }

        public void FitAspectRatio()
        {
            if (_aspectRatioFitter == null)
                _aspectRatioFitter = _rawImage.GetComponent<AspectRatioFitter>();
            _aspectRatioFitter.aspectRatio = _rawImage.texture.width / (float)_rawImage.texture.height;
        }
    }

    public class MiniAnimManager : MonoBehaviour
    {
        [SerializeField]
        GameObject _frameListView;

        [SerializeField]
        MiniAnimFrame _frameButtonTemplate;

        [SerializeField]
        RawImageAspectRatio _anim;

        [SerializeField]
        RawImageAspectRatio _framePreview;

        [SerializeField]
        private int _maxImageSize = 512;
        [SerializeField]
        private float _minSegmentLength = 40f;
        [SerializeField]
        private LineSegmentDetectorModes _mode = LineSegmentDetectorModes.RefineNone;
        [SerializeField]
        private HomographyMethods _homographyMethod = HomographyMethods.Ransac;
        [SerializeField]
        private double _ransacReprojThreshold = 5d;
        [SerializeField]
        private Material _debugMaterial;
        [SerializeField]
        private Vector2 _customSegmentLengthRange = new Vector2(20, 300);

        [SerializeField]
        private float _frameRate = 6f;
        [SerializeField]
        private int _maxPreviewImageSize = 256;

        private Mat _inputMat;
        private MiniAnimFrame _lastAddedFrame;

        public float FrameRate { get => _frameRate; set => _frameRate = value; }

        public float MinSegmentLength
        {
            get => _minSegmentLength;
            set
            {
                _minSegmentLength = value;
                InstancedDebugMaterial.SetFloat("_Threshold", MinSegmentLengthNormalized);
            }
        }

        public float MinSegmentLengthNormalized
        {
            get => Mathf.InverseLerp(_customSegmentLengthRange[0], _customSegmentLengthRange[1], MinSegmentLength);
            set => MinSegmentLength = Mathf.Lerp(_customSegmentLengthRange[0], _customSegmentLengthRange[1], value);
        }

        private Material _instancedDebugMaterial;
        private Material InstancedDebugMaterial => _instancedDebugMaterial ? _instancedDebugMaterial : _instancedDebugMaterial = Instantiate(_debugMaterial);

        float currentFrame = 0f;

        List<MiniAnimFrame> _frames = new List<MiniAnimFrame>();

        private void Update()
        {
            if (_frames.Count > 0)
            {
                currentFrame += Time.deltaTime * _frameRate;
                currentFrame %= _frames.Count;

                _anim.SetImage(_frames[Mathf.FloorToInt(currentFrame)].TextureFrame);
            }
        }

        public void VerAnim(bool ver)
        {
            _anim.SetActive(ver);
            _frameListView.SetActive(!ver);
        }

        public void NuevaImagen(int newFrameIndexOffset)
        {
            AbrirMediaInput(newFrameIndexOffset);
        }

        private void AbrirMediaInput(int newFrameIndexOffset)
        {
            var permiso = NativeCamera.TakePicture(path =>
            {
                if (!string.IsNullOrEmpty(path))
                {
                    Texture2D imagenRecuperada = NativeCamera.LoadImageAtPath(path, -1, false, false);
                    if (imagenRecuperada)
                    {
                        var warpedOutput = ProcesarImagenRecuperada(imagenRecuperada, replaceTexture: null);
                        AgregarFrame(warpedOutput, newFrameIndexOffset);
                    }
                }
            });

            if (permiso == NativeCamera.Permission.ShouldAsk)
            {
                if (NativeCamera.CanOpenSettings()) NativeCamera.OpenSettings();
            }
        }

        public void ReintentarProceso()
        {
            if (_inputMat != null && !_inputMat.IsDisposed)
            {
                ProcesarImagenRecuperada(_inputMat, _framePreview.Image);
                _framePreview.FitAspectRatio();
                _framePreview.SetMaterial(null);
            }
        }

        public void DrawDebugMat()
        {
            using var inputGray = new Mat();
            Cv2.CvtColor(_inputMat, inputGray, ColorConversionCodes.BGR2GRAY);

            float escalaInversa = 1f;
            float width = _inputMat.Width;
            float height = _inputMat.Height;
            if (_maxImageSize > 0 && (width > _maxImageSize || height > _maxImageSize))
            {
                escalaInversa = Mathf.Max(width, height) / _maxImageSize;
                float escala = 1f / escalaInversa;
                Cv2.Resize(inputGray, inputGray, Size.Zero, escala, escala);
                width = inputGray.Width;
                height = inputGray.Height;
            }

            using var detector = LineSegmentDetector.Create(_mode);
            detector.Detect(inputGray, out Vec4f[] lines, out double[] widths, out double[] prec, out double[] nfa);

            inputGray.SetTo(Scalar.Black);
            foreach (var line in lines)
            {
                Vector2 pA = new Vector2(line[0], line[1]);
                Vector2 pB = new Vector2(line[2], line[3]);
                var dist = Vector2.Distance(pA, pB);
                var value = Mathf.InverseLerp(_customSegmentLengthRange[0], _customSegmentLengthRange[1], dist) * 255f;
                var lineColors = new Scalar(value, value, value);

                int x1 = Mathf.FloorToInt(line[0]);
                int y1 = Mathf.FloorToInt(line[1]);
                int x2 = Mathf.FloorToInt(line[2]);
                int y2 = Mathf.FloorToInt(line[3]);
                Cv2.Line(inputGray, x1, y1, x2, y2, lineColors, 2);
            }

            OpenCvSharp.Unity.MatToTexture(inputGray, _framePreview.Image);
            _framePreview.SetMaterial(InstancedDebugMaterial);
            _framePreview.FitAspectRatio();
        }

        private Texture2D ProcesarImagenRecuperada(Texture2D imagenRecuperada, Texture2D replaceTexture)
        {
            if (_inputMat != null && !_inputMat.IsDisposed)
                _inputMat.Dispose();
            //using var inputMat...
            _inputMat = OpenCvSharp.Unity.TextureToMat(imagenRecuperada);
            Destroy(imagenRecuperada);

            return ProcesarImagenRecuperada(_inputMat, replaceTexture);
        }

        private Texture2D ProcesarImagenRecuperada(Mat inputMat, Texture2D replaceTexture)
        {
            using var inputGray = new Mat();
            Cv2.CvtColor(inputMat, inputGray, ColorConversionCodes.BGR2GRAY);

            float escalaInversa = 1f;
            float width = inputMat.Width;
            float height = inputMat.Height;
            if (_maxImageSize > 0 && (width > _maxImageSize || height > _maxImageSize))
            {
                escalaInversa = Mathf.Max(width, height) / _maxImageSize;
                float escala = 1f / escalaInversa;
                Cv2.Resize(inputGray, inputGray, Size.Zero, escala, escala);
                width = inputGray.Width;
                height = inputGray.Height;
            }

            using var detector = LineSegmentDetector.Create(_mode);
            detector.Detect(inputGray, out Vec4f[] lines, out double[] widths, out double[] prec, out double[] nfa);

            // 0 is top, height is bottom
            Vec4f lineBottom = new Vec4f(0, height, width, height),
                lineLeft = new Vec4f(0, 0, 0, height),
                lineTop = new Vec4f(0, 0, width, 0),
                lineRight = new Vec4f(width, 0, width, height);
            float middleW = width / 2f;
            float middleH = height / 2f;
            foreach (var line in lines)
            {
                Vector2 pA = new Vector2(line[0], line[1]);
                Vector2 pB = new Vector2(line[2], line[3]);

                if (Vector2.Distance(pA, pB) > _minSegmentLength)
                {
                    if ((pA.x > middleW) ^ (pB.x > middleW))
                    {// horizontal
                        if ((pA.y > middleH) && (pB.y > middleH))
                        {// arriba
                            if (pA.y < lineBottom[1])
                                lineBottom = line;
                        }

                        if ((pA.y < middleH) && (pB.y < middleH))
                        {// abajo
                            if (pA.y > lineTop[1])
                                lineTop = line;
                        }
                    }

                    if ((pA.y > middleH) ^ (pB.y > middleH))
                    {//vertical
                        if ((pA.x > middleW) && (pB.x > middleW))
                        {// der
                            if (pA.x < lineRight[0])
                                lineRight = line;
                        }

                        if ((pA.x < middleW) && (pB.x < middleW))
                        {// izq
                            if (pA.x > lineLeft[0])
                                lineLeft = line;
                        }
                    }
                }
            }

            Vector2 topLeft = Vector2.zero;
            LineIntersection(lineTop, lineLeft, ref topLeft);
            Vector2 topRight = Vector2.zero;
            LineIntersection(lineTop, lineRight, ref topRight);
            Vector2 bottomLeft = Vector2.zero;
            LineIntersection(lineBottom, lineLeft, ref bottomLeft);
            Vector2 bottomRight = Vector2.zero;
            LineIntersection(lineBottom, lineRight, ref bottomRight);

            // volvemos a tamaño original de imagen
            topLeft *= escalaInversa;
            topRight *= escalaInversa;
            bottomLeft *= escalaInversa;
            bottomRight *= escalaInversa;
            var maxWidth = Mathf.Max(Vector2.Distance(topLeft, topRight), Vector2.Distance(bottomLeft, bottomRight));
            var maxHeight = Mathf.Max(Vector2.Distance(topLeft, bottomLeft), Vector2.Distance(topRight, bottomRight));
            var srcPoints = new[]
            {
                new Point2d(topLeft.x, topLeft.y),
                new Point2d(topRight.x, topRight.y),
                new Point2d(bottomRight.x, bottomRight.y),
                new Point2d(bottomLeft.x, bottomLeft.y),
            };
            var dstPoints = new[]
            {
                new Point2d(0, 0),
                new Point2d(maxWidth, 0),
                new Point2d(maxWidth, maxHeight),
                new Point2d(0, maxHeight),
            };
            using var homography = Cv2.FindHomography(srcPoints, dstPoints, _homographyMethod, _ransacReprojThreshold);
            using var warpedImg = new Mat();
            Cv2.WarpPerspective(inputMat, warpedImg, homography, new Size(maxWidth, maxHeight));

            return OpenCvSharp.Unity.MatToTexture(warpedImg, replaceTexture);
        }

        private void AgregarFrame(Texture2D newTexture, int newFrameIndexOffset)
        {
            _lastAddedFrame = Instantiate(_frameButtonTemplate, _frameButtonTemplate.transform.parent);
            int frameIndex = _frames.Count - newFrameIndexOffset;
            _frames.Insert(frameIndex, _lastAddedFrame);
            _lastAddedFrame.transform.SetSiblingIndex(frameIndex);
            _lastAddedFrame.gameObject.SetActive(true);
            _lastAddedFrame.TextureFrame = newTexture;

            ConfirmarNuevoFrame(newTexture);
        }

        private void ConfirmarNuevoFrame(Texture2D newTexture)
        {
            _framePreview.SetImage(newTexture);

            _frameListView.SetActive(false);
            _framePreview.SetActive(true);
            _framePreview.SetMaterial(null);
        }

        public void ConfirmarNuevoFrameCancelar()
        {
            MiniAnimFrame actualPreview = null;
            foreach (var cada in _frames)
            {
                if (cada.TextureFrame == _framePreview.Image)
                {
                    actualPreview = cada;
                    break;
                }
            }

            if (actualPreview)
                DestruirFrame(actualPreview);

            _frameListView.SetActive(true);
            _framePreview.SetActive(false);
        }

        public void DestruirFrame(MiniAnimFrame paraDestruir)
        {
            if (paraDestruir == _lastAddedFrame)
                _lastAddedFrame = null;

            Destroy(paraDestruir.TextureFrame);
            _frames.Remove(paraDestruir);
            Destroy(paraDestruir.gameObject);
        }

        public void ConfirmarNuevoFrameAceptar()
        {
            var createdTexture = _lastAddedFrame.TextureFrame;
            var pngBytes = ImageConversion.EncodeToPNG(createdTexture);
            var imgname = $"frame_{_frames.IndexOf(_lastAddedFrame)}_id_{System.DateTime.Now.Ticks}";
            _lastAddedFrame.name = imgname;

#if UNITY_EDITOR
            // For testing purposes, also write to a file in the project folder
            System.IO.File.WriteAllBytes($"{Application.dataPath}/{imgname}.png", pngBytes);
#endif
            var cachepath = $"{Application.temporaryCachePath}/{imgname}.png";
            Debug.Log($"writing to {cachepath}");
            System.IO.File.WriteAllBytes(cachepath, pngBytes);

            if (_maxPreviewImageSize > 0 && (createdTexture.width > _maxPreviewImageSize || createdTexture.height > _maxPreviewImageSize))
            {
                using var inputTexture = OpenCvSharp.Unity.TextureToMat(createdTexture);
                float width = createdTexture.width;
                float height = createdTexture.height;
                float escala = _maxPreviewImageSize / Mathf.Max(width, height);
                Cv2.Resize(inputTexture, inputTexture, Size.Zero, escala, escala);
                OpenCvSharp.Unity.MatToTexture(inputTexture, createdTexture);
            }

            _frameListView.SetActive(true);
            _framePreview.SetActive(false);
        }

        // Infinite Line Intersection (line1 is p1-p2 and line2 is p3-p4)
        internal static bool LineIntersection(Vec4f l1, Vec4f l2, ref Vector2 result)
        {
            float bx = l1[2] - l1[0];
            float by = l1[3] - l1[1];
            float dx = l2[2] - l2[0];
            float dy = l2[3] - l2[1];
            float bDotDPerp = bx * dy - by * dx;
            if (bDotDPerp == 0)
            {
                return false;
            }
            float cx = l2[0] - l1[0];
            float cy = l2[1] - l1[1];
            float t = (cx * dy - cy * dx) / bDotDPerp;

            result.x = l1[0] + t * bx;
            result.y = l1[1] + t * by;
            return true;
        }
    }
}