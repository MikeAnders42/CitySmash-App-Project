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
    /// <summary>
    /// A rowhouse
    /// </summary>
    public class Building: GameObject
    {
        #region Fields

        // sprite change support
        private Texture2D ruinSprite;
        Rectangle collisionRectangle;
        bool facingUp = false;

        // starting dimensions
        private int startHeight = 80;
        private int startWidth = 20;

        // damage tracking support
        private int damageTaken = 0;
        private bool animationAdded = false;
        private bool hit = false;
        private bool exploded = false;

        // support for spawning destruction effects
        ContentManager Content;
        Rectangle holdDrawRectangle;
        int frameCounter0 = 0;
        int framecounter1 = 0;
        int frameCounter2 = 0;
        int elapsedSmokeTime = 0;
        int elapsedExplosionTimer = 0;
        const int totalSmokeTime = 10000;
        int totalExplosionTime = 2000;
        int elapsedTimeSinceHit = 0;
        const int timeSinceHitLimit = 200;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Content">content manager for loading sprites</param>
        /// <param name="sprite">sprite for the rowhouse</param>
        /// <param name="location">location of the start of the rowhouse</param>
        /// <param name="facingUp">true if the building is facing up</param>
        public Building(ContentManager Content, Texture2D sprite, Vector2 location, bool facingUp)
               : base(sprite, location)
        {                        
            this.Content = Content;
            this.facingUp = facingUp;
            DrawListOffset = 350;

            // set starting location            
            if (this.facingUp) 
            {
                Location = new Vector2(location.X, location.Y - (startHeight/2));
            } else { Location = location; }

            // load in main sprites
            ruinSprite = Content.Load<Texture2D>("Graphics/Buildings/RowhouseRuin" + Rand.rand.Next(0, 3));
            if (this.facingUp)
            {                
                Sprite = Content.Load<Texture2D>(sprite.Name + "Back");
            } else { Sprite = sprite; }

            // set draw and collision rectangles so rowhouse is adjacent to location
            if (sprite.Name.Contains("Double"))
            {
                startWidth = 40;
            }
            DrawRectangle = new Rectangle((int)(Location.X), (int)(Location.Y), startWidth, startHeight);
        }

        #endregion

        #region Properties

        /// <summary>
        /// calculate and return collision rectangle
        /// </summary>
        public Rectangle CollisionRectangle
        {
            get
            {
                if (damageTaken < 1)
                {
                    if (facingUp)
                    {
                        collisionRectangle = new Rectangle(DrawRectangle.Left, DrawRectangle.Y + (startHeight * 3 / 12),
                                           Width, startHeight * 5 / 12);
                    } else {
                        collisionRectangle = new Rectangle(DrawRectangle.Left, DrawRectangle.Y + (startHeight * 5 / 12),
                                           Width, startHeight * 5 / 12);
                    }
                }
                return collisionRectangle;
            }
        }

        /// <summary>
        /// The damage taken by the rowhouse, set automatically adds 1
        /// </summary>
        public int DamageTaken
        {
            get { return damageTaken; }
            set
            {
                if (damageTaken < 4)
                {
                    damageTaken = value;
                    animationAdded = false;
                    hit = true;
                }
            }
        }

        public bool Exploded
        {
            get { return exploded; }
        }

        public bool DamageResolved { get { return !animationAdded; } }

        public bool Hit { get { return hit; } }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the rowhouse to add destruction effects
        /// </summary>
        public void DestructionUpdate(GameTime gameTime)
        {
            if (hit)
            {
                if (elapsedTimeSinceHit < timeSinceHitLimit)
                {
                    elapsedTimeSinceHit += gameTime.ElapsedGameTime.Milliseconds;
                } else {
                    hit = false;
                    elapsedTimeSinceHit = 0;
                }
            }

            // add desruction effects
            if (!exploded && damageTaken > 0 && DestructionEffects.Count() < 1 && !animationAdded)
            {
                // on 1 damage spawn some smoke
                Vector2 spawnLoacation0 = new Vector2(DrawRectangle.X + Rand.rand.Next(5, DrawRectangle.Width - 10), DrawRectangle.Y + Rand.rand.Next(5, DrawRectangle.Height - 10));
                DestructionEffects.Add(new AnimatedElement(Content, spawnLoacation0, AnimatedElement.AnimationType.Smoke0));
                animationAdded = true;
            }
            if (!exploded && damageTaken < 3 && DestructionEffects.Count() > 0 && !animationAdded)
            {
                // on 2 damage spawn fires and more smoke
                Vector2 spawnLocation1 = new Vector2(DestructionEffects[0].Location.X, DestructionEffects[0].Location.Y + 5);
                DestructionEffects.Add(new AnimatedElement(Content, spawnLocation1, AnimatedElement.AnimationType.Fire));
                Vector2 spawnLocation2 = new Vector2(DrawRectangle.X + Rand.rand.Next(5, DrawRectangle.Width - 10), DrawRectangle.Y + Rand.rand.Next(5, DrawRectangle.Height - 10));
                DestructionEffects.Add(new AnimatedElement(Content, spawnLocation2, AnimatedElement.AnimationType.Smoke0));
                // check to make sure effects do not overlap
                while (DestructionEffects.Last().EffectOcclusionRectangle.Intersects(DestructionEffects[0].EffectOcclusionRectangle))
                {
                    // check if the first effect is closer to the top or bottom then move the second effect closer to the far side
                    if (Math.Abs(DrawRectangle.Top - DestructionEffects[0].EffectOcclusionRectangle.Top) > Math.Abs(DrawRectangle.Bottom - DestructionEffects[0].EffectOcclusionRectangle.Bottom))
                    {
                        DestructionEffects.Last().Y -= 5;
                    } else { DestructionEffects.Last().Y += 5; }
                }
                animationAdded = true;
            }
            if (damageTaken > 2 && !animationAdded)
            {
                // clear out smoke and fire
                if (DestructionEffects.Count() > 0 && DestructionEffects.Last().Type == AnimatedElement.AnimationType.Smoke0)
                {
                    foreach (AnimatedElement elementx in DestructionEffects)
                    {
                        elementx.IsFinished = true;
                    }
                }

                // if building is destroyed switch to a ruin sprite, remove collision, and add smoke effects for 30 seconds
                if (exploded)
                {
                    Sprite = ruinSprite;
                    collisionRectangle = new Rectangle(0, 0, 0, 0);
                    DrawRectangle = holdDrawRectangle;
                    // add smoke effects for 30 seconds
                    if (elapsedSmokeTime < totalSmokeTime)
                    {
                        elapsedSmokeTime += gameTime.ElapsedGameTime.Milliseconds;
                        if (elapsedSmokeTime > 1000 * frameCounter0)
                        {
                            foreach (AnimatedElement elementx in DestructionEffects)
                            {
                                elementx.IsFinished = true;
                            }
                            Vector2 spawnLocation3 = new Vector2(DrawRectangle.X + Rand.rand.Next(5, DrawRectangle.Width - 10), DrawRectangle.Y + Rand.rand.Next(5, Math.Abs(DrawRectangle.Height) - 10));
                            DestructionEffects.Add(new AnimatedElement(Content, spawnLocation3, AnimatedElement.AnimationType.Smoke1));
                            frameCounter0++;
                        }
                    } else {
                        foreach (AnimatedElement elementx in DestructionEffects)
                        {
                            elementx.IsFinished = true;
                        }
                        animationAdded = true;
                    }
                } else {
                    // start building destruction animation
                    if (elapsedExplosionTimer < totalExplosionTime)
                    {
                        // add random explosions to start destruction
                        elapsedExplosionTimer += gameTime.ElapsedGameTime.Milliseconds;
                        if (elapsedExplosionTimer > 200 * framecounter1 && elapsedExplosionTimer < 601)
                        {
                            Vector2 spawnLocation4 = new Vector2(DrawRectangle.X + Rand.rand.Next(5, DrawRectangle.Width - 10), DrawRectangle.Y + Rand.rand.Next(5, DrawRectangle.Height - 10));
                            DestructionEffects.Add(new AnimatedElement(Content, spawnLocation4, AnimatedElement.AnimationType.Explosion0));
                            holdDrawRectangle = DrawRectangle;
                            framecounter1++;
                        }
                        // sink building and add explosions
                        if (elapsedExplosionTimer > 601)
                        {
                            if (elapsedExplosionTimer > 600 + (200 * frameCounter2))
                            {
                                // add explosions to side of building
                                if (10 * (frameCounter2 % 4) == 0)
                                {
                                    Vector2 spawnLocation7 = new Vector2(DrawRectangle.X + (Width / 2), DrawRectangle.Bottom);
                                    DestructionEffects.Add(new AnimatedElement(Content, spawnLocation7, AnimatedElement.AnimationType.Explosion0));
                                }  else {
                                    Vector2 spawnLocation5 = new Vector2(DrawRectangle.X, DrawRectangle.Bottom - (10 * (frameCounter2 % 4)));
                                    Vector2 spawnLocation6 = new Vector2(DrawRectangle.Right, DrawRectangle.Bottom - (10 * (frameCounter2 % 4)));
                                    DestructionEffects.Add(new AnimatedElement(Content, spawnLocation5, AnimatedElement.AnimationType.Explosion0));
                                    DestructionEffects.Add(new AnimatedElement(Content, spawnLocation6, AnimatedElement.AnimationType.Explosion0));
                                }
                                // sink building
                                Height = Height - 5;
                                Y = Y + 5;
                                frameCounter2++;
                            }
                        }
                    } else {
                        // if building is destroyed set exploded property to true
                        exploded = true;
                        DrawListOffset = 50;
                    }
                }
            }


            for (int i = DestructionEffects.Count() - 1; i >= 0; i --)
            {
                if (DestructionEffects[i].IsFinished)
                {
                    DestructionEffects.RemoveAt(i);
                } else { DestructionEffects[i].Update(gameTime); }
            }
        }

        ///// <summary>
        ///// Override the base draw method, so that the animation sprites can be updated on call with the building
        ///// </summary>
        ///// <param name="spriteBatch">The spritebatch to draw with</param>
        //public override void Draw(SpriteBatch spriteBatch)
        //{
        //    if (!IsRotated)
        //    {
        //        //spriteBatch.Draw(Sprite, RotationDrawRectangle(drawRectangle, rotation), null, Color.White * opacity, rotation, new Vector2(0, 0), SpriteEffects.None, 0f);
        //    }
        //    else
        //    {
        //        spriteBatch.Draw(Sprite, DrawRectangle, Color.White * Opacity);
        //    }

        //    for (int i = 0; i < destructionEffects.Count(); i++)
        //    {
        //        destructionEffects[i].Draw(spriteBatch);
        //    }
        //}
        
        #endregion

    }
}
