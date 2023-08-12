using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using FontEngine = Slimple.Core.FontEngine;

namespace Slimple.Core
{
    public class FontData : IEquatable<FontData>
    {
        public Font font;
        public int pointSize;
        public int padding;

        public bool Equals(FontData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(font, other.font) && pointSize == other.pointSize && padding == other.padding;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FontData) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(font, pointSize, padding);
        }
    }
        
    public struct GlyphData : IEquatable<GlyphData>
    {
        public FontData fontData;
        public uint glyphIndex;

        public bool Equals(GlyphData other)
        {
            return Equals(fontData, other.fontData) && glyphIndex == other.glyphIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is GlyphData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(fontData, glyphIndex);
        }
    }
    
    public class GlyphInfo : Reference
    {
        private readonly WeakReference<Atlas> m_Atlas;

        public GlyphData glyphData { get; }
        
        public Glyph glyph { get; }
        
        public GlyphRect glyphRect { get; }

        internal GlyphInfo(Atlas atlas, GlyphData glyphData, Glyph glyph, GlyphRect glyphRect)
        {
            this.m_Atlas = new(atlas);
            this.glyphData = glyphData;
            this.glyph = glyph;
            this.glyphRect = glyphRect;
        }

        public override void OnDestroy()
        {
            if (!m_Atlas.TryGetTarget(out var atlas))
            {
                return;
            }
            atlas.RemoveGlyphInfo(this);
        }
    }

    public struct CharacterData : IEquatable<CharacterData>
    {
        public FontData fontData;
        public uint unicode;

        public bool Equals(CharacterData other)
        {
            return Equals(fontData, other.fontData) && unicode == other.unicode;
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(fontData, unicode);
        }
    }

    public class CharacterInfo : Reference
    {
        public CharacterData characterData { get; }
        
        public GlyphInfo glyphInfo { get; }
        
        internal CharacterInfo(CharacterData characterData, GlyphInfo glyphInfo)
        {
            this.characterData = characterData;
            this.glyphInfo = glyphInfo;
            this.glyphInfo?.Retain();
        }
        
        public override void OnDestroy()
        {
            glyphInfo?.Release();
            Atlas.RemoveCharacterInfo(this);
        }
    }
    
    public partial class Atlas
    {
        private static Dictionary<CharacterData, CharacterInfo> m_CharacterInfos = new();
        
        private readonly Dictionary<GlyphData, GlyphInfo> m_GlyphInfos = new();

        public static CharacterInfo GetOrCreateCharacterInfo(FontData fontData, uint unicode)
        {
            CharacterData glyphData = new CharacterData() { fontData = fontData, unicode = unicode };
            return GetOrCreateCharacterInfo(glyphData);
        }
        
        public static CharacterInfo GetOrCreateCharacterInfo(CharacterData characterData)
        {
            if (!m_CharacterInfos.TryGetValue(characterData, out var characterInfo))
            {
                var fontData = characterData.fontData;
                if (FontEngineError.Success == FontEngine.LoadFontFace(fontData.font, fontData.pointSize) &&
                    FontEngine.TryGetGlyphIndex(characterData.unicode, out var glyphIndex))
                {
                    GlyphData glyphData = new GlyphData()
                    {
                        fontData = fontData,
                        glyphIndex = glyphIndex,
                    };
                    GlyphInfo glyphInfo = null;
                    foreach (var (atlasID, atlas) in s_Atlases)
                    {
                        glyphInfo = atlas.GetOrCreateGlyphInfo(glyphData);
                        if (glyphInfo != null)
                        {
                            break;
                        }
                    }
                    if (glyphInfo == null)
                    {
                        var atlas = Create(Width, Height);
                        FontEngine.ResetAtlasTexture(atlas.texture);
                        glyphInfo = atlas.GetOrCreateGlyphInfo(glyphData);
                        Debug.Assert(glyphInfo != null);
                    }
                    characterInfo = new CharacterInfo(characterData, glyphInfo);
                }
                else
                {
                    characterInfo = new CharacterInfo(characterData, null);
                }
                m_CharacterInfos.Add(characterData, characterInfo);
            }
            return characterInfo;
        }

        internal static void RemoveCharacterInfo(CharacterInfo characterInfo)
        {
            m_CharacterInfos.Remove(characterInfo.characterData);
        }
        
        public GlyphInfo GetOrCreateGlyphInfo(FontData fontData, uint glyphIndex)
        {
            GlyphData glyphData = new GlyphData() { fontData = fontData, glyphIndex = glyphIndex };
            return GetOrCreateGlyphInfo(glyphData);
        }
        
        public GlyphInfo GetOrCreateGlyphInfo(GlyphData glyphData)
        {
            if (!m_GlyphInfos.TryGetValue(glyphData, out var glyphInfo))
            {
                var fontData = glyphData.fontData;
                if (FontEngineError.Success != FontEngine.LoadFontFace(fontData.font, fontData.pointSize))
                {
                    return null;
                }
                int oldUsedGlyphRectCount = m_Used.Count;
                if (!FontEngine.TryAddGlyphToTexture(glyphData.glyphIndex, fontData.padding, GlyphPackingMode.BestShortSideFit, m_Free, m_Used, GlyphRenderMode.SDFAA, m_Texture, out var glyph))
                {
                    return null;
                }
                Debug.Assert((oldUsedGlyphRectCount + 1 == m_Used.Count) ^ (glyph.glyphRect.width * glyph.glyphRect.height == 0));
                glyphInfo = new GlyphInfo(this, glyphData, glyph, oldUsedGlyphRectCount == m_Used.Count ? GlyphRect.zero : m_Used[^1]);
                m_GlyphInfos.Add(glyphData, glyphInfo);
            }
            return glyphInfo;
        }

        internal void RemoveGlyphInfo(GlyphInfo glyphInfo)
        {
            this.m_GlyphInfos.Remove(glyphInfo.glyphData);
            var glyphRect = glyphInfo.glyphRect;
            if (glyphRect != GlyphRect.zero)
            {
                int index = this.m_Used.IndexOf(glyphRect);
                Debug.Assert(index >= 0);
                int lastIndex = this.m_Used.Count - 1;
                if (index != lastIndex)
                {
                    this.m_Used[index] = this.m_Used[lastIndex];
                }
                this.m_Used.RemoveAt(lastIndex);
                this.m_Free.Add(glyphRect);
            }
        }
    }
}