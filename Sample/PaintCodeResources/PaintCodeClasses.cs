﻿using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
//using VectorCodeResources.Fonts;

namespace PaintCode
{
    // Resizing Behavior
    public enum ResizingBehavior
    {
        AspectFit, //!< The content is proportionally resized to fit into the target rectangle.
        AspectFill, //!< The content is proportionally resized to completely fill the target rectangle.
        Stretch, //!< The content is stretched to match the entire target rectangle.
        Center, //!< The content is centered in the target rectangle, but it is NOT resized.
    }

    public static class Helpers
    {
        public static readonly SKColor ColorWhite = new SKColor(0xffffffff);
        public static readonly SKColor ColorGray = new SKColor(0xff888888);
        public static readonly SKColor ColorBlack = new SKColor(0xff000000);

        // Color.argb(
        public static SKColor ColorFromArgb(byte a, byte r, byte g, byte b)
        {
            return new SKColor(r, g, b, a);
        }

        //String.valueOf
        public static string StringValueOf(int value)
        {
            return value.ToString();
        }

        public static SKRect ResizingBehaviorApply(ResizingBehavior behavior, SKRect rect, SKRect target)
        {
            if (rect.Equals(target) || target == null)
            {
                return rect;
            }

            if (behavior == ResizingBehavior.Stretch)
            {
                return target;
            }

            float xRatio = Math.Abs(target.Width / rect.Width);
            float yRatio = Math.Abs(target.Height / rect.Height);
            float scale = 0f;

            switch (behavior)
            {
                case ResizingBehavior.AspectFit:
                    {
                        scale = Math.Min(xRatio, yRatio);
                        break;
                    }
                case ResizingBehavior.AspectFill:
                    {
                        scale = Math.Max(xRatio, yRatio);
                        break;
                    }
                case ResizingBehavior.Center:
                    {
                        scale = 1f;
                        break;
                    }
            }

            float newWidth = Math.Abs(rect.Width * scale);
            float newHeight = Math.Abs(rect.Height * scale);
            return new SKRect(target.MidX - newWidth / 2,
                target.MidY - newHeight / 2,
                target.MidX + newWidth / 2,
                target.MidY + newHeight / 2);
        }
    }

    public static class TypefaceManager
    {
        static Dictionary<string, SKTypeface> typefaceCache;

        public static SKTypeface GetTypeface(string fullFontName)
        {
            SKTypeface result;

            if (typefaceCache == null)
            {
                typefaceCache = new Dictionary<string, SKTypeface>();
            }
            else
            {
                if (typefaceCache.TryGetValue(fullFontName, out result))
                    return result;
            }

            var assembly = Assembly.GetExecutingAssembly();

            var stream = assembly.GetManifestResourceStream("PaintCodeResources.Fonts." + fullFontName);
            result = SKTypeface.FromStream(stream);

            typefaceCache[fullFontName] = result;

            return result;
        }

    }

    public class Context
    { }

    public class StaticLayout : IDisposable
    {
        private int width;
        private SKTextAlign alignment;
        private string source;
        private SKPaint paint;

        private SKRect textRect;
        private SKPath textPath;

        private SKRect highestLetterRect;

        private float verticalInset;

        public float getHeight()
        {
            return this.highestLetterRect.Height + 0.5f;
        }

        public StaticLayout(string source, SKPaint paint, int width, SKTextAlign alignment)
        {
            this.source = source;
            this.paint = paint;
            this.width = width;
            this.alignment = alignment;

            paint.MeasureText("A,", ref this.highestLetterRect);
            paint.MeasureText(source, ref this.textRect);

            this.verticalInset = (float)(highestLetterRect.Height - this.textRect.Height);
        }

        public void draw(SKCanvas canvas)
        {
            this.paint.TextAlign = this.alignment;

            if (this.textPath == null)
            {
                var baseline = this.textRect.Height + 1 * this.verticalInset - 1;
                textPath = new SKPath();
                textPath.AddPoly(new SKPoint[] { new SKPoint(0, baseline), new SKPoint(this.width, baseline) }, false);
            }

            canvas.DrawTextOnPath(source, textPath, 0, 0, this.paint);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.textPath != null)
                        this.textPath.Dispose();

                    // we can not dispose paint object, it's managed elsewhere
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }

