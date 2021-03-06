﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSharpGL;

namespace Texture2D
{
    class CrateTextureSource : ITextureSource
    {
        private readonly Texture texture;
        public CrateTextureSource(string filename)
        {
            var bmp = new System.Drawing.Bitmap(filename);
            bmp.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipX);
            texture = new Texture(TextureTarget.Texture2D,
                new TexImage2D(TexImage2D.Target.Texture2D, 0, (int)GL.GL_RGBA, bmp.Width, bmp.Height, 0, GL.GL_BGRA, GL.GL_UNSIGNED_BYTE, new ImageDataProvider(bmp)));
            texture.BuiltInSampler.Add(new TexParameteri(TexParameter.PropertyName.TextureWrapS, (int)GL.GL_CLAMP_TO_EDGE));
            texture.BuiltInSampler.Add(new TexParameteri(TexParameter.PropertyName.TextureWrapT, (int)GL.GL_CLAMP_TO_EDGE));
            texture.BuiltInSampler.Add(new TexParameteri(TexParameter.PropertyName.TextureWrapR, (int)GL.GL_CLAMP_TO_EDGE));
            texture.BuiltInSampler.Add(new TexParameteri(TexParameter.PropertyName.TextureMinFilter, (int)GL.GL_LINEAR));
            texture.BuiltInSampler.Add(new TexParameteri(TexParameter.PropertyName.TextureMagFilter, (int)GL.GL_LINEAR));

            texture.Initialize();
            bmp.Dispose();
        }
        #region ITextureSource 成员

        public Texture BindingTexture { get { return texture; } }

        #endregion
    }
}
