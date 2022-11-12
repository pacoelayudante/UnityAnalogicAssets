using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniAnim
{
    public class MiniAnimFrame : MonoBehaviour
    {
        [SerializeField]
        RawImageAspectRatio _framePreview;

        [SerializeField]
        MiniAnimManager _miniAnimManager;

        [SerializeField]
        Button _addPrevBut;

        [SerializeField]
        Button _addNextBut;

        [SerializeField]
        Button _removeBut;

        public Texture2D TextureFrame
        {
            get => _framePreview.Image;
            set => _framePreview.SetImage(value);
        }
        
        private Vector2Int _originalFrameSize;
        public Vector2Int FrameSize => _originalFrameSize;

        private void Awake()
        {
            _removeBut.onClick.AddListener(Remove);
            _addNextBut.onClick.AddListener(AddNext);
            _addPrevBut.onClick.AddListener(AddPrev);
        }

        public void RecordOriginalFrameSize()
        {
            if (TextureFrame)
                _originalFrameSize = new Vector2Int(TextureFrame.width, TextureFrame.height);
        }

        private void AddNext()
        {
            _miniAnimManager.NuevaImagen(transform.parent.childCount - transform.GetSiblingIndex() - 3);
        }

        private void AddPrev()
        {
            _miniAnimManager.NuevaImagen(transform.parent.childCount - transform.GetSiblingIndex() - 2);
        }

        private void Remove()
        {
            _miniAnimManager.DestruirFrame(this);
        }
    }
}