    public static class Extensions
    {
        public static void AddRoundedRect(this SKPath path, SKRect rectangleRect, float[] rectangleCornerRadii, SKPathDirection direction)
        {
            if (rectangleCornerRadii[0] == 0f)
            {
                path.MoveTo(rectangleRect.Left, rectangleRect.Top);
            }
            else
            {
                path.MoveTo(rectangleRect.Left, rectangleRect.Bottom);
                path.ArcTo(rectangleRect.Left, rectangleRect.Top, rectangleRect.Right, rectangleRect.Top, rectangleCornerRadii[0]);
            }

            if (rectangleCornerRadii[2] == 0f)
            {
                path.LineTo(rectangleRect.Right, rectangleRect.Top);
            }
            else
            {
                path.ArcTo(rectangleRect.Right, rectangleRect.Top, rectangleRect.Right, rectangleRect.Bottom, rectangleCornerRadii[2]);
            }

            if (rectangleCornerRadii[4] == 0f)
            {
                path.LineTo(rectangleRect.Right, rectangleRect.Bottom);
            }
            else
            {
                path.ArcTo(rectangleRect.Right, rectangleRect.Bottom, rectangleRect.Left, rectangleRect.Bottom, rectangleCornerRadii[4]);
            }

            if (rectangleCornerRadii[6] == 0f)
            {
                path.LineTo(rectangleRect.Left, rectangleRect.Bottom);
            }
            else
            {
                path.ArcTo(rectangleRect.Left, rectangleRect.Bottom, rectangleRect.Left, rectangleRect.Top, rectangleCornerRadii[6]);
            }
        }

        public static void Reset(this SKPaint paint)
        {
            paint.BlendMode = SKBlendMode.SrcOver;
        }

        public static void mapPoints(this SKMatrix matrix, float[] points)
        {
            var newPoints = matrix.MapPoints(new SKPoint[] { new SKPoint(points[0], points[1]), new SKPoint(points[2], points[3]) });

            points[0] = newPoints[0].X;
            points[1] = newPoints[0].Y;
            points[2] = newPoints[1].X;
            points[3] = newPoints[1].Y;
        }
    }

    public class PaintCodeStaticLayout : IDisposable
    {
        private StaticLayout layout;
        private int width;
        private SKTextAlign alignment;
        private string source;
        private SKPaint paint;

        public StaticLayout get(int width, SKTextAlign alignment, string source, SKPaint paint)
        {
            if (this.layout == null || this.width != width || this.alignment != alignment || !this.source.Equals(source) || !this.paint.Equals(paint))
            {
                if (this.layout != null)
                {
                    this.layout.Dispose();
                }

                this.width = width;
                this.alignment = alignment;
                this.source = source;
                this.paint = paint;
                this.layout = new StaticLayout(source, paint, width, alignment);
            }
            return this.layout;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.layout != null)
                        this.layout.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }

    public class PaintCodeRadialGradient : IDisposable
    {
        private SKShader shader;
        private PaintCodeGradient paintCodeGradient;
        private float x0, y0, x1, y1, radius0, radius1;
        public SKShader get(PaintCodeGradient paintCodeGradient, float x0, float y0, float radius0, float x1, float y1, float radius1)
        {
            if (this.shader == null || this.x0 != x0 || this.y0 != y0 || this.radius0 != radius0 || this.x1 != x1 || this.y1 != y1 || this.radius1 != radius1 || !this.paintCodeGradient.Equals(paintCodeGradient))
            {
                if (this.shader != null)
                {
                    this.shader.Dispose();
                }

                this.x0 = x0;
                this.y0 = y0;
                this.radius0 = radius0;
                this.x1 = x1;
                this.y1 = y1;
                this.radius1 = radius1;
                this.paintCodeGradient = paintCodeGradient;
                this.shader = paintCodeGradient.radialGradient(x0, y0, radius0, x1, y1, radius1);
            }
            return this.shader;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.shader != null)
                    {
                        this.shader.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }


    public class PaintCodeLinearGradient : IDisposable
    {
        private SKShader shader;
        private PaintCodeGradient paintCodeGradient;
        private float x0, y0, x1, y1;
        public SKShader get(PaintCodeGradient paintCodeGradient, float x0, float y0, float x1, float y1)
        {
            if (this.shader == null || this.x0 != x0 || this.y0 != y0 || this.x1 != x1 || this.y1 != y1 || !this.paintCodeGradient.Equals(paintCodeGradient))
            {
                if (this.shader != null)
                {
                    this.shader.Dispose();
                }

                this.x0 = x0;
                this.y0 = y0;
                this.x1 = x1;
                this.y1 = y1;
                this.paintCodeGradient = paintCodeGradient;
                this.shader = paintCodeGradient.linearGradient(x0, y0, x1, y1);
            }
            return this.shader;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.shader != null)
                    {
                        this.shader.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }

