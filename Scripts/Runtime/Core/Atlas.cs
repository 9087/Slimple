using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;

namespace Slimple.Core
{
    public partial class Atlas : Reference
    {
        public static int Width { get; set; } = 512;
        public static int Height { get; set; } = 512;
        
        private static Dictionary<int, Atlas> s_Atlases = new();
        private static int s_Current = 0;

        public static Dictionary<int, Atlas> atlases => s_Atlases;
        
        public int id { get; private set; }

        private readonly List<GlyphRect> m_Free = new();
        private readonly List<GlyphRect> m_Used = new();

        private Texture2D m_Texture;

        public Texture2D texture => m_Texture;

        protected Atlas(int width, int height)
        {
            this.id = s_Current++;
            this.m_Texture = new Texture2D(width, height, TextureFormat.Alpha8, false);
            this.m_Free.Add(new GlyphRect(0, 0, width, height));
        }

        private static Atlas Create(int width, int height)
        {
            var atlas = new Atlas(width, height);
            atlas.Retain();
            s_Atlases.Add(atlas.id, atlas);
            return atlas;
        }
        
        public override void OnDestroy()
        {
            Object.DestroyImmediate(m_Texture);
            m_Texture = null;
        }
    }
}