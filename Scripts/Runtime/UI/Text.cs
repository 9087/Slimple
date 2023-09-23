using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Slimple.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using UnityEngine.TextCore;
using UnityEngine.UI;
using CharacterInfo = Slimple.Core.CharacterInfo;
using FontData = Slimple.Core.FontData;
using Object = UnityEngine.Object;

namespace Slimple.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Slimple UI - Text")]
    public class Text
        // Editor: Slime.Editor.UI.TextEditor
        : MaskableGraphic 
    {
        #region Editor
        #if UNITY_EDITOR
        
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReloading;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReloading;
        }

        private static void BeforeAssemblyReloading()
        {
            Text[] textComponents = FindObjectsOfType<Text>();
            foreach (var textComponent in textComponents)
            {
                foreach (var subordinate in textComponent.m_Subordinates)
                {
                    Object.DestroyImmediate(subordinate.gameObject);
                }
            }
        }

        #endif
        #endregion
        
        #region Utility

        private bool SetProperty<T>(ref T currentValue, T newValue)
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

        #region Mesh Info

        internal class VertexInfo
        {
            public Vector3 position;
            public Color32 color;
            public Vector2 uv0;
            public Vector4 property;

            public VertexInfo(Vector3 position, Color32 color, Vector2 uv0, Vector4 property)
            {
                this.position = position;
                this.color = color;
                this.uv0 = uv0;
                this.property = property;
            }
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

            private class Quad
            {
                public Vector2 v0;
                public Vector2 v1;
                public Vector2 v2;
                public Vector2 v3;

                public int r;

                private ref Vector2 this[int index]
                {
                    get
                    {
                        index = (index % 4 + 4) % 4;
                        switch (index)
                        {
                            case 0: return ref v0;
                            case 1: return ref v1;
                            case 2: return ref v2;
                            case 3: return ref v3;
                        }
                        throw new ArgumentOutOfRangeException();
                    }
                }

                public ref Vector2 lt => ref this[4 + 0 - r];
                public ref Vector2 lb => ref this[4 + 1 - r];
                public ref Vector2 rb => ref this[4 + 2 - r];
                public ref Vector2 rt => ref this[4 + 3 - r];
            }

            private static readonly Quad s_Quad = new();
            
            internal Rect Calculate(Text textComponent, Rect rect, Dictionary<int, List<VertexInfo>> allVertexInfos)
            {
                foreach (var (_, vertexInfos) in allVertexInfos)
                {
                    ListPool<VertexInfo>.Release(vertexInfos);
                }
                allVertexInfos.Clear();
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
                    
                var fontStyle = textComponent.m_TextPropertyData.fontStyle;
                var property = new Vector4(
                    (fontStyle == FontStyle.Bold || fontStyle == FontStyle.BoldAndItalic) ? 0.1f : 0,
                    0,
                    0,
                    0);
                
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
                    s_Quad.v0 = currentPosition +
                                 fontScale *
                                 (
                                    characterWalkingDescriptor.first * (metrics.horizontalBearingX - fontData.padding) +
                                    characterWalkingDescriptor.second * (-metrics.horizontalBearingY + faceInfo.ascentLine - fontData.padding) +
                                    (characterWalkingDescriptor.pivot - typographyWalkingDescriptor.pivot) * Abs(metrics.horizontalAdvance * characterWalkingDescriptor.first + glyphSize.y * characterWalkingDescriptor.second)
                                 );
                    s_Quad.v1 = s_Quad.v0 + fontScale * second;
                    s_Quad.v2 = s_Quad.v1 + fontScale * first;
                    s_Quad.v3 = s_Quad.v0 + fontScale * first;

                    var atlasID = glyphInfo.atlas.id;
                    if (!allVertexInfos.TryGetValue(atlasID, out var vertexInfos))
                    {
                        vertexInfos = ListPool<VertexInfo>.Get();
                        allVertexInfos[atlasID] = vertexInfos;
                    }
                    
                    s_Quad.r = Mathf.RoundToInt(Vector2.Angle(characterWalkingDescriptor.first, typographyWalkingDescriptor.first) / 90);

                    if (textComponent.fontStyle == FontStyle.Italic ||
                        textComponent.fontStyle == FontStyle.BoldAndItalic)
                    {
                        var offset = (s_Quad.lt - s_Quad.lb).magnitude * Mathf.Sin(20 * Mathf.PI / 180) * typographyWalkingDescriptor.first;
                        s_Quad.lt = s_Quad.lt + offset;
                        s_Quad.rt = s_Quad.rt + offset;
                    }
            
                    vertexInfos.Add(new VertexInfo(new Vector3(s_Quad.v0.x, s_Quad.v0.y, 0), color32, new Vector2(glyphRect.x, glyphRect.y + glyphRect.height), property));
                    vertexInfos.Add(new VertexInfo(new Vector3(s_Quad.v1.x, s_Quad.v1.y, 0), color32, new Vector2(glyphRect.x, glyphRect.y), property));
                    vertexInfos.Add(new VertexInfo(new Vector3(s_Quad.v2.x, s_Quad.v2.y, 0), color32, new Vector2(glyphRect.x + glyphRect.width, glyphRect.y), property));
                    vertexInfos.Add(new VertexInfo(new Vector3(s_Quad.v3.x, s_Quad.v3.y, 0), color32, new Vector2(glyphRect.x + glyphRect.width, glyphRect.y + glyphRect.height), property));
                    currentPosition = nextPosition;
                    count++;
                }
                return rect;
            }
        }

        private readonly Typography m_Typography = new();

        #endregion

        #region Mesh & Material
        
        [NonSerialized] private static readonly VertexHelper s_VertexHelper = new();
        [NonSerialized] private int[] m_AtlasIDs = new int[0];
        [NonSerialized] private RectTransform[] m_Subordinates = new RectTransform[0];
        [NonSerialized] private DrivenRectTransformTracker m_Tracker;

        private static readonly Vector4 s_DefaultTangent = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);
        private static readonly Vector3 s_DefaultNormal = Vector3.back;

        private static void UpdateGeometry(Text textComponent, RectTransform transform, List<VertexInfo> vertexInfos)
        {
            s_VertexHelper.Clear();
            Debug.Assert(vertexInfos.Count % 4 == 0);
            var quadCount = vertexInfos.Count / 4;
            VertexInfo info;
            for (int quadIndex = 0; quadIndex < quadCount; quadIndex++)
            {
                int offset = quadIndex * 4;
                info = vertexInfos[offset + 0]; s_VertexHelper.AddVert(info.position, info.color, info.uv0, info.property, s_DefaultNormal, s_DefaultTangent);
                info = vertexInfos[offset + 1]; s_VertexHelper.AddVert(info.position, info.color, info.uv0, info.property, s_DefaultNormal, s_DefaultTangent);
                info = vertexInfos[offset + 2]; s_VertexHelper.AddVert(info.position, info.color, info.uv0, info.property, s_DefaultNormal, s_DefaultTangent);
                info = vertexInfos[offset + 3]; s_VertexHelper.AddVert(info.position, info.color, info.uv0, info.property, s_DefaultNormal, s_DefaultTangent);
                s_VertexHelper.AddTriangle(offset + 0, offset + 1, offset + 2);
                s_VertexHelper.AddTriangle(offset + 2, offset + 3, offset + 0);
            }
            var components = ListPool<Component>.Get();
            transform.GetComponents(typeof(IMeshModifier), components);
            for (var i = 0; i < components.Count; i++)
            {
                ((IMeshModifier)components[i]).ModifyMesh(s_VertexHelper);
            }
            ListPool<Component>.Release(components);
            s_VertexHelper.FillMesh(workerMesh);
            transform.GetComponent<CanvasRenderer>().SetMesh(workerMesh);
        }

        protected override void UpdateGeometry()
        {
            if (rectTransform == null || rectTransform.rect.width < 0 || rectTransform.rect.height < 0)
            {
                base.UpdateGeometry();
                return;
            }
            var allVertexInfos = DictionaryPool<int, List<VertexInfo>>.Get();
            var r = GetPixelAdjustedRect();
            TryRefreshCharacterInfos();
            m_Typography.Calculate(this, r, allVertexInfos);
            this.canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            
            Array.Resize(ref m_AtlasIDs, allVertexInfos.Count);
            for (int i = m_Subordinates.Length; i > allVertexInfos.Count - 1 && i > 0; i--)
            {
                Object.DestroyImmediate(m_Subordinates[i - 1].gameObject);
                m_Subordinates[i - 1] = null;
            }
            if (allVertexInfos.Count - 1 != m_Subordinates.Length)
            {
                Array.Resize(ref m_Subordinates, Mathf.Max(allVertexInfos.Count - 1, 0));
            }
            for (int i = 0; i < m_Subordinates.Length; i++)
            {
                if (m_Subordinates[i] != null)
                {
                    continue;
                }
                var gameObject = new GameObject("Text Submesh");
                gameObject.hideFlags = HideFlags.HideAndDontSave;
                var rectTransform = gameObject.AddComponent<RectTransform>();
                rectTransform.SetParent(this.transform);
                gameObject.AddComponent<CanvasRenderer>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.All);
                m_Subordinates[i] = rectTransform;
            }
            int index = 0;
            foreach (var (atlasID, vertexInfos) in allVertexInfos)
            {
                var rectTransform = index == 0 ? this.transform as RectTransform : m_Subordinates[index - 1];
                UpdateGeometry(this, rectTransform, vertexInfos);
                if (m_AtlasIDs[index] != atlasID)
                {
                    m_AtlasIDs[index] = atlasID;
                    if (!m_MaterialDirty || index > 0)
                    {
                        UpdateMaterial(this, rectTransform, atlasID);
                    }
                }
                index++;
            }
            DictionaryPool<int, List<VertexInfo>>.Release(allVertexInfos);
        }

        protected override void OnPopulateMesh(VertexHelper vh) => throw new NotSupportedException();

        private static readonly int s_MainTexWidth = Shader.PropertyToID("_MainTexWidth");
        private static readonly int s_MainTexHeight = Shader.PropertyToID("_MainTexHeight");
        private static readonly int s_Padding = Shader.PropertyToID("_Padding");
        private bool m_MaterialDirty = false;

        public override void SetMaterialDirty()
        {
            if (!IsActive())
                return;
            m_MaterialDirty = true;
            base.SetMaterialDirty();
        }

        private static void UpdateMaterial(Text textComponent, RectTransform transform, int atlasID)
        {
            if (atlasID < 0)
            {
                return;
            }
            var canvasRenderer = transform.GetComponent<CanvasRenderer>();
            canvasRenderer.materialCount = 1;
            var texture = Atlas.atlases[atlasID].texture;
            var material = new Material(Shader.Find("Slime/UI/Text")) { mainTexture = texture };
            material.SetFloat(s_MainTexWidth, texture.width);
            material.SetFloat(s_MainTexHeight, texture.height);
            material.SetFloat(s_Padding, textComponent.m_TextPropertyData.fontData.padding);
            canvasRenderer.SetMaterial(material, 0);
            canvasRenderer.SetTexture(texture);
        }
        
        protected override void UpdateMaterial()
        {
            if (!IsActive())
                return;
            int index = 0;
            foreach (var atlasID in m_AtlasIDs)
            {
                var rectTransform = index == 0 ? this.transform as RectTransform : m_Subordinates[index - 1];
                UpdateMaterial(this, rectTransform, atlasID);
                index++;
            }
        }

        #endregion
    }
}