    public class PaintCodeGradient
    {
        private SKColor[] colors;
        private float[] positions;

        public PaintCodeGradient(SKColor[] colors, float[] positions)
        {
            if (positions == null)
            {
                int steps = colors.Length;
                positions = new float[steps];
                for (int i = 0; i < steps; i++)
                    positions[i] = (float)i / (steps - 1);
            }

            this.colors = colors;
            this.positions = positions;
        }

        public SKShader linearGradient(float x0, float y0, float x1, float y1)
        {
            return SKShader.CreateLinearGradient(new SKPoint(x0, y0), new SKPoint(x1, y1), this.colors, this.positions, SKShaderTileMode.Clamp);
        }

        public SKShader radialGradient(float startX, float startY, float startRadius, float endX, float endY, float endRadius)
        {
            int steps = this.colors.Length;
            float[] positions = new float[steps];

            if (startRadius > endRadius)
            {
                float ratio = endRadius / startRadius;
                SKColor[] colors = new SKColor[steps];

                for (int i = 0; i < steps; i++)
                {
                    colors[i] = this.colors[steps - i - 1];
                    positions[i] = (1 - this.positions[steps - i - 1]) * (1 - ratio) + ratio;
                }

                return SKShader.CreateRadialGradient(new SKPoint(endX, endY), startRadius, this.colors, this.positions, SKShaderTileMode.Clamp);
            }
            else
            {
                float ratio = startRadius / endRadius;

                for (int i = 0; i < steps; i++)
                {
                    positions[i] = this.positions[i] * (1 - ratio) + ratio;
                }

                return SKShader.CreateRadialGradient(new SKPoint(startX, startY), endRadius, this.colors, positions, SKShaderTileMode.Clamp);
            }
        }

        public override bool Equals(Object obj)
        {
            if (!(obj == this))
                return false;

            PaintCodeGradient other = (PaintCodeGradient)obj;
            return Array.Equals(this.colors, other.colors) && Array.Equals(this.positions, other.positions);
        }
    }

    public class PaintCodeColor
    {
        //internal static float[] ColorToHSV(int originalColor)
        //{
        //    float[] hsv = new float[3];
        //    SKColor.RGBToHSV(Color.GetRedComponent(originalColor), Color.GetGreenComponent(originalColor), Color.GetBlueComponent(originalColor), hsv);
        //    return hsv;
        //}

        //public static int colorByChangingHue(int originalColor, float newHue)
        //{
        //    float[] hsv = ColorToHSV(originalColor);
        //    hsv[0] = newHue;
        //    return Color.HSVToColor(Color.GetAlphaComponent(originalColor), hsv);
        //}

        public static SKColor colorByChangingSaturation(SKColor originalColor, float newSaturation)
        {
            float h, s, v;
            originalColor.ToHsv(out h, out s, out v);
            return SKColor.FromHsv(h, newSaturation, v);
        }

        //public static int colorByChangingValue(int originalColor, float newValue)
        //{
        //    float[] hsv = ColorToHSV(originalColor);
        //    hsv[2] = newValue;
        //    return Color.HSVToColor(Color.GetAlphaComponent(originalColor), hsv);
        //}

        //public static float Hue(int color)
        //{
        //    return ColorToHSV(color)[0];
        //}

        //public static float Saturation(int color)
        //{
        //    return ColorToHSV(color)[1];
        //}

        //public static float Brightness(int color)
        //{
        //    return ColorToHSV(color)[2];
        //}

        public static SKColor colorByChangingAlpha(SKColor color, byte newAlpha)
        {
            return new SKColor(newAlpha, color.Red, color.Green, color.Blue);
        }

        public static SKColor colorByBlendingColors(SKColor c1, float ratio, SKColor c2)
        {
            return new SKColor((byte)((1f - ratio) * c1.Red + ratio * c2.Red),
                (byte)((1f - ratio) * c1.Green + ratio * c2.Green),
                (byte)((1f - ratio) * c1.Blue + ratio * c2.Blue), (byte)((1f - ratio) * c1.Alpha + ratio * c2.Alpha));
        }

        public static SKColor colorByApplyingHighlight(SKColor color, float ratio)
        {
            return colorByBlendingColors(color, ratio, colorByChangingAlpha(Helpers.ColorWhite, color.Alpha));
        }

        //public static int colorByApplyingShadow(int color, float ratio)
        //{
        //    return colorByBlendingColors(color, ratio, colorByChangingAlpha(Helpers.ColorBlack, Color.GetAlphaComponent(color)));
        //}
    }
}