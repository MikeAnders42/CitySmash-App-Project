using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace CitySmash
{

    public class Monster0: GameObject
    {

        #region Fields

        // movement support
        int prevYMove = 0;

        //drawing support
        private ContentManager content;
        private Texture2D[] bodyParts = new Texture2D[20];
        private GameObject body;
        private GameObject rightHand;
        private GameObject leftHand;
        private GameObject rightLeg;
        private GameObject leftLeg;
        List<GameObject> bodyPartDrawList = new List<GameObject>();

        // animation movement support
        private int stepCounter = 0;
        private int walkStepDistance = 1;
        private int walkStepHolder = 1;
        private int attackStepDistance = 1;
        private int holdValue = 0;
        private int walkFacingOffset = 1;
        private double windUpRadius = 0;
        private double startAngle = 0;
        private float startRotation = 0;
        private bool attackRotation = false;
        private Vector2[] startingPosition = new Vector2[5];
        private GameObject attackingHand;
        private GameObject steppingFoot;
      

        //state changing and timing support
        public enum MonsterAnimationState { Attacking, Walking, Resting };
        public enum MonsterFacing { Up, Down, Left, Right };
        private MonsterAnimationState animationState = MonsterAnimationState.Resting;
        private MonsterFacing facing = MonsterFacing.Down;
        private int elapsedAnimationTime = 0;
        private const int animationTimeLimit = 360;

        // attacking collision support (event, delegate, and spawn limiter)
        public delegate Rectangle MovementCollisionRectangle(Rectangle monsterLimbRectangle, Rectangle objectRectangle, int currentDamage);
        public event MovementCollisionRectangle AttackCollisionRectangleEvent;
        private bool explosionSpawned = false;

        // damage support
        private int basicAttackDmg = 2;

        #endregion

        #region Constructors

        public Monster0(ContentManager Content, Vector2 location, Texture2D sprite) : base(sprite, location)
        {
            this.Location = location;
            this.Sprite = sprite;
            this.content = Content;
            DrawListOffset = 349;

            // load in monster sprites
            bodyParts[0] = Content.Load<Texture2D>("Graphics/MonsterParts/PufferFront");
            bodyParts[1] = Content.Load<Texture2D>("Graphics/MonsterParts/PufferBack");
            bodyParts[2] = Content.Load<Texture2D>("Graphics/MonsterParts/FistFinFar");
            bodyParts[3] = Content.Load<Texture2D>("Graphics/MonsterParts/FistFinNear");
            bodyParts[4] = Content.Load<Texture2D>("Graphics/MonsterParts/LegFin");
            bodyParts[5] = Content.Load<Texture2D>("Graphics/MonsterParts/LegFinLeft");
            bodyParts[6] = Content.Load<Texture2D>("Graphics/MonsterParts/LeftLegFinBack");
            bodyParts[7] = Content.Load<Texture2D>("Graphics/MonsterParts/RightLegFinBack");
            bodyParts[8] = Content.Load<Texture2D>("Graphics/MonsterParts/FrontFin");
            bodyParts[9] = Content.Load<Texture2D>("Graphics/MonsterParts/LegFinLeftSideOn");
            bodyParts[10] = Content.Load<Texture2D>("Graphics/MonsterParts/PufferSide");

            //assign sprites to game objects

            DrawRectangle = new Rectangle((int)location.X, (int)location.Y, 80, 80);

            // load bodyparts and size them proportionately
            body = new GameObject(bodyParts[0], location);
            rightLeg = new GameObject(bodyParts[5], location);
            leftLeg = new GameObject(bodyParts[4], location);
            rightHand = new GameObject(bodyParts[2], location);
            leftHand = new GameObject(bodyParts[3], location);

            body.DrawRectangle = DrawRectangle;
            rightLeg.DrawRectangle = new Rectangle(DrawRectangle.Center.X - 45, DrawRectangle.Center.Y - 2, 55, 55);
            leftLeg.DrawRectangle = new Rectangle(DrawRectangle.Center.X - 10, DrawRectangle.Center.Y - 10, 55, 55);
            rightHand.DrawRectangle = new Rectangle(DrawRectangle.Center.X - 13, DrawRectangle.Y + 6, 80, 80);
            leftHand.DrawRectangle = new Rectangle(DrawRectangle.Left - 24, DrawRectangle.Y + 8, 80, 80);

            // set body parts' draw depth
            body.Depth = 8;
            rightLeg.Depth = 2;
            leftLeg.Depth = 4;
            rightHand.Depth = 6;
            leftHand.Depth = 10;

            //add body parts to draw list and sort
            bodyPartDrawList.Add(body);
            bodyPartDrawList.Add(rightLeg);
            bodyPartDrawList.Add(leftLeg);
            bodyPartDrawList.Add(rightHand);
            bodyPartDrawList.Add(leftHand);
            bodyPartDrawList.Sort();

        }

        #endregion
        
        #region Properties

        /// <summary>
        /// Calculate and return collision rectangle
        /// </summary>
        public Rectangle CollisionRectangle
        {
            get {
                Rectangle collisionRectangle = new Rectangle(DrawRectangle.X + 15, DrawRectangle.Y + Height/2,
                        55, (Height/2) - 5);
                return collisionRectangle;
            }
        }

        public MonsterAnimationState AnimationState
        {
            get { return animationState; }
            set { animationState = value; }
        }
        
        public bool ExplosionSpawned
        {
            get { return explosionSpawned; }
            set { explosionSpawned = value; }
        }

        public int BasicAttackDamage
        {
            get { return basicAttackDmg; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Move the monster and sprite components based on movement direction
        /// </summary>
        /// <param name="xComponent">The amount the monster moves in the x direction</param>
        /// <param name="yComponent">The amount the monster moves in the y direction</param>
        public void Move(float xComponent, float yComponent)
        {
            xComponent = xComponent / 6;
            yComponent = (int)Math.Ceiling(yComponent / 6);
            if (animationState != MonsterAnimationState.Attacking)
            {
                // move the monster if it's in screen range                
                if (((int)xComponent != 0 ||
                    (int)yComponent != 0) && WithinScreenRange((int)xComponent, (int)yComponent))
                {
                    // change the animation state
                    animationState = MonsterAnimationState.Walking;

                    ////smooth movement
                    //if (facing == MonsterFacing.Up &&
                    //    yComponent > 0)
                    //{
                    //    yComponent = (int)(prevYMove / 1.3);
                    //}
                    //else if (facing == MonsterFacing.Down &&
                    //  yComponent < 0)
                    //{
                    //    yComponent = (int)(prevYMove / 1.3);
                    //}

                    // update location of the center body       
                    Y += (int)yComponent;
                    X += (int)xComponent;
                    body.Y += (int)yComponent;
                    body.X += (int)xComponent;
                    prevYMove = (int)yComponent;

                    //update location of legs
                    rightLeg.Y += (int)yComponent;
                    rightLeg.X += (int)xComponent;
                    leftLeg.Y += (int)yComponent;
                    leftLeg.X += (int)xComponent;

                    //update location of arms
                    rightHand.Y += (int)yComponent;
                    rightHand.X += (int)xComponent;
                    leftHand.Y += (int)yComponent;
                    leftHand.X += (int)xComponent;
                }
            }
        }

        /// <summary>
        /// Update the monster animations and facing states
        /// </summary>
        /// <param name="gameTime">The tracker for in-game time</param>
        /// <param name="drawList">The draw list to order the drawings</param>
        /// <param name="xComponent">The left-right movement vector</param>
        /// <param name="yComponent">The up-down movement vector</param>
        public new void Update(GameTime gameTime, ref GameObject[] drawList, float xComponent, float yComponent)
        {
            // update ripple effects
            for (int i = DestructionEffects.Count - 1; i >= 0; i--)
            {
                DestructionEffects[i].Update(gameTime);
                if (DestructionEffects[i].IsFinished)
                {
                    DestructionEffects.RemoveAt(i);
                }
            }

            if (Depth < drawList.GetLength(0) - 1 && Depth > 0)
            {
                drawList[Depth] = null;
            }
            Depth = (int)((DrawRectangle.Y) / 0.8) + DrawListOffset;
            while (Depth < drawList.GetLength(0) - 1 && Depth > 0 && drawList[Depth] is GameObject)
            {
                Depth += 1;
            }
            if (Depth < drawList.GetLength(0) - 1 && Depth > 0)
            {
                drawList[Depth] = this;
            }

            // update facing information to make sure we are attacking in the correct direction
            if (elapsedAnimationTime == 0)
            {
                FacingStateSwitch(xComponent, yComponent);
            }

            // update the bodypart sprites based on the facing state
            bodyPartDrawList.Sort();

            #region Walking Animation

            // activate a walking animation if appropriate
            if (animationState == MonsterAnimationState.Walking)
            {
                // check how long the animation has been playing and update the timers
                if (elapsedAnimationTime < animationTimeLimit)
                {
                    elapsedAnimationTime += gameTime.ElapsedGameTime.Milliseconds;
                    if (facing == MonsterFacing.Down || facing == MonsterFacing.Up ||
                        facing == MonsterFacing.Right || facing == MonsterFacing.Left)
                    {
                        if (elapsedAnimationTime > 40 * stepCounter)
                        {
                            // move the feet up and down clown
                            if (facing == MonsterFacing.Down || facing == MonsterFacing.Up)
                            {
                                rightLeg.Y -= walkStepDistance;
                                leftLeg.Y += walkStepDistance;
                            } else {
                                // move the feet left and right Mike
                                rightLeg.X += 3 * walkStepDistance;
                                leftLeg.X -= 3 * walkStepDistance;
                                // pick up feet movement when facing right
                                if (facing == MonsterFacing.Right && elapsedAnimationTime > animationTimeLimit / 2)
                                {
                                    if (walkStepDistance == 1)
                                    {
                                        rightLeg.Y += walkStepDistance;
                                    } else { leftLeg.Y -= walkStepDistance; }
                                } else if (facing == MonsterFacing.Right && elapsedAnimationTime <= animationTimeLimit / 2)
                                {
                                    if (walkStepDistance == 1)
                                    {
                                        rightLeg.Y -= walkStepDistance;
                                    } else { leftLeg.Y += walkStepDistance; }
                                }
                                // pick up feet when moving left
                                if (facing == MonsterFacing.Left && elapsedAnimationTime > animationTimeLimit / 2)
                                {
                                    if (walkStepDistance == 1)
                                    {
                                        leftLeg.Y += walkStepDistance;
                                    }
                                    else { rightLeg.Y -= walkStepDistance; }
                                }  else if (facing == MonsterFacing.Left && elapsedAnimationTime <= animationTimeLimit / 2)
                                {
                                    if (walkStepDistance == 1)
                                    {
                                        leftLeg.Y -= walkStepDistance;
                                    }
                                    else { rightLeg.Y += walkStepDistance; }
                                }
                            }

                            // move the arms back and forth
                            rightHand.X -= walkStepDistance;
                            leftHand.X += walkStepDistance;
                            if (walkStepDistance == walkFacingOffset)
                            {
                                if (stepCounter > 4)
                                {
                                    leftHand.Y -= 2 * walkFacingOffset * walkStepDistance;
                                }
                            }
                            else
                            {
                                if (stepCounter < 5)
                                {
                                    leftHand.Y -= 2 * walkFacingOffset * walkStepDistance;
                                }
                            }
                            stepCounter += 1;
                        }
                    }
                } else {
                    // if the animation is finished reset the trackers
                    animationState = MonsterAnimationState.Resting;
                    elapsedAnimationTime = 0;
                    walkStepDistance = walkStepDistance * (-1);
                    stepCounter = 0;
                    // create ripples step
                    steppingFoot = DownFoot();
                    Vector2 spawnVector = new Vector2(steppingFoot.DrawRectangle.Center.X, steppingFoot.DrawRectangle.Bottom - 10);
                    if (facing == MonsterFacing.Up)
                    {
                        spawnVector.Y -= 20;
                    }
                    DestructionEffects.Add(new AnimatedElement(content, spawnVector, AnimatedElement.AnimationType.Ripple));
                    //OnStep();
                }
            }

            #endregion

            #region Attacking Animation
            // activate attack animation if appropriate
            if (animationState == MonsterAnimationState.Attacking)
            {
                if (elapsedAnimationTime == 0)
                {
                    explosionSpawned = false;
                }

                // pick the correct hand
                if (walkStepDistance > 0)
                {
                    attackingHand = leftHand;
                } else {
                    attackingHand = rightHand;
                }

                // check how long the animation has been playing and update the timers
                if (elapsedAnimationTime < (animationTimeLimit * 2))
                {
                    elapsedAnimationTime += gameTime.ElapsedGameTime.Milliseconds;
                    // the animaiton should complete in one cycle. Use the attackStepDistance to switch between the different hand motions
                    if (elapsedAnimationTime > attackStepDistance * 2 * animationTimeLimit/3)
                    {
                        attackStepDistance += 1;
                    }
                    // move the objects in evenly spaced steps
                    if (elapsedAnimationTime > 40 * stepCounter)
                    {

                        #region Up And Down Attack Animation

                        //switch animation depending on facing state
                        if (facing == MonsterFacing.Down)
                        {
                            //store original positions
                            if (stepCounter == 0)
                            {
                                holdValue = attackingHand.Depth;
                                startingPosition[0] = new Vector2(attackingHand.X, attackingHand.Y);
                                windUpRadius = Math.Sqrt(Math.Pow(body.X - attackingHand.X, 2) + Math.Pow(body.Y - attackingHand.Y, 2));
                                startAngle = Math.Atan(Math.Abs(body.X - attackingHand.X) / Math.Abs(body.Y - attackingHand.Y));
                            }
                            // wind back hand in circle
                            if (attackStepDistance == 1)
                            {
                                attackingHand.X = body.X - walkStepDistance * (int)(windUpRadius * Math.Sin(startAngle + (stepCounter * Math.PI / 36)));
                                attackingHand.Y = body.Y + (int)(windUpRadius * Math.Cos(startAngle + (stepCounter * Math.PI / 36)));
                            }
                            // swing hand to the front and give it rotation
                            if (attackStepDistance == 2)
                            {
                                attackingHand.Depth = 100;
                                if (!attackRotation)
                                {
                                    attackingHand.Rotation += (float)Math.PI / 4;
                                    attackingHand.IsRotated = true;
                                    attackRotation = true;
                                }
                                attackingHand.X += walkStepDistance * Math.Abs(attackingHand.X - body.DrawRectangle.Center.X + (20)) / stepCounter;
                                attackingHand.Y += Math.Abs(attackingHand.Y - body.DrawRectangle.Bottom + 10) / stepCounter; 

                            }
                            // return hand to original position and rotation
                            if (attackStepDistance == 3)
                            {
                                // initiate attack event
                                OnAttack();

                                attackingHand.Depth = holdValue;
                                if (attackRotation)
                                {
                                    attackingHand.Rotation -= (float)Math.PI / 4;
                                    attackRotation = false;
                                }
                                attackingHand.X -= walkStepDistance * Math.Abs(attackingHand.X - (int)startingPosition[0].X) / 10;
                                attackingHand.Y -= Math.Abs(attackingHand.Y - (int)startingPosition[0].Y) / 10;
                            }
                            stepCounter += 1;
                        }
                        // initiate animation for facing up
                        if (facing == MonsterFacing.Up)
                        {
                            // save the stating positions so we can reset them later
                            if (stepCounter == 0 &&
                                elapsedAnimationTime < 100)
                            {
                                holdValue = attackingHand.Depth;
                                startRotation = attackingHand.Rotation;
                                startingPosition[0] = new Vector2(attackingHand.X, attackingHand.Y);
                                windUpRadius = Math.Sqrt(Math.Pow(body.X - attackingHand.X, 2) + Math.Pow(body.Y - attackingHand.Y, 2));
                                startAngle = Math.Atan(Math.Abs(body.X - attackingHand.X) / Math.Abs(body.Y - attackingHand.Y));

                            }
                            // The animaiton states should switch animation states halfway through
                            if (elapsedAnimationTime <= animationTimeLimit + 60)
                            {
                                attackingHand.X = body.X - walkStepDistance * (int)(windUpRadius * Math.Sin(startAngle + (stepCounter * Math.PI / 18)));
                                attackingHand.Y = body.Y + (int)(windUpRadius * Math.Cos(startAngle + (stepCounter * Math.PI / 18)));
                                attackingHand.IsRotated = true;
                                attackingHand.Rotation += (float)Math.PI / 18;
                                stepCounter += 1;
                                                                
                            } else {
                                // initiate attack event
                                OnAttack();

                                attackingHand.X = body.X - walkStepDistance * (int)(windUpRadius * Math.Sin(startAngle + (stepCounter * Math.PI / 36)));
                                attackingHand.Y = body.Y + (int)(windUpRadius * Math.Cos(startAngle + (stepCounter * Math.PI / 36)));
                                attackingHand.Rotation -= (float)Math.PI / 36;
                                stepCounter -= 1;
                            }          
                        }

                        #endregion

                        #region Left and Right Attack Animation

                        //switch animation depending on facing state
                        if (facing == MonsterFacing.Left || facing == MonsterFacing.Right)
                        {
                            //store original positions
                            if (stepCounter == 0)
                            {
                                startingPosition[0] = new Vector2(attackingHand.X, attackingHand.Y);
                            }
                            // wind back hand in circle
                            if (attackStepDistance == 1)
                            {
                                attackingHand.X += 2 * walkStepDistance;
                                attackingHand.Y += walkStepDistance;
                            }
                            // swing hand to the front
                            if (attackStepDistance == 2)
                            {
                                if (facing == MonsterFacing.Right)
                                {
                                    attackingHand.X += (Math.Abs(body.DrawRectangle.Right + 10 - attackingHand.X) / stepCounter);
                                } else {
                                    attackingHand.X += (body.X - 65 - attackingHand.X) / stepCounter;
                                }
                            }
                            // return hand to original position and rotation
                            if (attackStepDistance == 3)
                            {
                                // initiate attack event
                                OnAttack();
                                attackingHand.X -= ((attackingHand.X - (int)startingPosition[0].X) / 10);
                            }
                            stepCounter += 1;
                        }

                        #endregion

                    }
                } else {
                    // if the animation is finished reset the trackers
                    attackingHand.Y = (int)startingPosition[0].Y;
                    attackingHand.X = (int)startingPosition[0].X;
                    if (facing == MonsterFacing.Up)
                    {
                        attackingHand.Rotation = startRotation;
                    }                    
                    elapsedAnimationTime = 0;
                    stepCounter = 0;
                    attackStepDistance = 0;
                    animationState = MonsterAnimationState.Walking;
                }
            }

            #endregion

        }

        protected virtual Rectangle OnAttack()
        {
            
            Rectangle attackingHandCollisionBox = new Rectangle(attackingHand.DrawRectangle.Center.X, attackingHand.DrawRectangle.Center.Y, 5, 5);
               return AttackCollisionRectangleEvent(attackingHandCollisionBox, attackingHandCollisionBox, basicAttackDmg);         
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // draw ripple effects
            foreach (AnimatedElement elementx in DestructionEffects)
            {
                elementx.Draw(spriteBatch);
            }

            // draw each sprite
            foreach (GameObject bodyPartX in bodyPartDrawList)
            {
                SpriteEffects flipEffect = SpriteEffects.None;
                if (FlipSpriteH)
                {
                    flipEffect = SpriteEffects.FlipHorizontally;
                }
                if (bodyPartX.IsRotated)
                {
                    spriteBatch.Draw(bodyPartX.Sprite, RotationDrawRectangle(bodyPartX.DrawRectangle, bodyPartX.Rotation), null, Color.White, bodyPartX.Rotation, new Vector2(0, 0), flipEffect, 0f);
                } else {
                    spriteBatch.Draw(bodyPartX.Sprite, bodyPartX.DrawRectangle, null, Color.White, 0, new Vector2(), flipEffect, 0);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// returns the foot that just stepped down based on the facing state and leg position
        /// </summary>
        /// <returns></returns>
        private GameObject DownFoot()
        {
            GameObject downFoot = new GameObject(Sprite, Location);
            if (facing == MonsterFacing.Up)
            {
                if (leftLeg.Y > rightLeg.Y)
                {
                    downFoot = rightLeg;
                }
                else { downFoot = leftLeg; }
            }
            if (facing == MonsterFacing.Down)
            {
                if (leftLeg.Y > rightLeg.Y)
                {
                    downFoot = leftLeg;
                } else { downFoot = rightLeg; }
            }
            if (facing == MonsterFacing.Left)
            {
                if (leftLeg.X > rightLeg.X)
                {
                    downFoot = rightLeg;
                } else { downFoot = leftLeg; }
            }
            if (facing == MonsterFacing.Right)
            {
                if (leftLeg.X > rightLeg.X)
                {
                    downFoot = leftLeg;
                } else { downFoot = rightLeg; }
            }

            return downFoot;
        }

        private Rectangle RotationDrawRectangle(Rectangle drawRectangle, float rotation)
        {
            Rectangle newDrawRectangle = drawRectangle;
            float radius;

            radius = (float)Math.Sqrt(Math.Pow(drawRectangle.Width / 2, 2) + Math.Pow(drawRectangle.Height / 2, 2));
            newDrawRectangle.Y = drawRectangle.Center.Y - (int)(radius * (float)Math.Cos(rotation - Math.PI / 4));
            newDrawRectangle.X = drawRectangle.Center.X + (int)(radius * (float)Math.Sin(rotation - Math.PI / 4));
            
            return newDrawRectangle;
        }

        private void StoreLimbPositions()
        {
            if (facing == MonsterFacing.Down)
            {
                // store hand and leg starting positions
                walkStepHolder = walkStepDistance;
                startingPosition[1] = new Vector2(DrawRectangle.X - rightHand.X, DrawRectangle.Y - rightHand.Y);
                startingPosition[2] = new Vector2(DrawRectangle.X - leftHand.X, DrawRectangle.Y - leftHand.Y);
                startingPosition[3] = new Vector2(DrawRectangle.X - leftLeg.X, DrawRectangle.Y - leftLeg.Y);
                startingPosition[4] = new Vector2(DrawRectangle.X - rightLeg.X, DrawRectangle.Y - rightLeg.Y);
            }
        }

        private void ReturnToStartingLimbPositions()
        {
            rightHand.X = DrawRectangle.X - (int)startingPosition[1].X;
            rightHand.Y = DrawRectangle.Y - (int)startingPosition[1].Y;
            leftHand.X = DrawRectangle.X - (int)startingPosition[2].X;
            leftHand.Y = DrawRectangle.Y - (int)startingPosition[2].Y;
            leftLeg.X = DrawRectangle.X - (int)startingPosition[3].X;
            leftLeg.Y = DrawRectangle.Y - (int)startingPosition[3].Y;
            rightLeg.X = DrawRectangle.X - (int)startingPosition[4].X;
            rightLeg.Y = DrawRectangle.Y - (int)startingPosition[4].Y;
        }

        /// <summary>
        /// change which way the moster is facing based on it's movement vectors
        /// </summary>
        /// <param name="xComponent"></param>
        /// <param name="yComponent"></param>
        private void FacingStateSwitch(float xComponent, float yComponent)
        {

            // set body part positions if the moster is moving up or down
            if (Math.Abs(yComponent) > Math.Abs(xComponent))
            {
                // set body part positions if the moster is moving up
                if (yComponent < 0 &&
                    facing != MonsterFacing.Up)
                {
                    // store starting positions
                    StoreLimbPositions();
                    // set facing state and limb sprite
                    facing = MonsterFacing.Up;
                    body.Sprite = bodyParts[1];
                    rightLeg.Sprite = bodyParts[6];
                    leftLeg.Sprite = bodyParts[7];
                    rightHand.Sprite = bodyParts[8];
                    leftHand.Sprite = bodyParts[8];
                    // set limb sprite positions and walking tracker
                    ReturnToStartingLimbPositions();
                    walkStepDistance = walkStepHolder;
                    FlipSpriteH = false;
                    rightHand.X = DrawRectangle.X - (int)startingPosition[1].X + 10;
                    leftHand.Y = DrawRectangle.Y - (int)startingPosition[2].Y - (walkStepHolder * 10);
                    leftLeg.X = DrawRectangle.X - (int)startingPosition[3].X;
                    leftLeg.Y = DrawRectangle.Y - (int)startingPosition[3].Y;
                    rightLeg.X = DrawRectangle.X - (int)startingPosition[4].X;
                    rightLeg.Y = DrawRectangle.Y - (int)startingPosition[4].Y;
                    leftHand.IsRotated = true;
                    leftHand.Rotation += (float)Math.PI;
                    walkFacingOffset = -1;
                }
                // set body part positions if the monster is moving down
                else if (yComponent > 0 &&
                    facing != MonsterFacing.Down)
                {
                    facing = MonsterFacing.Down;
                    body.Sprite = bodyParts[0];
                    leftLeg.Sprite = bodyParts[4];
                    // restore hand and leg starting positions
                    FlipSpriteH = false;
                    ReturnToStartingLimbPositions();
                    leftHand.IsRotated = false;
                    leftHand.Rotation = 0;
                    walkFacingOffset = 1;
                    // set limb sprites and walking tracker
                    walkStepDistance = walkStepHolder;
                    rightLeg.Sprite = bodyParts[5];
                    rightHand.Sprite = bodyParts[2];
                    leftHand.Sprite = bodyParts[3];
                }
            } else {
                // set body part positions if the monster is moving right
                if (xComponent > 0 &&
                    facing != MonsterFacing.Right)
                {
                    // store starting positions
                    StoreLimbPositions();
                    // set hand and leg starting sprites
                    facing = MonsterFacing.Right;
                    body.Sprite = bodyParts[10];
                    leftLeg.Sprite = bodyParts[9];
                    rightLeg.Sprite = bodyParts[9];
                    leftHand.Sprite = bodyParts[3];
                    rightHand.Sprite = bodyParts[2];
                    // set hand and leg starting positions
                    walkStepDistance = 1;
                    FlipSpriteH = false;
                    leftHand.IsRotated = false;
                    leftHand.Rotation = 0;
                    leftLeg.Y = DrawRectangle.Y + 33;
                    leftLeg.X = DrawRectangle.X + 24;
                    rightLeg.Y = DrawRectangle.Y + 30;
                    rightLeg.X = DrawRectangle.X - 4;
                    rightLeg.Y = DrawRectangle.Y + 27;
                    rightHand.X = DrawRectangle.X + 10;
                    leftHand.X = DrawRectangle.X;
                    leftHand.Y = DrawRectangle.Y + 10;
                }
                // set body part positions if the monster is moving left
                if (xComponent < 0 &&
                    facing != MonsterFacing.Left)
                {
                    // store starting positions
                    StoreLimbPositions();
                    // set hand and leg starting positions
                    facing = MonsterFacing.Left;
                    // set hand and leg starting sprites
                    body.Sprite = bodyParts[10];
                    leftLeg.Sprite = bodyParts[9];
                    rightLeg.Sprite = bodyParts[9];
                    leftHand.Sprite = bodyParts[3];
                    rightHand.Sprite = bodyParts[2];
                    // set hand and leg starting positions
                    walkStepDistance = 1;
                    FlipSpriteH = true;
                    leftHand.IsRotated = false;
                    leftHand.Rotation = 0;
                    leftLeg.Y = DrawRectangle.Y + 33;
                    leftLeg.X = DrawRectangle.X + 28;
                    rightLeg.X = DrawRectangle.X + 2;
                    rightLeg.Y = DrawRectangle.Y + 27;
                    rightHand.X = DrawRectangle.X - 10;
                    leftHand.X = DrawRectangle.X - 5;
                    leftHand.Y = DrawRectangle.Y + 10;
                }
            }

        }

        /// <summary>
        /// check to see if the arguments are inside the screen
        /// </summary>
        /// <param name="xComponenet">the distance we are moving left and right</param>
        /// <param name="yComponenet">the distance we are moving up and down</param>
        /// <returns></returns>
        private bool WithinScreenRange(int xComponenet, int yComponent)
        {
            if (DrawRectangle.X + xComponenet >= 0 &&
                DrawRectangle.Right + xComponenet < Game1.WINDOW_WIDTH &&
                DrawRectangle.Y + yComponent >= 0 &&
                DrawRectangle.Bottom + yComponent < Game1.WINDOW_HEIGHT)
            {
                return true;
            }
            else { return false; }
        }

        #endregion

    }
}
