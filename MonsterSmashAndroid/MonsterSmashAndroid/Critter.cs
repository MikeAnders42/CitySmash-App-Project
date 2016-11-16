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
    public class Critter : GameObject
    {

        #region Fields

        // testing
        private bool stopSpinning = false;
        private bool startTurn = false;
        private List<Vector2> targetRoadHistory = new List<Vector2>();

        // destruction support
        private int crushelapsedTime = 0;
        private const int crushTimeLimit = 400;
        private bool squished = false;

        // sprite support
        public enum CritterType { Dog, Person, Car};
        private CritterType type;
        private Texture2D[] spriteList = new Texture2D[4];
        private Texture2D crushedSprite;
        private ContentManager content;

        // animation support
        private int elapsedGameTime = 0;
        private const int animationTimeLimit = 500;
        private int walkingFrameCount = 0;
        private int elapsedSlowDownTime = 0;
        private const int SlowDownTimeLimit = 150;
        private int elapsedSpeedUpTime = 0;
        private const int speedUpTimeLimit = 200;
        private int elapsedNotSearchingForIntersetionTime = 0;
        private const int NotSearchingForIntersetionTimeLimit = 500;
        public Vector2 Origin;


        // draw order/occlusion support
        int drawOrder;
        int drawListOffset = 220;

        #endregion

        #region Constructors

        /// <summary>
        /// create a new critter
        /// </summary>
        /// <param name="sprite">The sprite to use for the critter</param>
        /// <param name="location">The location to spawn the critter at</param>
        /// /// <param name="type">The type of critter to spawn</param>
        public Critter(ContentManager content, Texture2D sprite, Vector2 location, CritterType type) : base(sprite, location)
        {
            this.type = type;
            this.content = content;
            StartMapCoordinates = location;
            Opacity = 1f;
            this.IsRotated = true;

            // turning support
            XOffsetMultiplier = 1;
            RadiansTurned = 0;
            IntersectionChecking = true;
            TurnDone = false;

            // load the content for dogs
            if (type == CritterType.Dog)
            {
                Sprite = content.Load<Texture2D>("graphics/Balloons/BalloonDog");
                crushedSprite = null;
                DrawRectangle = new Rectangle((int)location.X - 10, (int)location.Y - 10, 20, 20);
            }
           // load the content for people
           if (type == CritterType.Person)
            {               
                spriteList[0] = content.Load<Texture2D>("Graphics/Balloons/BalloonPersonWalking0");
                spriteList[1] = content.Load<Texture2D>("Graphics/Balloons/BalloonPersonWalking1");
                spriteList[2] = content.Load<Texture2D>("Graphics/Balloons/BalloonPersonWalking2");
                spriteList[3] = content.Load<Texture2D>("Graphics/Balloons/BalloonPersonWalking3");
                crushedSprite = null;
                Sprite = spriteList[0];
                DrawRectangle = new Rectangle((int)location.X - 10, (int)location.Y - 15, 20, 20);
            }
           // load content for cars
           if (type == CritterType.Car)
            {
                int whichSprite = Rand.rand.Next(1, 7);
                DrawRectangle = new Rectangle((int)location.X - 13, (int)location.Y - 11, 26, 26);
                switch (whichSprite)
                {
                    
                    case 1:
                        Sprite = content.Load<Texture2D>("Graphics/Cars/BlackSUV");
                        break;
                    case 2:
                        Sprite = content.Load<Texture2D>("Graphics/Cars/RedSUV");
                        break;
                    case 3:
                        Sprite = content.Load<Texture2D>("Graphics/Cars/SilverSUV");
                        break;
                    case 4:
                        Sprite = content.Load<Texture2D>("Graphics/Cars/WhiteSUV");
                        break;
                    case 5:
                        Sprite = content.Load<Texture2D>("Graphics/Cars/BlueCar");
                        break;
                    case 6:
                        Sprite = content.Load<Texture2D>("Graphics/Cars/RedCar");
                        break;
                }
                crushedSprite = content.Load<Texture2D>("Graphics/Cars/CarWreck");
            }
           //  randomize starting animation and add movement support
            elapsedGameTime = Rand.rand.Next(1, 4) * 200;
            XMove = 0;
            YMove = 0;
        }

        #endregion

        #region Properties

        public CritterType Type
        {
            get { return type; }
        }

        public bool Squished
        {
            get { return squished; }
            set { squished = value; }
        }      

        public bool CrossingIntersection
        {
            get; set;
        }

        public Vector2 TargetMapCoordinates
        {
            get;
            set;
        }

        public Vector2 StartMapCoordinates
        {
            get; set;
        }

        public Vector2 TurnPoint
        {
            get; set;
        }

        public int NodeNumber
        {
            get; set;
        }

        public int XMove
        {
            get; set;
        }

        public int YMove
        {
            get; set;
        }

        public int TargetNode
        {
            get; set;
        }

        public Rectangle PersonNodeCollisionRectangle
        {
            get { return new Rectangle(DrawRectangle.Center.X, DrawRectangle.Bottom - 9, 6, 9); }
        }

        public Rectangle CarCollisionRectangle
        {
            get { return new Rectangle(DrawRectangle.Center.X + 5, DrawRectangle.Bottom + 5, Width - 10, Height - 10); }
        }

        public bool SlowDownQueued
        {
            get; set;
        }

        public bool SpeedUpQueued
        {
            get; set;
        }

        /// <summary>
        /// Set whether the first node of a targeted intersection has been reached
        /// </summary>
        public bool NodeReached
        {
            get; set;
        }

        public bool StoppedForInt
        {
            get; set;
        }

        public bool StoppedForCar
        {
            get; set;
        }

        public bool StartTurn
        {
            get { return startTurn; }
            set { startTurn = value; }
        }

        public bool IntersectionChecking
        {
            get; set;
        }

        public bool PauseCarMovement { get; set; }

        public bool TurnDone { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Animate the people and dog sprites
        /// </summary>
        /// <param name="gameTime">the gametime tracker</param>
        public void Animate(GameTime gameTime, ref GameObject[] drawList)
        {
            //// debugging field for road targets
            //if (targetRoadHistory.Count == 0 || targetRoadHistory.Last() != TargetMapCoordinates)
            //{
            //    targetRoadHistory.Add(TargetMapCoordinates);
            //}

            if (!squished)
            {

                #region Car Animation

                // turn car right
                if (TurnDirection == Turn.Right && !stopSpinning)
                {
                    if (!startTurn)
                    {
                        if (RadiansTurned < Math.PI / 2)
                        {
                            RadiansTurned += (float)Math.PI / 100;
                            Rotation += (float)Math.PI / 100;
                            if (!FlipRotate && !FlipSpriteH)
                            {
                                IsRotated = false;
                            }
                        } else {
                            XOffsetMultiplier = 1;
                            TurnDirection = Turn.Straight;
                            stopSpinning = true;
                            TurnDone = true;
                        }
                    }
                }

                #region Car SpeedUp and SlowDown

                // set whether the car should stop at an intersection
                if (!IntersectionChecking)
                {
                    if (elapsedNotSearchingForIntersetionTime < NotSearchingForIntersetionTimeLimit)
                    {
                        elapsedNotSearchingForIntersetionTime += gameTime.ElapsedGameTime.Milliseconds;
                    }
                    else { IntersectionChecking = true; }
                }

                // turn off slowdown if we are stopped
                if ((XMove == 0 && YMove == 0) ||
                    Math.Abs(XMove + YMove) == 1)
                {
                    SlowDownQueued = false;
                }
                // make cars slow down one tick every few hundred millisecond
                if (SlowDownQueued)
                {
                    // prioritize slowdown
                    SpeedUpQueued = false;
                    if (elapsedSlowDownTime < SlowDownTimeLimit)
                    {
                        elapsedSlowDownTime += gameTime.ElapsedGameTime.Milliseconds;
                    }
                    else
                    {
                        if (XMove != 0)
                        {
                            XMove = (Math.Abs(XMove) - 1) * (XMove / Math.Abs(XMove));
                        }
                        if (YMove != 0)
                        {
                            YMove = (Math.Abs(YMove) - 1) * (YMove / Math.Abs(YMove));
                        }
                        SlowDownQueued = false;
                        elapsedSlowDownTime = 0;
                    }
                }
                // speed car up
                if (SpeedUpQueued)
                {
                    // delay between speed increases
                    if (elapsedSpeedUpTime < speedUpTimeLimit)
                    {
                        elapsedSpeedUpTime += gameTime.ElapsedGameTime.Milliseconds;
                    }
                    else
                    {
                        elapsedSpeedUpTime = 0;
                        if (Math.Abs(XMove) == 3 || Math.Abs(YMove) == 3)
                        {
                            SpeedUpQueued = false;
                        }
                        else
                        {
                            if (YMove != 0)
                            { YMove += 1 * YMove / Math.Abs(YMove); }
                            if (XMove != 0)
                            { XMove += 1 * XMove / Math.Abs(XMove); }
                        }
                    }
                }
                // make cars move
                if (type == CritterType.Car && !PauseCarMovement)
                {
                    if (startTurn)
                    {
                        X += YMove;
                        Y -= XMove;
                    }
                    else if (TurnDirection == Turn.Straight)
                    {
                        X += XMove;
                        Y += YMove;
                    }
                }
                #endregion

                #endregion

                #region Person Animation

                // initiate walking animation for people
                if (type == CritterType.Person)
                {
                    // change facing direction
                    if (XMove < 0)
                    {
                        FlipSpriteH = true;
                    }
                    else { FlipSpriteH = false; }
                    // actvate animation
                    elapsedGameTime += gameTime.ElapsedGameTime.Milliseconds;

                    if (elapsedGameTime < animationTimeLimit)
                    {
                        if (elapsedGameTime > (animationTimeLimit / 4) * walkingFrameCount)
                        {
                            X += XMove;
                            Y += YMove;
                            Sprite = spriteList[walkingFrameCount];
                            walkingFrameCount++;
                        }

                    } else {
                        // reset walking animation
                        elapsedGameTime = 0;
                        walkingFrameCount = 0;
                        Sprite = spriteList[walkingFrameCount];
                    }
                }

                // update draw order based on current position
                drawList[drawOrder] = null;
                if (drawOrder < drawList.GetLength(0))
                {
                    drawOrder = (int)Math.Abs((DrawRectangle.Y / 0.8)) + drawListOffset;
                }
                while (drawOrder < drawList.GetLength(0) - 1 && drawList[drawOrder] != null)
                {
                    drawOrder += 1;
                }
                drawList[drawOrder] = this;

                #endregion

            } else {

                #region Destruction Animation

                // update and clear out finished animation effects
                for (int i = DestructionEffects.Count() - 1; i >=0; i--)
                {
                    DestructionEffects[i].Update(gameTime);
                    if (DestructionEffects[i].IsFinished)
                    {
                        DestructionEffects.RemoveAt(i);
                    }
                }

                //switch the sprite over, and play the destruction sounds/animations
                if (type == CritterType.Car)
                {
                    // start the timer for the car sprite to explode
                    if (crushelapsedTime < crushTimeLimit)
                    {
                        crushelapsedTime += gameTime.ElapsedGameTime.Milliseconds;
                        // add destruction animations
                        if (DestructionEffects.Count == 0)
                        {
                            // switch over the car sprite and add fire
                            Sprite = crushedSprite;
                            Vector2 spawnVector = new Vector2(X + (Width / 2) + Rand.rand.Next(-5, 6), Y + (Height / 2) + Rand.rand.Next(-5, 6));
                            DestructionEffects.Add(new AnimatedElement(content, spawnVector, AnimatedElement.AnimationType.Fire));
                        }
                    }  else {
                        // explode
                        if (DestructionEffects.Last().Type == AnimatedElement.AnimationType.Fire)
                        {
                            DestructionEffects.Clear();
                            Vector2 spawnVector = new Vector2(X + (Width / 2), Y + (Height / 2));
                            DestructionEffects.Add(new AnimatedElement(content, spawnVector, AnimatedElement.AnimationType.Explosion0));
                        }
                        // if the explosion has finished deactivate the car
                        if (DestructionEffects.Count() == 0)
                        { Active = false; }
                    }
                } else if (type == CritterType.Person)
                {
                    Active = false;
                }

                #endregion

            }
        }

        #endregion

    }
}
