using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;

namespace MGUI.Core.UI.Brushes.Border_Brushes
{
    /// <summary>See also:<br/><see cref="MGUniformBorderBrush"/><br/><see cref="MGDockedBorderBrush"/><br/><see cref="MGBandedBorderBrush"/><br/><see cref="MGTexturedBorderBrush"/></summary>
    public interface IBorderBrush : ICloneable
    {
        public void Draw(ElementDrawArgs DA, MGElement Element, Rectangle Bounds, Thickness BT);

        public IBorderBrush Copy();
        object ICloneable.Clone() => Copy();
    }
}
