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
    public class Road : GameObject
    {
        #region Fields

        // Sprite selection and map generation support
        public enum RoadWidth {TwoLane, FourLane };
        public enum RoadType { Intersection, Road}
        public enum SidewalkFacing { Left, Right, Up, Down}
        
        private RoadWidth width;
        private RoadType type;
        private SidewalkFacing facing;
        private bool horizontal = true;

        // support for movement nodes for roads
        public Vector2[] roadNodes = new Vector2[6];
        private Vector2 botLWalkNode;
        private Vector2 topLWalkNode;
        private Vector2 botRDriveNode0;
        private Vector2 botRDriveNode1;
        private Vector2 topLDriveNode0;
        private Vector2 topLDriveNode1;

        // support for movement nodes for intersections
        public Vector2[] intersectionNodes = new Vector2[12];
        private Vector2 iTopRWalkNode;
        private Vector2 iBotRWalkNode;
        private Vector2 iTopLWalkNode;
        private Vector2 iBotLWalkNode;

        // stopping nodes
        private Vector2 iTopLStopNode0;
        private Vector2 iTopLStopNode1;
        private Vector2 iBotRStopNode0;
        private Vector2 iBotRStopNode1;
        private Vector2 iLeftBotStopNode0;
        private Vector2 iLeftBotStopNode1;
        private Vector2 iRightTopStopNode0;
        private Vector2 iRightTopStopNode1;

        // testing support
        Rectangle testRectangle;
        Texture2D testSquareSprite;

        #endregion

        #region Constructors

        /// <summary>
        /// Spawn a new road
        /// </summary>
        /// <param name="sprite">image to use for object</param>
        /// <param name="location">top left of road (should be a multiple of 80)</param>
        /// /// <param name="width">the number of lanes for the road</param>
        /// /// <param name="length">units of length for the road (80 pixels in a unit)</param>
        /// /// <param name="type">specify road or intersection</param>
        public Road(ContentManager Content, String spritename, Vector2 location, RoadWidth width, RoadType type, bool horizontal) : base(Content, spritename, location)
        {
            this.width = width;
            this.type = type;
            this.horizontal = horizontal;

            // set opacity
            this.OpacityStrength = 0;

            // set occlusion properties
            if (type == RoadType.Intersection)
            {
                DrawListOffset = 100;
            }
            else { DrawListOffset = 1; }

            // set drawRectangle for road/intersection
            DrawRectangle = new Rectangle((int)location.X, (int)location.Y, 82, 82);

            testSquareSprite = Content.Load<Texture2D>("Graphics/TestSquare");

            // add rotation to exception sprite
            if (spritename.Contains("2") &&
                width == RoadWidth.FourLane)
            {
                Rotation += (float)Math.PI / 2;
                IsRotated = false;
            }

            // add built properties for intersections
            if (spritename.Contains("Three") &&
                width == RoadWidth.TwoLane)
            {
                RightSideBuilt = false;
            }

            // set rotation for vertical roads
            if (!horizontal)
            {
                Rotation += (float)Math.PI / 2;
                IsRotated = false;
            }

            #region Pathing Node Creation

            // create pathing nodes for each road type
            if (type == RoadType.Intersection && width == RoadWidth.TwoLane)
            {
                if (spritename.Contains("4"))
                {
                    // nodes for a 2x4 intersection
                    iTopLStopNode1 = new Vector2((int)location.X + 30, (int)location.Y + 4);
                    iTopLStopNode0 = new Vector2((int)location.X + 17, (int)location.Y + 4);
                    iBotRStopNode1 = new Vector2((int)location.X + 45, (int)location.Y + 71);
                    iBotRStopNode0 = new Vector2((int)location.X + 58, (int)location.Y + 71);
                    iLeftBotStopNode0 = new Vector2((int)location.X - 7, (int)location.Y + 45);
                    iRightTopStopNode0 = new Vector2((int)location.X + 84, (int)location.Y + 30);

                    iBotLWalkNode = new Vector2((int)location.X + 7, (int)location.Y + 56);
                    iBotRWalkNode = new Vector2((int)location.X + 69, (int)location.Y + 56);
                    iTopLWalkNode = new Vector2((int)location.X + 7, (int)location.Y + 20);
                    iTopRWalkNode = new Vector2((int)location.X + 69, (int)location.Y + 20);
                } else {
                    // nodes for a 2x2-four intersection
                    iTopRWalkNode = new Vector2((int)location.X + 56, (int)location.Y + 18);
                    iBotRWalkNode = new Vector2((int)location.X + 56, (int)location.Y + 58);
                    iTopLWalkNode = new Vector2((int)location.X + 17, (int)location.Y + 18);
                    iBotLWalkNode = new Vector2((int)location.X + 17, (int)location.Y + 58);

                    iTopLStopNode0 = new Vector2((int)location.X + 30, (int)location.Y + 4);
                    iBotRStopNode0 = new Vector2((int)location.X + 45, (int)location.Y + 71);
                    iLeftBotStopNode0 = new Vector2((int)location.X + 3, (int)location.Y + 45);
                    iRightTopStopNode0 = new Vector2((int)location.X + 71, (int)location.Y + 30);
                }
            }

            if (type == RoadType.Intersection && width == RoadWidth.FourLane)
            {
                // nodes for a 4x2 intersection
                if (spritename.Contains("2"))
                {
                    iTopLStopNode0 = new Vector2((int)location.X + 30, (int)location.Y - 7);
                    iBotRStopNode0 = new Vector2((int)location.X + 45, (int)location.Y + 84);
                    iLeftBotStopNode0 = new Vector2((int)location.X + 3, (int)location.Y + 46);
                    iLeftBotStopNode1 = new Vector2((int)location.X + 3, (int)location.Y + 58);
                    iRightTopStopNode0 = new Vector2((int)location.X + 71, (int)location.Y + 17);
                    iRightTopStopNode1 = new Vector2((int)location.X + 71, (int)location.Y + 31);

                    iBotLWalkNode = new Vector2((int)location.X + 17, (int)location.Y + 69);
                    iBotRWalkNode = new Vector2((int)location.X + 56, (int)location.Y + 69);
                    iTopLWalkNode = new Vector2((int)location.X + 17, (int)location.Y + 7);
                    iTopRWalkNode = new Vector2((int)location.X + 56, (int)location.Y + 7);
                    
                } else {
                    // nodes for a 4x4 intersection
                    iTopLStopNode1 = new Vector2((int)location.X + 30, (int)location.Y - 7);
                    iTopLStopNode0 = new Vector2((int)location.X + 17, (int)location.Y - 7);
                    iBotRStopNode1 = new Vector2((int)location.X + 45, (int)location.Y + 84);
                    iBotRStopNode0 = new Vector2((int)location.X + 58, (int)location.Y + 84);
                    iLeftBotStopNode0 = new Vector2((int)location.X - 7, (int)location.Y + 46);
                    iLeftBotStopNode1 = new Vector2((int)location.X - 7, (int)location.Y + 58);
                    iRightTopStopNode0 = new Vector2((int)location.X + 84, (int)location.Y + 17);
                    iRightTopStopNode1 = new Vector2((int)location.X + 84, (int)location.Y + 31);


                    iBotLWalkNode = new Vector2((int)location.X + 7, (int)location.Y + 69);
                    iBotRWalkNode = new Vector2((int)location.X + 69, (int)location.Y + 69);
                    iTopLWalkNode = new Vector2((int)location.X + 7, (int)location.Y + 7);
                    iTopRWalkNode = new Vector2((int)location.X + 69, (int)location.Y + 7);                   
                }
            }
            
            if (width == RoadWidth.TwoLane && type == RoadType.Road && horizontal)
            {
                // nodes for a 2 lane horzontal road
                topLWalkNode = new Vector2((int)location.X, (int)location.Y + 20);
                topLDriveNode0 = new Vector2((int)location.X, (int)location.Y + 30);
                botRDriveNode0 = new Vector2((int)location.X, (int)location.Y + 45);
                botLWalkNode = new Vector2((int)location.X, (int)location.Y + 56);
            }
            else if (width == RoadWidth.FourLane && type == RoadType.Road && horizontal)
            {
                // nodes for a 4 lane horzontal road
                topLWalkNode = new Vector2((int)location.X, (int)location.Y + 7);
                topLDriveNode0 = new Vector2((int)location.X, (int)location.Y + 17);
                topLDriveNode1 = new Vector2((int)location.X, (int)location.Y + 31);
                botRDriveNode1 = new Vector2((int)location.X, (int)location.Y + 46);
                botRDriveNode0 = new Vector2((int)location.X, (int)location.Y + 58);
                botLWalkNode = new Vector2((int)location.X, (int)location.Y + 69);                
            }

            if (width == RoadWidth.TwoLane && type == RoadType.Road && !horizontal)
            {
                // nodes for a 2 lane vertical road
                topLWalkNode = new Vector2((int)location.X + 20, (int)location.Y);
                topLDriveNode0 = new Vector2((int)location.X + 30, (int)location.Y);
                botRDriveNode0 = new Vector2((int)location.X + 45, (int)location.Y);
                botLWalkNode = new Vector2((int)location.X + 56, (int)location.Y);
            } else if (width == RoadWidth.FourLane && !horizontal) {
                // nodes for a 4 lane vertical road
                topLWalkNode = new Vector2((int)location.X + 7, (int)location.Y);
                topLDriveNode0 = new Vector2((int)location.X + 17, (int)location.Y);
                topLDriveNode1 = new Vector2((int)location.X + 31, (int)location.Y);
                botRDriveNode1 = new Vector2((int)location.X + 46, (int)location.Y);
                botRDriveNode0 = new Vector2((int)location.X + 58, (int)location.Y);
                botLWalkNode = new Vector2((int)location.X + 69, (int)location.Y);
            }

        // access to movement nodes for roads
        roadNodes[0] = botLWalkNode;
        roadNodes[1] = topLWalkNode;
        roadNodes[2] = botRDriveNode0;
        roadNodes[3] = botRDriveNode1;
        roadNodes[4] = topLDriveNode0;
        roadNodes[5] = topLDriveNode1;

        // access to movement nodes for intersections
        intersectionNodes[0] = iTopRWalkNode;
        intersectionNodes[1] = iBotRWalkNode;
        intersectionNodes[2] = iTopLWalkNode;
        intersectionNodes[3] = iBotLWalkNode;
        intersectionNodes[4] = iTopLStopNode0;
        intersectionNodes[5] = iTopLStopNode1;
        intersectionNodes[6] = iBotRStopNode0;
        intersectionNodes[7] = iBotRStopNode1;
        intersectionNodes[8] = iLeftBotStopNode0;
        intersectionNodes[9] = iLeftBotStopNode1;
        intersectionNodes[10] = iRightTopStopNode0;
        intersectionNodes[11] = iRightTopStopNode1;

        #endregion

    }

        /// <summary>
        /// Spawn a new dead-end intersection with rotation
        /// </summary>
        /// <param name="sprite">image to use for object</param>
        /// <param name="location">top left of intersection (should be a multiple of 80)</param>
        /// <param name="width">number of lanes the road has</param>
        /// <param name="facing">which way the dead end is facing</param>
        public Road(ContentManager Content, String spritename, Vector2 location, RoadWidth width, SidewalkFacing facing) : base(Content, spritename, location)
        {
            this.width = width;
            type = RoadType.Intersection;
            this.facing = facing;
            this.DrawListOffset = 100;

            // set opacity
            OpacityStrength = 0;

            // set drawRectangle for road/intersection
            DrawRectangle = new Rectangle((int)location.X, (int)location.Y, 82, 82);

            testSquareSprite = Content.Load<Texture2D>("Graphics/TestSquare");

            // add rotation to exception sprite
            if (spritename.Contains("4-4"))
            {
                Rotation = (float)Math.PI / 2;
            }

            // set rotation and built properties
            switch (facing)
            {                
                case SidewalkFacing.Down:
                    Rotation += -(float)Math.PI / 2;
                    break;
                case SidewalkFacing.Left:
                    LeftSideBuilt = false;
                    Rotation += -(float)Math.PI;
                    break;
                case SidewalkFacing.Up:
                    Rotation += -(float)(3 * Math.PI / 2);
                    break;                       
                case SidewalkFacing.Right:
                    RightSideBuilt = false;
                    break;
            }

            // trigger sprite rotation
            if (Rotation != 0f)
            {
                IsRotated = false;
            }

            #region Pathing Node Creation

            if (width == RoadWidth.TwoLane)
            {
                if (spritename.Contains("4"))
                {
                    if (Rotation > 0)
                    {
                        X += 1;
                    }
                    // pathing nodes for 2x4-Three intersection
                    iTopLStopNode1 = new Vector2((int)location.X + 30, (int)location.Y + 4);
                    iTopLStopNode0 = new Vector2((int)location.X + 17, (int)location.Y + 4);
                    iBotRStopNode1 = new Vector2((int)location.X + 45, (int)location.Y + 71);
                    iBotRStopNode0 = new Vector2((int)location.X + 58, (int)location.Y + 71);
                    iLeftBotStopNode0 = new Vector2((int)location.X - 7, (int)location.Y + 45);
                    iRightTopStopNode0 = new Vector2((int)location.X + 84, (int)location.Y + 30);

                    iBotLWalkNode = new Vector2((int)location.X + 7, (int)location.Y + 56);
                    iBotRWalkNode = new Vector2((int)location.X + 69, (int)location.Y + 56);
                    iTopLWalkNode = new Vector2((int)location.X + 7, (int)location.Y + 20);
                    iTopRWalkNode = new Vector2((int)location.X + 69, (int)location.Y + 20);
                } else {
                    // pathing nodes for 2x2-Three intersection
                    if (Rotation != 0)
                    {
                        Y += 1;
                    } else { Y -= 1;  }
                    iTopRWalkNode = new Vector2((int)location.X + 56, (int)location.Y + 18);
                    iBotRWalkNode = new Vector2((int)location.X + 56, (int)location.Y + 58);
                    iTopLWalkNode = new Vector2((int)location.X + 17, (int)location.Y + 18);
                    iBotLWalkNode = new Vector2((int)location.X + 17, (int)location.Y + 58);

                    iTopLStopNode0 = new Vector2((int)location.X + 30, (int)location.Y + 4);
                    iBotRStopNode0 = new Vector2((int)location.X + 45, (int)location.Y + 71);
                    iLeftBotStopNode0 = new Vector2((int)location.X + 3, (int)location.Y + 45);
                    iRightTopStopNode0 = new Vector2((int)location.X + 71, (int)location.Y + 30);
                }
            }

            if (type == RoadType.Intersection && width == RoadWidth.FourLane)
            {
                // nodes for a 4x2 intersection
                if (spritename.Contains("2"))
                {
                    Y -= 2;
                    iTopLStopNode0 = new Vector2((int)location.X + 30, (int)location.Y - 7);
                    iBotRStopNode0 = new Vector2((int)location.X + 45, (int)location.Y + 84);
                    iLeftBotStopNode0 = new Vector2((int)location.X + 3, (int)location.Y + 46);
                    iLeftBotStopNode1 = new Vector2((int)location.X + 3, (int)location.Y + 58);
                    iRightTopStopNode0 = new Vector2((int)location.X + 71, (int)location.Y + 17);
                    iRightTopStopNode1 = new Vector2((int)location.X + 71, (int)location.Y + 31);

                    iBotLWalkNode = new Vector2((int)location.X + 56, (int)location.Y + 69);
                    iBotRWalkNode = new Vector2((int)location.X + 56, (int)location.Y + 69);
                    iTopLWalkNode = new Vector2((int)location.X + 17, (int)location.Y + 7);
                    iTopRWalkNode = new Vector2((int)location.X + 17, (int)location.Y + 7);

                }
                else
                {
                    // nodes for a 4x4 intersection
                    iTopLStopNode1 = new Vector2((int)location.X + 30, (int)location.Y - 7);
                    iTopLStopNode0 = new Vector2((int)location.X + 17, (int)location.Y - 7);
                    iBotRStopNode1 = new Vector2((int)location.X + 45, (int)location.Y + 84);
                    iBotRStopNode0 = new Vector2((int)location.X + 58, (int)location.Y + 84);
                    iLeftBotStopNode0 = new Vector2((int)location.X - 7, (int)location.Y + 46);
                    iLeftBotStopNode1 = new Vector2((int)location.X - 7, (int)location.Y + 58);
                    iRightTopStopNode0 = new Vector2((int)location.X + 84, (int)location.Y + 17);
                    iRightTopStopNode1 = new Vector2((int)location.X + 84, (int)location.Y + 31);


                    iBotLWalkNode = new Vector2((int)location.X + 7, (int)location.Y + 69);
                    iBotRWalkNode = new Vector2((int)location.X + 69, (int)location.Y + 69);
                    iTopLWalkNode = new Vector2((int)location.X + 7, (int)location.Y + 7);
                    iTopRWalkNode = new Vector2((int)location.X + 69, (int)location.Y + 7);
                }
            }

            // access to movement nodes for roads
            roadNodes[0] = botLWalkNode;
            roadNodes[1] = topLWalkNode;
            roadNodes[2] = botRDriveNode0;
            roadNodes[3] = botRDriveNode1;
            roadNodes[4] = topLDriveNode0;
            roadNodes[5] = topLDriveNode1;

            // access to movement nodes for intersections
            intersectionNodes[0] = iTopRWalkNode;
            intersectionNodes[1] = iBotRWalkNode;
            intersectionNodes[2] = iTopLWalkNode;
            intersectionNodes[3] = iBotLWalkNode;
            intersectionNodes[4] = iTopLStopNode0;
            intersectionNodes[5] = iTopLStopNode1;
            intersectionNodes[6] = iBotRStopNode0;
            intersectionNodes[7] = iBotRStopNode1;
            intersectionNodes[8] = iLeftBotStopNode0;
            intersectionNodes[9] = iLeftBotStopNode1;
            intersectionNodes[10] = iRightTopStopNode0;
            intersectionNodes[11] = iRightTopStopNode1;

            #endregion

        }

        #endregion

        #region Properties

        public RoadWidth Width
        {
            get { return width; }
        }

        public RoadType Type
        {
            get { return type; }
        }

        public bool Horizontal
        {
            get { return horizontal; }
        }

        #endregion

        public void testDraw(SpriteBatch spriteBatch)
        {
            if (roadNodes[3] != null)
            {
                testRectangle = new Rectangle((int)roadNodes[3].X, (int)roadNodes[3].Y, 2, 2);
            }
            if (testRectangle != null && testSquareSprite != null)
            { spriteBatch.Draw(testSquareSprite, testRectangle, Color.White); }            
        }

    }
}
