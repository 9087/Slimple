using System;
using System.Collections.Generic;
using System.Globalization;
using Slimple.Core;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using UnityEngine.TextCore;
using UnityEngine.UI;
using CharacterInfo = Slimple.Core.CharacterInfo;
using FontData = Slimple.Core.FontData;

namespace Slimple.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Slimple UI - Text")]
    public class Text
        // Editor: Slime.Editor.UI.TextEditor
        : MaskableGraphic 
    {
        #region Utility

        protected bool SetProperty<T>(ref T currentValue, T newValue)
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
            {
                return false;
            }
            currentValue = newValue;
            return true;
        }
        
        #endregion

        #region Properties
        
        [SerializeField]
        [FormerlySerializedAs("m_FontData")]
        private TextPropertyData m_TextPropertyData = TextPropertyData.defaultFontData;
        
        #region Text
        
        [TextArea(3, 10)][SerializeField] private string m_Text;

        public string text
        {
            get => m_Text;
            set
            {
                if (!SetProperty(ref m_Text, value))
                {
                    return;
                }
                SetVerticesDirty();
                SetLayoutDirty();
            }
        }
        
        #endregion
        
        #region Font

        public Font font
        {
            get => m_TextPropertyData.m_Font;
            set
            {
                if (!SetProperty(ref m_TextPropertyData.m_Font, value))
                {
                    return;
                }
                SetAllDirty();
            }
        }
        
        #endregion

        #region FontStyle
        
        public FontStyle fontStyle
        {
            get => m_TextPropertyData.m_FontStyle;
            set
            {
                if (!SetProperty(ref m_TextPropertyData.m_FontStyle, value))
                {
                    return;
                }
                SetAllDirty();
            }
        }
        
        #endregion

        #region FontSize

        public int fontSize
        {
            get => m_TextPropertyData.m_FontSize;
            set
            {
                if (!SetProperty(ref m_TextPropertyData.m_FontSize, value))
                {
                    return;
                }
                SetVerticesDirty();
            }
        }
        
        #endregion

        #region Alignment
        
        public TextAnchor alignment
        {
            get => m_TextPropertyData.m_Alignment;
            set
            {
                if (!SetProperty(ref m_TextPropertyData.m_Alignment, value))
                {
                    return;
                }
                SetVerticesDirty();
            }
        }

        #endregion

        #region HorizontalOverflow
        
        public HorizontalWrapMode horizontalOverflow
        {
            get => m_TextPropertyData.m_HorizontalOverflow;
            set
            {
                if (!SetProperty(ref m_TextPropertyData.m_HorizontalOverflow, value))
                {
                    return;
                }
                SetVerticesDirty();
            }
        }

        #endregion

        #region VerticalOverflow
        
        public VerticalWrapMode verticalOverflow
        {
            get => m_TextPropertyData.m_VerticalOverflow;
            set
            {
                if (!SetProperty(ref m_TextPropertyData.m_VerticalOverflow, value))
                {
                    return;
                }
                SetVerticesDirty();
            }
        }

        #endregion

        #region Direction
        
        public TextDirection direction
        {
            get => m_TextPropertyData.m_Direction;
            set
            {
                if (!SetProperty(ref m_TextPropertyData.m_Direction, value))
                {
                    return;
                }
                SetVerticesDirty();
            }
        }

        #endregion
        
        #endregion

        #region Component Lifecycle

        protected Text()
        {
            useLegacyMeshGeneration = false;
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            this.SetAllDirty();
        }

        protected override void OnDestroy()
        {
            ClearCharacterInfos();
            base.OnDestroy();
        }

        #endregion

        #region Font

        private static readonly Dictionary<FontData, FaceInfo> s_FaceInfos = new();

        private static FaceInfo GetFaceInfo(FontData fontData)
        {
            if (!s_FaceInfos.TryGetValue(fontData, out var faceInfo))
            {
                FontEngine.LoadFontFace(fontData.font, fontData.pointSize);
                faceInfo = FontEngine.GetFaceInfo();
                s_FaceInfos.Add(fontData, faceInfo);
            }
            return faceInfo;
        }

        private FaceInfo faceInfo => GetFaceInfo(m_TextPropertyData.fontData);

        #endregion
        
        #region Character Info

        private List<CharacterInfo> m_CharacterInfos = new();

        private void TryRefreshCharacterInfos()
        {
            var lastCharacterInfos = m_CharacterInfos;
            m_CharacterInfos = ListPool<CharacterInfo>.Get();
            for (int i = 0; m_Text != null && i < m_Text.Length; i++)
            {
                uint unicode = (uint)m_Text[i];
                var characterInfo = Atlas.GetOrCreateCharacterInfo(m_TextPropertyData.fontData, unicode);
                characterInfo.Retain();
                m_CharacterInfos.Add(characterInfo);   
            }
            foreach (var characterInfo in lastCharacterInfos)
            {
                characterInfo.Release();
            }
            ListPool<CharacterInfo>.Release(lastCharacterInfos);
        }

        private void ClearCharacterInfos()
        {
            foreach (var characterInfo in m_CharacterInfos)
            {
                characterInfo.Release();
            }
            m_CharacterInfos.Clear();
        }

        #endregion

        #region Typography

        public class Typography
        {
            public class Descriptor
            {
                public Vector2 first { get; private set; }
                
                public Vector2 second { get; private set; }
                
                public Vector2 diagonal { get; private set; }
                
                public Vector2 pivot { get; private set; }
                
                public Descriptor(Vector2 first, Vector2 second)
                {
                    this.first = first;
                    this.second = second;
                    this.diagonal = first + second;
                    this.pivot = (Vector2.one - this.diagonal) * 0.5f;
                }
            }

            private enum WalkingStyle
            {
                Horizontal,
                Vertical,
            }

            private static readonly Dictionary<WalkingStyle, Descriptor> s_Descriptor = new()
            {
                { WalkingStyle.Horizontal, new Descriptor(new Vector2(+1, +0), new Vector2(+0, -1)) },
                { WalkingStyle.Vertical,   new Descriptor(new Vector2(+0, -1), new Vector2(-1, +0)) },
            };
            
            private static Vector2 Abs(Vector2 value)
            {
                return new Vector2(Mathf.Abs(value.x), Mathf.Abs(value.y));
            }
            
            public Rect Set(Text textComponent, Rect rect, VertexHelper vh)
            {
                Descriptor typographyWalkingDescriptor;
                switch (textComponent.direction)
                {
                    case TextDirection.Horizontal:
                        typographyWalkingDescriptor = s_Descriptor[WalkingStyle.Horizontal];
                        break;
                    case TextDirection.Vertical:
                        typographyWalkingDescriptor = s_Descriptor[WalkingStyle.Vertical];
                        break;
                    default:
                        throw new NotImplementedException();
                }
                
                Color32 color32 = textComponent.color;
                var faceInfo = textComponent.faceInfo;
                var fontData = textComponent.m_TextPropertyData.fontData;
                Vector2 startPosition = rect.position + rect.size * typographyWalkingDescriptor.pivot;
                Vector2 endPosition = rect.position + rect.size * (Vector2.one - typographyWalkingDescriptor.pivot);
                Vector2 currentPosition = startPosition;
                int count = 0;
                float fontScale = (float)textComponent.fontSize / fontData.pointSize;
                foreach (var characterInfo in textComponent.m_CharacterInfos)
                {
                    var glyphInfo = characterInfo.glyphInfo;
                    if (glyphInfo == null)
                    {
                        continue;
                    }
                    var glyphRect = glyphInfo.glyphRect;
                    var metrics = glyphInfo.glyph.metrics;
                    
                    var category = CharUnicodeInfo.GetUnicodeCategory((int) characterInfo.characterData.unicode);
                    bool cjk = category == UnicodeCategory.OtherLetter || category == UnicodeCategory.OtherPunctuation;

                    Descriptor characterWalkingDescriptor;
                    switch (textComponent.direction)
                    {
                        case TextDirection.Horizontal:
                            characterWalkingDescriptor = s_Descriptor[WalkingStyle.Horizontal];
                            break;
                        case TextDirection.Vertical:
                            characterWalkingDescriptor = s_Descriptor[cjk ? WalkingStyle.Horizontal : WalkingStyle.Vertical];
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    
                    var characterSize = new Vector2(metrics.horizontalAdvance, faceInfo.lineHeight);
                    Vector2 GetNextPosition() => currentPosition +
                                                 fontScale *
                                                 typographyWalkingDescriptor.first *
                                                 Abs(characterSize.x * characterWalkingDescriptor.first + characterSize.y * characterWalkingDescriptor.second);

                    var nextPosition = GetNextPosition();
                    var firstOverflow = Vector2.Dot(nextPosition - endPosition, typographyWalkingDescriptor.first);
                    if (textComponent.horizontalOverflow == HorizontalWrapMode.Wrap && firstOverflow > 0)
                    {
                        currentPosition = startPosition * Abs(typographyWalkingDescriptor.first) + currentPosition * Abs(typographyWalkingDescriptor.second) + fontScale * faceInfo.lineHeight * typographyWalkingDescriptor.second;
                        nextPosition = GetNextPosition();
                    }
                    var secondOverflow = Vector2.Dot(nextPosition - endPosition, typographyWalkingDescriptor.second);
                    if (textComponent.verticalOverflow == VerticalWrapMode.Truncate && secondOverflow > -fontScale * faceInfo.lineHeight)
                    {
                        break;
                    }
                    
                    var glyphSize = new Vector2(glyphRect.width, glyphRect.height);
                    var first = glyphSize.x * characterWalkingDescriptor.first;
                    var second = glyphSize.y * characterWalkingDescriptor.second;
                    Vector2 v0 = currentPosition +
                                 fontScale *
                                 (
                                    characterWalkingDescriptor.first * (metrics.horizontalBearingX - fontData.padding) +
                                    characterWalkingDescriptor.second * (-metrics.horizontalBearingY + faceInfo.ascentLine - fontData.padding) +
                                    (characterWalkingDescriptor.pivot - typographyWalkingDescriptor.pivot) * Abs(metrics.horizontalAdvance * characterWalkingDescriptor.first + glyphSize.y * characterWalkingDescriptor.second)
                                 );
                    Vector2 v1 = v0 + fontScale * second;
                    Vector2 v2 = v1 + fontScale * first;
                    Vector2 v3 = v0 + fontScale * first;
                    
                    vh.AddVert(new Vector3(v0.x, v0.y, 0), color32, new Vector2(glyphRect.x, glyphRect.y + glyphRect.height));
                    vh.AddVert(new Vector3(v1.x, v1.y, 0), color32, new Vector2(glyphRect.x, glyphRect.y));
                    vh.AddVert(new Vector3(v2.x, v2.y, 0), color32, new Vector2(glyphRect.x + glyphRect.width, glyphRect.y));
                    vh.AddVert(new Vector3(v3.x, v3.y, 0), color32, new Vector2(glyphRect.x + glyphRect.width, glyphRect.y + glyphRect.height));
                    int offset = count * 4;
                    vh.AddTriangle(offset + 0, offset + 1, offset + 2);
                    vh.AddTriangle(offset + 2, offset + 3, offset + 0);
                    
                    currentPosition = nextPosition;
                    count++;
                }
                return rect;
            }
        }
        
        protected readonly Typography m_Typography = new();

        #endregion
        
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            var r = GetPixelAdjustedRect();
            vh.Clear();
            TryRefreshCharacterInfos();
            var preferredRect = m_Typography.Set(this, r, vh);
        }

        protected override void UpdateMaterial()
        {
            if (!IsActive())
                return;
            if (Atlas.atlases.Count == 0)
            {
                return;
            }
            canvasRenderer.materialCount = 1;
            var texture = Atlas.atlases[0].texture;
            var material = new Material(Shader.Find("Slime/UI/Text"));
            material.mainTexture = texture;
            material.SetFloat("_MainTexWidth", texture.width);
            material.SetFloat("_MainTexHeight", texture.height);
            material.SetFloat("_Padding", m_TextPropertyData.fontData.padding);
            canvasRenderer.SetMaterial(material, 0);
            canvasRenderer.SetTexture(texture);
        }
    }
}