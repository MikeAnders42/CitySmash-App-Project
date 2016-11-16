using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;


namespace CitySmash
{
    public class GameObject : IComparable<GameObject>
    {
        #region Fields

        // map generation support
        private bool leftBuilt = true;
        private bool rightBuilt = true;
        private bool bottomBuilt = true;

        // drawing support
        private Texture2D sprite;
        private Rectangle drawRectangle;
        private Vector2 location;
        private float rotation = 0f;
        private bool isRotated = true;
        Rectangle newDrawRectangle;

        // occlusion and drawdepth support
        private int drawListOffset;
        private int drawOrder;
        private float opacity = 1;
        private float opacityStrength = 1;
        private bool isOverlapping = false;
        private int elapsedGametime = 10000;
        private int fadeTimer = 10000;

        // directional support
        private bool flipSpriteH = false;
        private bool flipSpriteV = false;
        public enum Turn { Right, Left, Straight };
        private Turn turnDirection = Turn.Straight;

        // destruction support
        private List<AnimatedElement> destructionEffects = new List<AnimatedElement>();

        #endregion

        #region Comparators

        public int CompareTo(GameObject other)
        {
            // handle invalid inputs
            if (other == null) return 1;

            // otherwise compare the draworder
            return drawOrder.CompareTo(other.drawOrder);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Create object centered on a given location
        /// </summary>
        /// <param name="sprite">image to use for object</param>
        /// <param name="location">location to center image on</param>
        public GameObject(Texture2D sprite, Vector2 location)
        {
            this.sprite = sprite;
            this.location = location;
            Active = true;

            // set draw and collision rectangles so rowhouse is centered on location
            drawRectangle = new Rectangle((int)(location.X) - (sprite.Width / 2), (int)(location.Y) - (sprite.Height / 2), sprite.Width, sprite.Height);
        }

        public GameObject(ContentManager Content, String spritename, Vector2 location)
        {
            Active = true;

            //clean spritename
            if (spritename.Contains("Graphics/"))
            {
                sprite = Content.Load<Texture2D>(spritename);
            } else {
                this.sprite = Content.Load<Texture2D>("Graphics/" + spritename);
            }

            // set draw and collision rectangles so rowhouse is centered on location
            drawRectangle = new Rectangle((int)(location.X) - (sprite.Width / 2), (int)(location.Y) - (sprite.Height / 2), sprite.Width, sprite.Height);
        }

        #endregion

        #region Properties
        /// <summary>
        /// get or set opacity effect strength
        /// </summary>
        public float OpacityStrength
        {
            get { return opacityStrength; }
            set { opacityStrength = value; }
        }

        /// <summary>
        /// get or set drawlist offset
        /// </summary>
        public int DrawListOffset
        {
            get { return drawListOffset; }
            set { drawListOffset = value; }
        }

        /// <summary>
        /// get or set draw depth (1-100: background, XX01-xx48 obstacles, XX49 monster, XX50- XY00: buildings)
        /// </summary>
        public int Depth
        {
            get { return drawOrder; }
            set { drawOrder = value; }
        }

        /// <summary>
        /// Get or set the sprite
        /// </summary>
        public Texture2D Sprite
        {
            get { return sprite; }
            set { sprite = value; }
        }

        /// <summary>
        /// Get or set the drawrectangle
        /// </summary>
        public Rectangle DrawRectangle
        {
            get { return drawRectangle; }
            set { drawRectangle = value; }
        }

        /// <summary>
        /// Get or set the location (default is the center of the object)
        /// </summary>
        public Vector2 Location
        {
            get { return location; }
            set { location = value; }
        }

        public float Opacity
        {
            get { return opacity; }
            set { opacity = value; }
        }

        public bool IsOverlapping
        {
            get { return isOverlapping; }
            set { isOverlapping = value;
                elapsedGametime = 0;
            }
        }

        public bool LeftSideBuilt
        {
            get { return leftBuilt; }
            set { leftBuilt = value; }
        }

        public bool RightSideBuilt
        {
            get { return rightBuilt; }
            set { rightBuilt = value; }
        }

        public bool BottomBuilt
        {
            get { return bottomBuilt; }
            set { bottomBuilt = value; }
        }

        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        public bool IsRotated
        {
            get { return isRotated; }
            set { isRotated = value; }
        }

        public int Width
        {
            get { return drawRectangle.Width; }
            set { drawRectangle.Width = value; }
        }

        public int Height
        {
            get { return drawRectangle.Height; }
            set { drawRectangle.Height = value; }
        }

        public int X
        {
            get { return drawRectangle.X; }
            set { drawRectangle.X = value; }
        }

        public int Y
        {
            get { return drawRectangle.Y; }
            set { drawRectangle.Y = value; }
        }

        public bool FlipSpriteH
        {
            get { return flipSpriteH; }
            set { flipSpriteH = value; }
        }

        public bool FlipSpriteV
        {
            get { return flipSpriteV; }
            set { flipSpriteV = value; }
        }

        public bool FlipRotate
        {
            get; set;
        }

        public Rectangle NewDrawRectangle
        {
            get { return newDrawRectangle; }
        }

        public Turn TurnDirection
        {
            get { return turnDirection; }
            set { turnDirection = value; }
        }

        public float TurnRadius
        {
            get; set;
        }

        public float RadiansTurned
        {
            get; set;
        }

        public float TurnStartAngle
        {
            get; set;
        }

        public int XOffsetMultiplier
        {
            get; set;
        }

        public List<AnimatedElement> DestructionEffects
        {
            get { return destructionEffects; }
            set { destructionEffects = value; }
        }

        public bool Active;

        #endregion

        #region Public methods

        /// <summary>
        /// Draws the object
        /// </summary>
        /// <param name="spriteBatch">spritebatch</param>
        virtual public void Draw(SpriteBatch spriteBatch)
        {
            if (Active)
            {
                // use the sprite batch to draw the object
                if (!isRotated)
                {
                    spriteBatch.Draw(sprite, RotationDrawRectangle(drawRectangle, rotation), null, Color.White * opacity, rotation, new Vector2(0, 0), SpriteEffects.None, 0f);
                }
                else if (flipSpriteH)
                {
                    spriteBatch.Draw(Sprite, RotationDrawRectangle(drawRectangle, rotation), null, Color.White, rotation, new Vector2(0, 0), SpriteEffects.FlipHorizontally, 0f);
                }
                else if (flipSpriteV)
                {
                    spriteBatch.Draw(Sprite, DrawRectangle, null, Color.White, 0, new Vector2(), SpriteEffects.FlipVertically, 0);
                }
                else if (FlipRotate)
                {
                    spriteBatch.Draw(sprite, RotationDrawRectangle(drawRectangle, rotation), null, Color.White, rotation, new Vector2(0, 0), SpriteEffects.FlipVertically, 0f);
                }
                else
                {
                    spriteBatch.Draw(Sprite, DrawRectangle, Color.White * opacity);
                }

                foreach(AnimatedElement elementx in destructionEffects)
                {
                    elementx.Draw(spriteBatch);                }
            }
        }

        /// <summary>
        /// Updates the GameObject
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime, ref GameObject[] drawList)
        {
            if (Active)
            {
                // update occlusion based on timer
                if (elapsedGametime < fadeTimer)
                {
                    elapsedGametime += gameTime.ElapsedGameTime.Milliseconds;
                    if (IsOverlapping)
                    {
                        opacity += opacityStrength * (.4f - opacity) / ((fadeTimer - elapsedGametime) / 1000);
                    } else {
                        opacity += opacityStrength * (1f - opacity) / ((fadeTimer - elapsedGametime) / 1000);
                    }
                }
            }

            // update draw order based on current position
            drawList[Depth] = null;
            if (Active)
            {
                drawOrder = (int)(drawRectangle.Y / 0.8) + drawListOffset;
                while (drawList[drawOrder] != null)
                {
                    drawOrder += 1;
                }
                drawList[drawOrder] = this;
            }

        }

