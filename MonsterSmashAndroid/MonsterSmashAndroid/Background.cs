using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitySmash
{
    class Background
    {

        #region Fields

        private Rectangle drawRectangle;
        private Texture2D sprite;
        private bool facingUp;

        #endregion

        #region Constructors

        /// <summary>
        /// create a new backgound sprite at the given location
        /// </summary>
        /// <param name="drawRectangle">The location to draw the sprite onto</param>
        /// <param name="sprite">The sprite to draw at the given location</param>
        public Background (Rectangle drawRectangle, Texture2D sprite, bool facingUp)
        {
            this.sprite = sprite;
            this.drawRectangle = drawRectangle;
            this.facingUp = facingUp;

            if (facingUp)
            {
                
            }
        }

        #endregion 

        #region Public Methods

        /// <summary>
        /// Draw the background texture
        /// </summary>
        /// <param name="spriteBatch">The spritebatch to draw with</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (facingUp)
            {
                spriteBatch.Draw(sprite, drawRectangle, null, Color.White, 0, new Vector2(), SpriteEffects.FlipVertically, 0);
            } else {
                spriteBatch.Draw(sprite, drawRectangle, Color.White);
            }
        }

        #endregion

    }
}
