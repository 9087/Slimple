
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace Slimple.Core
{
    public static class FontEngine
    {
        #region LoadFontFace

        public static FontEngineError LoadFontFace(Font font, int pointSize)
        {            
            return UnityEngine.TextCore.LowLevel.FontEngine.LoadFontFace(font, pointSize);
        }

        #endregion

        #region ResetAtlasTexture

        private delegate void ResetAtlasTextureDelegate(Texture2D texture);

        private static ResetAtlasTextureDelegate s_ResetAtlasTextureDelegate;

        public static void ResetAtlasTexture(Texture2D texture)
        {
            if (s_ResetAtlasTextureDelegate == null)
            {
                var methodInfo = typeof(UnityEngine.TextCore.LowLevel.FontEngine).GetMethod(nameof(ResetAtlasTexture), BindingFlags.Static | BindingFlags.NonPublic);
                Debug.Assert(methodInfo != null);
                s_ResetAtlasTextureDelegate =  (ResetAtlasTextureDelegate) Delegate.CreateDelegate(typeof(ResetAtlasTextureDelegate), methodInfo);
            }
            s_ResetAtlasTextureDelegate.Invoke(texture);
        }

        #endregion
        
        #region TryAddGlyphToTexture
        
        private delegate bool TryAddGlyphToTextureDelegate(
            uint glyphIndex,
            int padding,
            GlyphPackingMode packingMode,
            List<GlyphRect> freeGlyphRects,
            List<GlyphRect> usedGlyphRects,
            GlyphRenderMode renderMode,
            Texture2D texture,
            out Glyph glyph);

        private static TryAddGlyphToTextureDelegate s_TryAddGlyphToTextureDelegate;
        
        public static bool TryAddGlyphToTexture(
            uint glyphIndex,
            int padding,
            GlyphPackingMode packingMode,
            List<GlyphRect> freeGlyphRects,
            List<GlyphRect> usedGlyphRects,
            GlyphRenderMode renderMode,
            Texture2D texture,
            out Glyph glyph)
        {
            if (s_TryAddGlyphToTextureDelegate == null)
            {
                var methodInfo = typeof(UnityEngine.TextCore.LowLevel.FontEngine).GetMethod(nameof(TryAddGlyphToTexture), BindingFlags.Static | BindingFlags.NonPublic);
                Debug.Assert(methodInfo != null);
                s_TryAddGlyphToTextureDelegate =  (TryAddGlyphToTextureDelegate) Delegate.CreateDelegate(typeof(TryAddGlyphToTextureDelegate), methodInfo);
            }
            return s_TryAddGlyphToTextureDelegate.Invoke(glyphIndex, padding, packingMode, freeGlyphRects, usedGlyphRects, renderMode, texture, out glyph);
        }

        #endregion

        #region TryGetGlyphIndex

        public static bool TryGetGlyphIndex(uint unicode, out uint glyphIndex)
        {
            return UnityEngine.TextCore.LowLevel.FontEngine.TryGetGlyphIndex(unicode, out glyphIndex);
        }

        #endregion

        #region GetFaceInfo
        
        public static FaceInfo GetFaceInfo()
        {
            return UnityEngine.TextCore.LowLevel.FontEngine.GetFaceInfo();
        }

        #endregion
    }
}