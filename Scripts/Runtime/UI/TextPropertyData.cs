using System;
using Slimple.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Slimple.UI
{
    public enum TextDirection
    {
        Horizontal = 0,
        Vertical = 1,
    }
        
    [Serializable]
    public class TextPropertyData : ISerializationCallbackReceiver
    {
        [SerializeField]
        [FormerlySerializedAs("font")]
        internal Font m_Font;

        [SerializeField]
        [FormerlySerializedAs("fontSize")]
        internal int m_FontSize;

        [SerializeField]
        [FormerlySerializedAs("fontStyle")]
        internal FontStyle m_FontStyle;

        [SerializeField]
        internal bool m_BestFit;

        [SerializeField]
        internal int m_MinSize;

        [SerializeField]
        internal int m_MaxSize;

        [SerializeField]
        [FormerlySerializedAs("alignment")]
        internal TextAnchor m_Alignment;

        [SerializeField]
        internal bool m_AlignByGeometry;

        [SerializeField]
        [FormerlySerializedAs("richText")]
        internal bool m_RichText;

        [SerializeField]
        internal HorizontalWrapMode m_HorizontalOverflow;

        [SerializeField]
        internal VerticalWrapMode m_VerticalOverflow;

        [SerializeField]
        internal float m_LineSpacing;

        [SerializeField]
        internal TextDirection m_Direction;

        public static TextPropertyData defaultFontData
        {
            get
            {
                var fontData = new TextPropertyData
                {
                    m_FontSize  = 14,
                    m_LineSpacing = 1f,
                    m_FontStyle = FontStyle.Normal,
                    m_BestFit = false,
                    m_MinSize = 10,
                    m_MaxSize = 40,
                    m_Alignment = TextAnchor.UpperLeft,
                    m_HorizontalOverflow = HorizontalWrapMode.Wrap,
                    m_VerticalOverflow = VerticalWrapMode.Truncate,
                    m_RichText  = true,
                    m_AlignByGeometry = false,
                    m_Direction = TextDirection.Horizontal,
                };
                return fontData;
            }
        }

        public int pointSize => 64;
        
        public int padding => 5;

        public Font font
        {
            get { return m_Font; }
            set { m_Font = value; }
        }

        internal FontData fontData => new FontData {font = m_Font, pointSize = 64, padding = 5};

        public int fontSize
        {
            get { return m_FontSize; }
            set { m_FontSize = value; }
        }

        public FontStyle fontStyle
        {
            get { return m_FontStyle; }
            set { m_FontStyle = value; }
        }

        public bool bestFit
        {
            get { return m_BestFit; }
            set { m_BestFit = value; }
        }

        public int minSize
        {
            get { return m_MinSize; }
            set { m_MinSize = value; }
        }

        public int maxSize
        {
            get { return m_MaxSize; }
            set { m_MaxSize = value; }
        }

        public TextAnchor alignment
        {
            get { return m_Alignment; }
            set { m_Alignment = value; }
        }

        public bool alignByGeometry
        {
            get { return m_AlignByGeometry; }
            set { m_AlignByGeometry = value;  }
        }

        public bool richText
        {
            get { return m_RichText; }
            set { m_RichText = value; }
        }

        public HorizontalWrapMode horizontalOverflow
        {
            get { return m_HorizontalOverflow; }
            set { m_HorizontalOverflow = value; }
        }

        public VerticalWrapMode verticalOverflow
        {
            get { return m_VerticalOverflow; }
            set { m_VerticalOverflow = value; }
        }

        public float lineSpacing
        {
            get { return m_LineSpacing; }
            set { m_LineSpacing = value; }
        }

        public TextDirection direction
        {
            get { return m_Direction; }
            set { m_Direction = value; }
        }
        
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {}

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_FontSize = Mathf.Clamp(m_FontSize, 0, 300);
            m_MinSize = Mathf.Clamp(m_MinSize, 0, m_FontSize);
            m_MaxSize = Mathf.Clamp(m_MaxSize, m_FontSize, 300);
        }
    }
}
