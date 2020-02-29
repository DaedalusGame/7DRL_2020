﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    public class SpriteReference
    {
        //In ms
        long unloadTime = 50000;

        public string FileName;
        private Texture2D _Texture;
        private DateTime _LastUsed;
        private int _SubImageCount;
        public bool KeepLoaded;

        public delegate void LoadFunction();

        public LoadFunction OnLoad;

        public void SetLoadFunction(LoadFunction function)
        {
            OnLoad = function;
        }

        public int SubImageCount
        {
            get
            {
                _LastUsed = DateTime.Now;

                return Math.Max(1, _SubImageCount);
            }
            set
            {
                _SubImageCount = value;
            }
        }

        public virtual int Width
        {
            get
            {
                _LastUsed = DateTime.Now;

                if (_Texture == null)
                    return 0;

                return _Texture.Width / SubImageCount;
            }
        }

        public virtual int Height
        {
            get
            {
                _LastUsed = DateTime.Now;

                if (_Texture == null)
                    return 0;

                return _Texture.Height;
            }
        }

        public Rectangle Rect
        {
            get
            {
                return new Rectangle(0, 0, Width, Height);
            }
        }

        public Vector2 Middle
        {
            get
            {
                return new Vector2(Width / 2, Height / 2);
            }
        }

        public bool ShouldLoad
        {
            get
            {
                return KeepLoaded || (DateTime.Now - _LastUsed).TotalMilliseconds < unloadTime;
            }
            set
            {
                _LastUsed = DateTime.Now;
            }
        }

        public bool IsLoaded
        {
            get
            {
                return _Texture != null;
            }
        }

        public virtual Texture2D Texture
        {
            get
            {
                _LastUsed = DateTime.Now;

                if (_Texture == null)
                    return SpriteLoader.Instance.NullSprite;

                return _Texture;
            }
            set
            {
                _Texture = value;
            }
        }

        public SpriteReference(string filename)
        {
            FileName = filename;
        }

        public Rectangle GetFrameRect(int frame)
        {
            if (frame < 0)
                frame = (frame % SubImageCount) + SubImageCount;
            return new Rectangle((frame % SubImageCount) * Width, 0, Width, Height);
        }

        public Matrix GetFrameMatrix(int frame)
        {
            var w = GetFrameMaxU(frame) - GetFrameMinU(frame);
            var h = GetFrameMaxV(frame) - GetFrameMinV(frame);
            return Matrix.CreateScale(w, h, 1) * Matrix.CreateTranslation(GetFrameMinU(frame), GetFrameMinV(frame), 0);
        }

        public float GetFrameMinU(int frame)
        {
            return (frame % SubImageCount) * Width / Texture.Width;
        }

        public float GetFrameMaxU(int frame)
        {
            return (frame % SubImageCount + 1) * Width / Texture.Width;
        }

        public float GetFrameMinV(int frame)
        {
            return 0;
        }

        public float GetFrameMaxV(int frame)
        {
            return 1;
        }

        public void Unload()
        {
            _Texture = null;
        }
    }

    public class DynamicSprite : SpriteReference
    {
        public delegate void RenderDelegate();

        RenderDelegate Render;
        RenderTarget2D RenderTarget;

        public override Texture2D Texture
        {
            get
            {
                return RenderTarget;
            }
            set
            {
                //NOOP
            }
        }

        public override int Width => RenderTarget.Width;

        public override int Height => RenderTarget.Height;

        public DynamicSprite(string filename, RenderTarget2D renderTarget, RenderDelegate render, bool keepLoaded) : base(filename)
        {
            Render = render;
            RenderTarget = renderTarget;
            KeepLoaded = keepLoaded;
        }

        public void Update(GraphicsDevice device)
        {
            device.SetRenderTarget(RenderTarget);
            Render();
        }
    }

    class SpriteLoader
    {
        private Regex FileNameExpression = new Regex(@"^[\w,\s-]+_strip([\d]+)");

        private static SpriteLoader _Instance;

        public static SpriteLoader Instance
        {
            get
            {
                return _Instance;
            }
            set
            {
                if (_Instance == null) _Instance = value;
            }
        }

        public static void Init(GraphicsDevice device)
        {
            Instance = new SpriteLoader(device);
        }

        GraphicsDevice GraphicsDevice;
        public Texture2D NullSprite;
        public Dictionary<string, SpriteReference> Sprites = new Dictionary<string, SpriteReference>();
        List<SpriteReference> QueuedSprites = new List<SpriteReference>();
        List<SpriteReference> AllSprites = new List<SpriteReference>();
        List<DynamicSprite> DynamicSprites = new List<DynamicSprite>();
        int unloadcheck;

        public SpriteLoader(GraphicsDevice device)
        {
            GraphicsDevice = device;

            NullSprite = new Texture2D(GraphicsDevice, 1, 1);
        }

        public void Update(GameTime time)
        {
            if (QueuedSprites.Count == 0)
                QueuedSprites.AddRange(Sprites.Values.Where(x => !x.IsLoaded && x.ShouldLoad));

            if (QueuedSprites.Count > 0)
                do
                {
                    var spritereference = QueuedSprites.Last();
                    Load(spritereference);
                    QueuedSprites.RemoveAt(QueuedSprites.Count - 1);
                }
                while (QueuedSprites.Count > 0 && !time.IsRunningSlowly);

            if (AllSprites.Count > 0)
            {
                unloadcheck = (unloadcheck + 1) % AllSprites.Count;
                var sprite = AllSprites[unloadcheck];
                if (!sprite.ShouldLoad && sprite.IsLoaded)
                {
                    sprite.Unload();
                }
            }

            DynamicSprites.RemoveAll(sprite => !sprite.ShouldLoad);
        }

        public void Draw(GameTime time)
        {
            foreach (var sprite in DynamicSprites)
            {
                sprite.Update(GraphicsDevice);
            }
            GraphicsDevice.SetRenderTarget(null);
        }

        public SpriteReference AddSprite(string filename)
        {
            return AddSprite(filename, false);
        }

        public SpriteReference AddSprite(DynamicSprite sprite)
        {
            if (Sprites.ContainsKey(sprite.FileName))
                return Sprites[sprite.FileName];

            DynamicSprites.Add(sprite);
            Sprites.Add(sprite.FileName, sprite);
            return sprite;
        }

        public SpriteReference AddSprite(string filename, bool keeploaded)
        {
            if (Sprites.ContainsKey(filename))
                return Sprites[filename];

            var rval = new SpriteReference(filename) { KeepLoaded = keeploaded };
            Sprites.Add(filename, rval);
            AllSprites.Add(rval);
            return rval;
        }

        private void Load(SpriteReference reference)
        {
            int count = 1;
            FileInfo fileinfo = new FileInfo(reference.FileName + ".png");
            if (!fileinfo.Exists)
            {
                DirectoryInfo dirinfo = fileinfo.Directory;
                fileinfo = dirinfo.GetFiles(Path.GetFileNameWithoutExtension(fileinfo.Name) + "_strip*").FirstOrDefault();
            }

            if (fileinfo == null || !fileinfo.Exists)
                return;

            var match = FileNameExpression.Match(fileinfo.Name);
            if (match.Success)
                count = int.Parse(match.Groups[1].Value);

            FileStream stream = fileinfo.OpenRead();
            reference.Texture = Texture2D.FromStream(GraphicsDevice, stream);
            reference.SubImageCount = count;
            stream.Close();
            reference.OnLoad?.Invoke();
        }
    }
}
