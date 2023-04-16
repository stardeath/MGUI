﻿using DrawingColor = System.Drawing.Color;
using XNAColor = Microsoft.Xna.Framework.Color;

namespace MGUI.Shared.Helpers
{
    public static class ColorUtils
    {
        public static XNAColor Darken(this XNAColor @this, float shadowIntensity) => XNAColor.Lerp(@this, XNAColor.Black, shadowIntensity);
        public static XNAColor Brighten(this XNAColor @this, float brightenIntensity) => XNAColor.Lerp(@this, XNAColor.White, brightenIntensity);
        public static int PackedIntValue(this XNAColor @this) => unchecked((int)@this.PackedValue);

        public static DrawingColor AsDrawingColor(this XNAColor @this) => DrawingColor.FromArgb(@this.A, @this.R, @this.G, @this.B);
        public static XNAColor AsXNAColor(this DrawingColor @this) => new(@this.R, @this.G, @this.B, @this.A);
    }
}