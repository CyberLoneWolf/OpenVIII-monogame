﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace OpenVIII.IGMDataItem
{
    public class Texture : Base, I_Data<Texture2D>, I_Color
    {
        #region Constructors

        public Texture()
        {
            Color = Color.White;
            Faded_Color = Color;
            Blink_Adjustment = 1f;
        }

        #endregion Constructors

        #region Properties

        public override bool Blink { get => base.Blink && (Color != Faded_Color); set => base.Blink = value; }
        public Texture2D Data { get; set; }
        public Rectangle Restriction { get; set; }

        #endregion Properties

        #region Methods

        public override void Draw()
        {
            if (Enabled)
            {
                var p = Pos;
                var src = new Rectangle(0, 0, Data.Width, Data.Height);
                if (!Restriction.IsEmpty)
                {
                    p = Rectangle.Intersect(p, Restriction);
                    if (p != Pos)
                    {
                        var missing = new Rectangle(
                            Math.Abs(p.X - Pos.X),
                            Math.Abs(p.Y - Pos.Y),
                            Pos.Width - p.Width,
                            Pos.Height - p.Height
                            );

                        var scale = new Vector2(
                            (float)Width / Data.Width,
                            (float)Height / Data.Height);
                        var ploc = (src.Location.ToVector2() * scale + missing.Location.ToVector2()) / scale;
                        var pSize = (src.Size.ToVector2() * scale - missing.Size.ToVector2()) / scale;

                        src.Location = ploc.ToPoint();
                        src.Size = pSize.ToPoint();
                    }
                }
                if (!Blink)
                    Memory.SpriteBatch.Draw(Data, p, src, Color * Fade);
                else
                    Memory.SpriteBatch.Draw(Data, p, src, Color.Lerp(Color, Faded_Color, Menu.Blink_Amount) * Blink_Adjustment * Fade);
                // if (Blink) Memory.spriteBatch.Draw(Data, Pos, null, Faded_Color * Fade *
                // Blink_Amount * Blink_Adjustment);
            }
        }

        #endregion Methods
    }
}