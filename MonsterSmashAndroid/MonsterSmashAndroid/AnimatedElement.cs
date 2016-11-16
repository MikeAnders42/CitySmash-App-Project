using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitySmash
{
    public class AnimatedElement
    {

        #region Fields

        // drawing dupport
        private Vector2 location;
        private Texture2D sprite;
        private Rectangle drawRectangle;
        private Rectangle effectOcclusionRectangle;
        private Rectangle sourceRectangle;

        // animation support
        private int elapsedGameTime = 0;
        private int animationTimeLimit = 1000;
        private int frameCounter = 0;
        private int iterations = 0;
        private int frameWidth;
        private int framesPerRow;
        private int xReset = 0;

        // type support
        public enum AnimationType { Explosion0, Explosion1, Smoke0, Smoke1, Fire, Ripple }
        private AnimationType type;

        // deletion support
        private bool isFinished;

        int rippleDamage = 1;
        public event Monster0.MovementCollisionRectangle StepCollisionRectangleEvent;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new animated element
        /// </summary>
        /// <param name="content">The content manager to load the sprite</param>
        /// <param name="location">The location of the center of the element</param>
        /// <param name="location">What type of element to use</param>
        public AnimatedElement(ContentManager content, Vector2 location, AnimationType type)
        {
            this.type = type;
            this.location = location;
            eventThreaded = false;
            switch (type)
            {
                case AnimationType.Explosion0:
                    frameWidth = 140;
                    framesPerRow = 4;
                    iterations = 16;
                    sprite = content.Load<Texture2D>("Graphics/SpriteStrips/Explosion");
                    drawRectangle = new Rectangle((int)location.X - 20, (int)location.Y - 20, 40, 40);
                    sourceRectangle = new Rectangle(0, 0, frameWidth, frameWidth);                    
                    break;
                case AnimationType.Smoke0:
                    framesPerRow = 4;
                    frameWidth = 140;
                    iterations = 16;
                    sprite = content.Load<Texture2D>("Graphics/SpriteStrips/Smoke0");
                    drawRectangle = new Rectangle((int)location.X - 20, (int)location.Y - 20, 40, 40);
                    sourceRectangle = new Rectangle(0, 0, frameWidth, frameWidth);                    
                    break;
                case AnimationType.Smoke1:
                    framesPerRow = 4;
                    frameWidth = 140;
                    iterations = 16;
                    sprite = content.Load<Texture2D>("Graphics/SpriteStrips/Smoke1");
                    drawRectangle = new Rectangle((int)location.X - 20, (int)location.Y - 20, 40, 40);
                    sourceRectangle = new Rectangle(0, 0, frameWidth, frameWidth);
                    break;
                case AnimationType.Ripple:
                    animationTimeLimit = 500;
                    framesPerRow = 4;
                    frameWidth = 140;
                    iterations = 8;
                    sprite = content.Load<Texture2D>("Graphics/SpriteStrips/Ripple");
                    drawRectangle = new Rectangle((int)location.X - 25, (int)location.Y - 25, 60, 60);
                    sourceRectangle = new Rectangle(0, 0, frameWidth, frameWidth);
                    break;
                case AnimationType.Explosion1:
                case AnimationType.Fire:
                    animationTimeLimit = 500;
                    frameWidth = 72;
                    framesPerRow = 6;
                    iterations = 6;
                    xReset = 2;
                    if (type == AnimationType.Fire)
                    {
                        sprite = content.Load<Texture2D>("Graphics/SpriteStrips/Fire");
                    } else {
                        sprite = content.Load<Texture2D>("Graphics/SpriteStrips/ExplosionPow");
                        animationTimeLimit = 250;
                    }
                    drawRectangle = new Rectangle((int)location.X - 20, (int)location.Y - 20, 40, 40);
                    sourceRectangle = new Rectangle(0, 0, 72, 72);
                    break;                    
            }
            effectOcclusionRectangle = new Rectangle((int)location.X, drawRectangle.Bottom - 20, 20, 20);
        }

        #endregion

        #region Properties

        public bool IsFinished
        {
            get { return isFinished; }
            set { isFinished = value; }
        }

        public Vector2 Location
        {
            get { return location; }
            set { location = value; }
        }

        public Rectangle EffectOcclusionRectangle
        {
            get { return effectOcclusionRectangle; }
        }

        public int Y
        {
            get { return drawRectangle.Y; }
            set
            {
                drawRectangle.Y = value;
                effectOcclusionRectangle.Y = drawRectangle.Bottom - 20;
            }
        }

        public AnimationType Type
        {
            get { return type; }
        }

        public bool eventThreaded { get; set; }

        #endregion

        #region Public Methods

        public void Update(GameTime gameTime)
        {
            if (type == AnimationType.Ripple)
            {
                OnStep();
            }

            if (elapsedGameTime < animationTimeLimit)
            {
                elapsedGameTime += gameTime.ElapsedGameTime.Milliseconds;
                if (elapsedGameTime > frameCounter * (animationTimeLimit / iterations))
                {
                    if (sourceRectangle.X > ((frameWidth * (framesPerRow - 1)) - 10))
                    {
                        sourceRectangle.X = (xReset & frameWidth);
                        if (sourceRectangle.Y + frameWidth < sprite.Height)
                        {
                            sourceRectangle.Y += frameWidth;
                        }
                    } else {
                        sourceRectangle.X += frameWidth;
                    }
                    frameCounter++;
                }
            } else {
                if (type == AnimationType.Explosion0 || type == AnimationType.Explosion1 || type == AnimationType.Ripple)
                {
                    isFinished = true;
                } else {
                    elapsedGameTime = 0;
                    frameCounter = 0;
                    sourceRectangle.X = (xReset & frameWidth);
                    sourceRectangle.Y = 0;
                }
            }
        }

        /// <summary>
        /// Draw the explosion
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!isFinished)
            {
                spriteBatch.Draw(sprite, drawRectangle, sourceRectangle, Color.White);
            }
        }

        #endregion

        #region Private Methods

        protected virtual Rectangle OnStep()
        {
            Rectangle emptyRectangle = new Rectangle();
            if (StepCollisionRectangleEvent != null)
            {
                Rectangle collisionRectangle = new Rectangle(drawRectangle.Center.X - ((drawRectangle.Width * (frameCounter / 8))/2),
                    drawRectangle.Center.Y - ((drawRectangle.Height * (frameCounter / 8))/2),
                    drawRectangle.Width * (frameCounter / 8), drawRectangle.Height * (frameCounter / 8));
                return StepCollisionRectangleEvent(collisionRectangle, drawRectangle, rippleDamage);
            }
            return emptyRectangle;
        }

        #endregion
    }
}