        #endregion

        #region Private Methods

        private Rectangle RotationDrawRectangle(Rectangle drawRectangle, float rotation)
        {
            newDrawRectangle = drawRectangle;
            float radius;
                        
            radius = (float)Math.Sqrt(Math.Pow(drawRectangle.Width / 2, 2) + Math.Pow(drawRectangle.Height / 2, 2));
            newDrawRectangle.Y = drawRectangle.Center.Y - (int)(radius * (float)Math.Cos(rotation - Math.PI / 4));
            newDrawRectangle.X = drawRectangle.Center.X + (int)(radius * (float)Math.Sin(rotation - Math.PI / 4));
            if (TurnRadius != 0)
            {
                int yOffset = (int)((TurnRadius * Math.Cos(TurnStartAngle)) - (TurnRadius * Math.Cos(TurnStartAngle + RadiansTurned)));
                int xOffset = (int)(((TurnRadius) * Math.Sin(TurnStartAngle)) - ((TurnRadius) * Math.Sin(TurnStartAngle + RadiansTurned)));
                newDrawRectangle.Y = newDrawRectangle.Y - yOffset;
                newDrawRectangle.X = newDrawRectangle.X + (int)(xOffset) + 1;
            }


            return newDrawRectangle;
        }

        #endregion
    }
}
