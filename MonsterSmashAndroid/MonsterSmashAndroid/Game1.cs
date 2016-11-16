using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Collections.Generic;

namespace CitySmash
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // initialize screen dimensions and scaling support
        AccelerometerListener accelListener;
        public float[] axisRotation = new float[3];
        public float actualWidth;
        public float actualHeight;

        float scaleX;
        float scaleY;
        int xOffset;
        int yOffset;
        float masterScale;
        Matrix scalingMatrix;

        // declare window dimensions
        public const int WINDOW_WIDTH = 1280;
        public const int WINDOW_HEIGHT = 720;

        // drawing support
        Texture2D debugRectangleSprite;
        Texture2D[] treeSprites = new Texture2D[10];
        Texture2D[] backgroundsSprites = new Texture2D[10];
        Texture2D[] rowhouseSprite = new Texture2D[10];
        List<GameObject> trees = new List<GameObject>();
        List<Background> backgrounds = new List<Background>();
        List<Critter> critters = new List<Critter>();
        List<AnimatedElement> explosions = new List<AnimatedElement>();
        List<Building> buildings = new List<Building>();
        List<Road> roads = new List<Road>();
        Monster0 monster;

        // map generation support
        GameObject[,] map = new GameObject[9, 64];
        Road.RoadWidth currentWidth = Road.RoadWidth.TwoLane;
        Road.RoadWidth nextWidth;
        Road mapitem = null;
        List<GameObject> holdList = new List<GameObject>();
        string spritename0;
        string prevIntersectionName = "blank";
        const int roadMapStep = 4;
        int whatWidthToBuild;
        int nextRow;
        int carCount = 0;
        int personCount = 0;

        // pedestrian spawning support
        Vector2[] holdNodes;
        int walkNodeCount;
        int spawnTimerLimit = 3000;
        int elapsedTimeSinceSpawn = 0;

        // collision support
        Vector2 moveVectorCheck;
        Rectangle rectangleCheck;
        Rectangle collisionRectangle;
        Rectangle[] carCrashCheck = new Rectangle[5];

        // occlusion/drawing depth support (basically a list of what order to draw things in)
        GameObject[] gameObjectsDrawList = new GameObject[1600];

        public Game1(ref AccelerometerListener accelListener)
        {
            this.accelListener = accelListener;
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content/";
            graphics.SupportedOrientations = DisplayOrientation.LandscapeRight; //| DisplayOrientation.LandscapeLeft

            // set window dimensions
            graphics.PreferredBackBufferWidth = WINDOW_WIDTH;
            graphics.PreferredBackBufferHeight = WINDOW_HEIGHT;
            IsFixedTimeStep = true;

            // clear axis values
            axisRotation[0] = 0;
            axisRotation[1] = 0;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // set window scaling
            scaleX = actualWidth / WINDOW_WIDTH;
            scaleY = actualHeight / WINDOW_HEIGHT;
            xOffset = 0;
            yOffset = 0;
            masterScale = 1f;
            // select the smaller scale; this keeps the scaling consistent and within the screen bounds
            if (scaleX > scaleY)
            {
                masterScale = scaleY;
                yOffset = (int)(actualHeight - (WINDOW_HEIGHT * masterScale)) / 2;
            } else {
                masterScale = scaleX;
                xOffset = (int)(actualWidth - (WINDOW_WIDTH * masterScale)) / 2;
            }
            // create a scaling matrix and adjust the viewport and drawing methods to scale
            scalingMatrix = Matrix.CreateScale(masterScale, masterScale, 1.0f);
            GraphicsDevice.Viewport = new Viewport(xOffset, yOffset, (int)(WINDOW_WIDTH * masterScale), (int)(WINDOW_HEIGHT * masterScale));

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // load sprites and objects
            debugRectangleSprite = Content.Load<Texture2D>("Graphics/TestSquare");
            rowhouseSprite[0] = Content.Load<Texture2D>("Graphics/Buildings/Red_SlimRowhouse");
            rowhouseSprite[1] = Content.Load<Texture2D>("Graphics/Buildings/Yellow_SlimRowhouse");
            rowhouseSprite[2] = Content.Load<Texture2D>("Graphics/Buildings/FancySlimRowhouse");
            rowhouseSprite[3] = Content.Load<Texture2D>("Graphics/Buildings/GreyTwoStorySlimRowhouse");
            rowhouseSprite[4] = Content.Load<Texture2D>("Graphics/Buildings/YellowTwoStorySlimRowhouse");
            rowhouseSprite[5] = Content.Load<Texture2D>("Graphics/Buildings/DoubleWideRowhouse");
            backgroundsSprites[0] = Content.Load<Texture2D>("Graphics/Background0");
            backgroundsSprites[1] = Content.Load<Texture2D>("Graphics/Background1");
            backgroundsSprites[2] = Content.Load<Texture2D>("Graphics/Background2");
            treeSprites[0] = Content.Load<Texture2D>("Graphics/Tree0");
            treeSprites[1] = Content.Load<Texture2D>("Graphics/Tree1");
            treeSprites[2] = Content.Load<Texture2D>("Graphics/Tree2");
            backgrounds.Add(new Background(new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT), Content.Load<Texture2D>("Graphics/Grass"), false));
            monster = new Monster0(Content, new Vector2(0, 0), debugRectangleSprite);

            // attach event code to monster/monster effects
            monster.AttackCollisionRectangleEvent += CollisionResolutionObject;

            //// clear drawlist
            //for (int i = 0; i < gameObjectsDrawList.Count(); i++)
            //{
            //    gameObjectsDrawList[i] = null;
            //}

            #region Initial Horizontal Road Generation

            // 0.0 generate the next width and next row offset
            nextWidth = GetRandomRoadWidth();
            nextRow = Rand.rand.Next(3, 5);

            // 1.0 loop through the map array's rows one at a time
            for (int i = 0; i < map.GetLength(0); i += nextRow)
            {
                // 2.1 set the current width, generate the next width, generate the next row number
                nextRow = Rand.rand.Next(3, 5);
                currentWidth = nextWidth;
                nextWidth = GetRandomRoadWidth();

                // 2.0 loop through the map array's columns one at a time
                for (int j = 0; j < map.GetLength(1); j += roadMapStep)
                {
                    // add in roads and intersections
                    // 3.1 make sure map location is empty
                    if (map[i, j] == null)
                    {
                        if (j != 0)
                        {
                            // 3.15 cast left object as Road type object
                            holdList.Clear();
                            holdList.Add(map[i, j - roadMapStep]);
                            mapitem = holdList.OfType<Road>().FirstOrDefault();
                        }
                        else { mapitem = null; }

                        // 3.2 if this is the first column or the left object is an intersection, add a road (or blank road spaces)
                        if (j == 0 ||

                            (mapitem != null && mapitem.Type == Road.RoadType.Intersection))
                        {
                            // 3.3 Spawn road and add it to the map. On the first row pick random lengths, after that fill in the spaces between intersections. Stop at the map's edge
                            for (int q = 0;
                                (j != map.GetLength(1)) && ((i == 0 && q < RandomRoadLength()) || (i != 0 && map[i, j] == null));
                                q++)
                            {
                                // 3.35 only add a sprite if the intersection offers a connection
                                if (mapitem == null || mapitem.RightSideBuilt != false)
                                {
                                    roads.Add(new Road(Content, GetRoadSprite(currentWidth), new Vector2(j * 80 / roadMapStep, i * 80), currentWidth, Road.RoadType.Road, true));
                                    for (int k = 0; k < roadMapStep; k++)
                                    {
                                        map[i, j + k] = roads.Last();
                                    }
                                }
                                j += roadMapStep;
                            }
                            j -= roadMapStep;
                        }
                        else
                        {

                            // 3.4 if the left object is blank or a road add an intersection to it
                            if (mapitem == null)
                            {
                                // 3.41 is the left object had no connect, spawn a random left-facing three-way intersection
                                spritename0 = GetIntersectionSprite(currentWidth, false);
                                roads.Add(new Road(Content, spritename0, new Vector2(j * 80 / roadMapStep, i * 80), currentWidth, Road.SidewalkFacing.Left));
                                for (int k = 0; k < roadMapStep; k++)
                                {
                                    map[i, j + k] = roads.Last();
                                }
                                // cast intersection down
                                SpawnRandomIntersection(i, j, nextRow, true, true, spritename0);
                            }
                            else
                            {
                                spritename0 = GetIntersectionSprite(currentWidth, true);
                                // Spawn a random intersection, using the appropriate constructor
                                SpawnRandomIntersection(i, j, 0, true, false, spritename0);
                                // cast intersection down
                                SpawnRandomIntersection(i, j, nextRow, true, true, spritename0);
                            }
                        }
                    }
                    else
                    {
                        // 4.1 cast current object as Road type object
                        holdList.Clear();
                        holdList.Add(map[i, j]);
                        mapitem = holdList.OfType<Road>().FirstOrDefault();
                        // 4.2 if road is an intersection cast it down
                        if (mapitem.Type == Road.RoadType.Intersection)
                        {
                            // 4.25 cast intersection down
                            SpawnRandomIntersection(i, j, nextRow, true, true, mapitem.Sprite.Name);

                        }
                    }
                }
            }

            #endregion

            #region Initial Vertical Road Generation

            // loop through map one row at a time (starting a the second row
            for (int i = 1; i < map.GetLength(0); i++)
            {
                //loop through map one column at a time
                for (int j = 0; j < map.GetLength(1); j += roadMapStep)
                {
                    // if map index is blank check to see if a road is needed
                    if (map[i, j] == null)
                    {
                        // cast upper object as Road type object
                        holdList.Clear();
                        holdList.Add(map[i - 1, j]);
                        mapitem = holdList.OfType<Road>().FirstOrDefault();

                        // if the object above is an intersection, add a road beneath it
                        if (mapitem != null &&
                            (mapitem.Type == Road.RoadType.Intersection ||
                            !mapitem.Horizontal))
                        {
                            // get the proper road width from the above object
                            if (mapitem.Type == Road.RoadType.Intersection)
                            {
                                if (mapitem.Sprite.Name.Contains("2x2") ||
                                    (mapitem.Sprite.Name.Contains("2x4") && mapitem.Width == Road.RoadWidth.FourLane))
                                {
                                    currentWidth = Road.RoadWidth.TwoLane;
                                } else {
                                    currentWidth = Road.RoadWidth.FourLane;
                                }
                            }

                            if (!mapitem.Horizontal)
                            {
                                currentWidth = mapitem.Width;
                            }

                            // spawn a new road and add it to the map
                            roads.Add(new Road(Content, GetRoadSprite(currentWidth), new Vector2(j * 80 / roadMapStep, i * 80), currentWidth, Road.RoadType.Road, false));
                            for (int k = 0; k < roadMapStep; k++)
                            {
                                map[i, j + k] = roads.Last();
                            }
                        }
                    }
                }
            }

            #endregion

            #region Initial Building Generation

            bool continuousBuilding = false;
            int prevSprite = 0;
            //loop through map rows
            for (int i = 1; i < map.GetLength(0); i++)
            {
                // loop through map columns
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    // if space is blank (and connects to a road), add a randomized rowhouse
                    if (map[i, j] == null &&
                        ((map[i - 1, j] != null && map[i - 1, j].GetType() == typeof(Road)) ||
                        (i + 1 < map.GetLength(0) && map[i + 1, j] != null && map[i + 1, j].GetType() == typeof(Road))))
                    {
                        // check previous sprite and spawn a new rowhouse
                        if (!continuousBuilding)
                        {
                            continuousBuilding = true;
                            prevSprite = Rand.rand.Next(0, 6);
                        } else {
                            if (prevSprite == 3 || prevSprite == 4)
                            {
                                prevSprite = Rand.rand.Next(0, 8);
                                if (prevSprite > 5) { prevSprite -= 3; }
                            }
                            else if (prevSprite == 5)
                            {
                                prevSprite = Rand.rand.Next(0, 8);
                                if (prevSprite > 5) { prevSprite = 5; }
                            }
                            else { prevSprite = Rand.rand.Next(0, 6); }
                        }
                        // make sure we are not spawning on top of a road
                        if (prevSprite > 4 && j + 1 < map.GetLength(1) && map[i, j + 1] != null) { prevSprite = Rand.rand.Next(0, 5); }
                        // set the direction the building is facing
                        bool facingUp;
                        if (map[i - 1, j] != null && map[i - 1, j].GetType() == typeof(Road))
                        {
                            facingUp = true;
                        }
                        else { facingUp = false; }
                        // spawn new rowhouse and add it to the map, spawn background textures to match
                        buildings.Add(new Building(Content, rowhouseSprite[prevSprite], new Vector2(j * 20, i * 80), facingUp));
                        Texture2D backgroundSpriteToUse;
                        if (prevSprite < 3 || (prevSprite == 5 && facingUp))
                        {
                            backgroundSpriteToUse = backgroundsSprites[0];
                        }
                        else if (prevSprite == 5 && !facingUp)
                        {
                            backgroundSpriteToUse = backgroundsSprites[2];
                        } else {
                            backgroundSpriteToUse = backgroundsSprites[1];
                        }
                        backgrounds.Add(new Background(new Rectangle((j * 20) - 25, (i * 80) - 20, 80, 120), backgroundSpriteToUse, facingUp));
                        map[i, j] = buildings.Last();
                        if (prevSprite == 5)
                        {
                            if (j + 1 < map.GetLength(1) - 1)
                            {
                                map[i, j + 1] = buildings.Last();
                                j += 1;
                            }
                        }
                    }
                    else { continuousBuilding = false; }
                }
            }

            #endregion

            #region Initial Pedestrian and Car Generation

            // loop through map elements and find roads
            Road prevRoad = null;
            Road holdRoad;
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    // check the map objects for roads
                    if (map[i, j] != null &&
                        map[i, j].GetType() == typeof(Road))
                    {
                        // cast the object to a road class
                        holdRoad = (Road)map[i, j];
                        if (prevRoad == null ||
                            holdRoad.DrawRectangle != prevRoad.DrawRectangle)
                        {
                            //check if the road is an intersection or not
                            if (holdRoad.Type == Road.RoadType.Road)
                            {
                                walkNodeCount = 2;
                                holdNodes = holdRoad.roadNodes;
                            } else {
                                walkNodeCount = 4;
                                holdNodes = holdRoad.intersectionNodes;
                            }
                            // loop through spawnpoints, spawn random pedestrians
                            for (int k = 0; k < holdNodes.Count(); k++)
                            {
                                int spawnSomeone = Rand.rand.Next(0, 10);
                                if (spawnSomeone == 0)
                                {
                                    // spawn pedestrians 
                                    if (k < walkNodeCount)
                                    {
                                        critters.Add(new Critter(Content, rowhouseSprite[0], holdNodes[k], Critter.CritterType.Person));
                                        critters.Last().NodeNumber = k;
                                        critters.Last().StartMapCoordinates = new Vector2(i, j);
                                        critters.Last().TargetMapCoordinates = critters.Last().StartMapCoordinates;
                                        critters.Last().Origin = critters.Last().StartMapCoordinates;
                                    } else {
                                        // spawn cars                               
                                        critters.Add(new Critter(Content, rowhouseSprite[0], holdNodes[k], Critter.CritterType.Car));
                                        critters.Last().NodeNumber = k;
                                        critters.Last().StartMapCoordinates = new Vector2(i, j);
                                        bool isRoad = false;
                                        if (holdRoad.Type == Road.RoadType.Road)
                                        {
                                            isRoad = true;
                                        }
                                        critters.Last().TargetMapCoordinates = GetNextNodeForCar(critters.Last().StartMapCoordinates, critters.Last().XMove, critters.Last().YMove, isRoad);
                                        critters.Last().Origin = critters.Last().StartMapCoordinates;
                                        // set rotation for cars spawned on roads
                                        if (holdRoad.Type == Road.RoadType.Road)
                                        {
                                            SpawnCarOnRoad(holdRoad, k);
                                        }
                                        // set rotation for cars spawned on intersections
                                        if (holdRoad.Type == Road.RoadType.Intersection)
                                        {
                                            SpawnCarOnIntersection(holdRoad, k);
                                        }
                                    }
                                }
                            }
                            prevRoad = holdRoad;
                        }
                    }
                }
            }
            // clean up bad spawns, too lazy to do this another way
            for (int i = critters.Count() - 1; i >= 0; i--)
            {
                // clean up spawns at map borders
                if (critters[i].Type == Critter.CritterType.Car &&
                    (critters[i].X < 10 ||
                    critters[i].X > WINDOW_WIDTH - 10 ||
                    critters[i].Y < 10 ||
                    critters[i].Y > WINDOW_HEIGHT - 10))
                {
                    critters[i].Active = false;
                } else {
                    // syntactic simplifaction and out-of-bounds checks
                    carCrashCheck[0] = map[(int)critters[i].StartMapCoordinates.X, (int)critters[i].StartMapCoordinates.Y].DrawRectangle;

                    // clean up spawn not intersecting roads
                    if (!critters[i].CarCollisionRectangle.Intersects(carCrashCheck[0]))
                    {
                        critters[i].Active = false;
                    }
                }
            }

            #endregion

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            //    Exit();
            //// get controler input
            //GamePadState gamepad = GamePad.GetState(PlayerIndex.One);            
            //MouseState mouse = Mouse.GetState();

            axisRotation[0] = (axisRotation[0] + accelListener.AccelReadings[1]) / 2;
            axisRotation[1] = (axisRotation[1] + 6 + accelListener.AccelReadings[0]) / 2;

            #region Updates and Animation

            // update game objects and their draw order
            gameObjectsDrawList[(int)Math.Round(monster.Location.Y / 80) * 100 + 49] = monster;

            monster.Update(gameTime, ref gameObjectsDrawList, -axisRotation[0], -axisRotation[1]);

            foreach (Building rowhouseX in buildings)
            {
                rowhouseX.Update(gameTime, ref gameObjectsDrawList);
                rowhouseX.DestructionUpdate(gameTime);
            }

            // TODO: limit to once per game, unless you add in scrolling
            foreach (Road roadX in roads)
            {
                roadX.Update(gameTime, ref gameObjectsDrawList);
            }

            // animate explosions and remove expired explosions
            for (int i = explosions.Count - 1; i >= 0; i--)
            {
                if (explosions[i].IsFinished)
                {
                    explosions.RemoveAt(i);
                }
                else { explosions[i].Update(gameTime); }
            }

            #endregion

            #region Critter Pathing

            // animate critters and remove squished/out of bounds critters
            for (int i = critters.Count() - 1; i >= 0; i--)
            {
                if (critters[i].X < -30 ||
                    critters[i].X > WINDOW_WIDTH + 30 ||
                    critters[i].Y < -30 ||
                    critters[i].Y > WINDOW_HEIGHT + 30 ||
                    critters[i].Squished ||
                    critters[i].Origin == new Vector2(0, 0))
                {
                    critters[i].Active = false;
                }
                else if (critters[i].TargetMapCoordinates == new Vector2())
                { critters[i].Active = false;
                }
                else
                {
                    // animate critter
                    critters[i].Animate(gameTime, ref gameObjectsDrawList);
                    // Some syntactic simplification, and resetting the start location
                    int a = (int)critters[i].TargetMapCoordinates.X;
                    int b = (int)critters[i].TargetMapCoordinates.Y;
                    int c = (int)critters[i].StartMapCoordinates.X;
                    int d = (int)critters[i].StartMapCoordinates.Y;
                    Road targetRoad = (Road)map[a, b];
                    Road startRoad = (Road)map[c, d];
                    int lastIntNode = 0;
                    int checkStart = 4;

                    #region Person Pathing

                    if (critters[i].Type == Critter.CritterType.Person)
                    {
                        if (targetRoad.Type == Road.RoadType.Road)
                        {
                            critters[i].NodeReached = false;

                            // check map elements to see what direction we can travel too
                            checkStart = Rand.rand.Next(0, 4);
                            SetMovemenForPerson(i, checkStart, a, b, critters[i].StartMapCoordinates);
                        }

                        // if the next target is an intersection then pick another node/road to travel to
                        if (targetRoad.Type == Road.RoadType.Intersection)
                        {

                            // check for a collision with the pathing nodes, if we have hit one then lock onto it and reset values
                            if (!critters[i].NodeReached)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (critters[i].PersonNodeCollisionRectangle.Intersects(new Rectangle((int)targetRoad.intersectionNodes[j].X, (int)targetRoad.intersectionNodes[j].Y, 1, 1)))
                                    {
                                        critters[i].NodeReached = true;
                                        lastIntNode = j;
                                        checkStart = Rand.rand.Next(0, 3);
                                        critters[i].X = (int)targetRoad.intersectionNodes[j].X - 10;
                                        critters[i].Y = (int)targetRoad.intersectionNodes[j].Y - 15;
                                        critters[i].XMove = 0;
                                        critters[i].YMove = 0;
                                    }
                                }
                            }

                            // reset values if critter has crossed intersection
                            if (critters[i].CrossingIntersection &&
                                critters[i].PersonNodeCollisionRectangle.Intersects(new Rectangle((int)targetRoad.intersectionNodes[critters[i].TargetNode].X, (int)targetRoad.intersectionNodes[critters[i].TargetNode].Y, 1, 1)))
                            {
                                critters[i].CrossingIntersection = false;
                                critters[i].X = (int)targetRoad.intersectionNodes[critters[i].TargetNode].X - 10;
                                critters[i].Y = (int)targetRoad.intersectionNodes[critters[i].TargetNode].Y - 15;
                                critters[i].XMove = 0;
                                critters[i].YMove = 0;
                                checkStart = 0;
                            }

                            // on a 1 or 2, cross the intersection
                            if (checkStart != 0 && checkStart != 4)
                            {
                                // loop through intersection walking nodes to find a collision
                                if (!critters[i].CrossingIntersection)
                                {
                                    //set intersection tracking to true
                                    critters[i].CrossingIntersection = true;

                                    // pick the next node to walk to
                                    if (lastIntNode == checkStart || lastIntNode == 3)
                                    {
                                        critters[i].TargetNode = lastIntNode - checkStart;
                                    }
                                    else { critters[i].TargetNode = lastIntNode + checkStart; }

                                    // add velocity
                                    if (checkStart == 1)
                                    {
                                        if (lastIntNode % 2 == 0)
                                        {
                                            critters[i].YMove = 1;
                                        }
                                        else { critters[i].YMove = -1; }
                                    }
                                    else if (checkStart == 2)
                                    {
                                        if (lastIntNode < 2)
                                        {
                                            critters[i].YMove = -1;
                                        }
                                        else { critters[i].YMove = 1; }
                                    }
                                    // update starting node number for horizontal roads
                                    if (startRoad.Horizontal && checkStart == 1)
                                    {
                                        if (critters[i].NodeNumber == 0)
                                        {
                                            critters[i].NodeNumber = 1;
                                        }
                                        else { critters[i].NodeNumber = 0; }
                                    }
                                    // update starting node for vertical nodes
                                    if (!startRoad.Horizontal && checkStart == 2)
                                    {
                                        if (critters[i].NodeNumber == 0)
                                        {
                                            critters[i].NodeNumber = 1;
                                        }
                                        else { critters[i].NodeNumber = 0; }
                                    }

                                    // TODO: implement a timer for letting cars pass

                                }
                            }
                            else if (checkStart == 0)
                            {
                                // move to the nearest road
                                SetMovemenForPerson(i, checkStart, a, b, critters[i].StartMapCoordinates);
                            }
                        }

                        // if the next target is in bounds and we've arrived at the target, set the target coordinates
                        if (a + critters[i].YMove >= 0 &&
                            a + critters[i].YMove < map.GetLength(0) &&
                            b + (4 * critters[i].XMove) >= 0 &&
                            b + (4 * critters[i].XMove) < map.GetLength(1) &&
                            !critters[i].CrossingIntersection &&
                            !(!critters[i].NodeReached && targetRoad.Type == Road.RoadType.Intersection) &&
                            critters[i].DrawRectangle.Intersects(targetRoad.DrawRectangle))
                        {
                            critters[i].StartMapCoordinates = critters[i].TargetMapCoordinates;
                            critters[i].TargetMapCoordinates = new Vector2(a + critters[i].YMove, b + (4 * critters[i].XMove));
                        }
                    }
                    #endregion

                    #region Car Pathing

                    if (critters[i].Type == Critter.CritterType.Car)
                    {
                        // move on to the next node if the car is on a road
                        if (targetRoad.Type == Road.RoadType.Road && critters[i].IntersectionChecking)
                        {
                            if (critters[i].DrawRectangle.Intersects(targetRoad.DrawRectangle))
                            {
                                critters[i].StartMapCoordinates = critters[i].TargetMapCoordinates;
                                critters[i].TargetMapCoordinates = GetNextNodeForCar(critters[i].TargetMapCoordinates, critters[i].XMove, critters[i].YMove, true);
                            }
                        }
                        // slow the car by one tick per second if it's nearing another car
                        if (!critters[i].SlowDownQueued)
                        {
                            carCrashCheck[0] = critters[i].DrawRectangle;
                            carCrashCheck[1] = critters[i].DrawRectangle;
                            carCrashCheck[2] = critters[i].DrawRectangle;
                            carCrashCheck[3] = critters[i].DrawRectangle;
                            carCrashCheck[1].X += 5 * critters[i].XMove;
                            carCrashCheck[1].Y += 5 * critters[i].YMove;
                            carCrashCheck[2].X += 15 * critters[i].XMove;
                            carCrashCheck[2].Y += 15 * critters[i].YMove;
                            carCrashCheck[3].X += 35 * critters[i].XMove;
                            carCrashCheck[3].Y += 35 * critters[i].YMove;
                            // loop thorugh the set of critters and check for collisions
                            bool collisionFound = false;
                            foreach (Critter critterx in critters)
                            {
                                if (critterx.Type == Critter.CritterType.Car &&
                                    !(Math.Abs(critterx.XMove) == Math.Abs(critters[i].XMove) && Math.Abs(critterx.YMove) == Math.Abs(critters[i].YMove)) &&
                                    !(critterx.XMove * critters[i].XMove < 0 || critterx.YMove * critters[i].YMove < 0))
                                {
                                    // slow the car down
                                    if (carCrashCheck[3].Intersects(critterx.DrawRectangle))
                                    {
                                        critters[i].SlowDownQueued = true;
                                    }
                                    // stop the car
                                    if (carCrashCheck[1].Intersects(critterx.DrawRectangle))
                                    {
                                        //critters[i].PauseCarMovement = true;
                                        critters[i].StoppedForCar = true;
                                        //collisionFound = true;
                                    }
                                }
                            }
                            if (!collisionFound) { critters[i].PauseCarMovement = false; }
                        }
                        // slow the car if it is going to hit an intersection
                        if (critters[i].IntersectionChecking &&
                            !critters[i].SlowDownQueued &&
                            targetRoad.Type == Road.RoadType.Intersection)
                        {
                            // slow the car down
                            if (carCrashCheck[2].Intersects(targetRoad.DrawRectangle) ||
                                carCrashCheck[3].Intersects(targetRoad.DrawRectangle))
                            {
                                critters[i].SlowDownQueued = true;
                            }
                            // stop the car
                            for (int k = 3; k < targetRoad.intersectionNodes.Count() - 1; k++)
                            {
                                carCrashCheck[4] = new Rectangle((int)targetRoad.intersectionNodes[k].X, (int)targetRoad.intersectionNodes[k].Y, 1, 1);
                                if (critters[i].DrawRectangle.Intersects(carCrashCheck[4]))
                                {
                                    critters[i].PauseCarMovement = true;
                                    critters[i].NodeNumber = k;
                                    critters[i].StoppedForInt = true;
                                    critters[i].StoppedForCar = false;
                                }
                            }
                        }
                    }

                    #endregion

                    #region Intersection Handler

                    if (critters[i].Type == Critter.CritterType.Car)
                    {
                        // turning support - for calculating the turnign angle
                        int opposite = 0;
                        int adjacent = 1;


                        // move cars through intersections
                        if (critters[i].StoppedForInt)
                        {
                            critters[i].StoppedForInt = false;
                            critters[i].PauseCarMovement = false;
                            critters[i].IntersectionChecking = false;
                            // set next target intersection
                            GetNextRoadForCarAtIntersection(i);
                            // set turn values for cars stopped on the right of the intersection
                            if (critters[i].NodeNumber == 10 || critters[i].NodeNumber == 11)
                            {
                                critters[i].TurnPoint = targetRoad.intersectionNodes[0];
                            }
                            // set turn values for cars stopped on the left of the intersection
                            if (critters[i].NodeNumber == 8 || critters[i].NodeNumber == 9)
                            {
                                critters[i].TurnPoint = targetRoad.intersectionNodes[3];
                                critters[i].TurnStartAngle = (float)Math.PI;
                                critters[i].XOffsetMultiplier = 20;
                            }
                            // set turn values for cars stopped on the top of the intersection
                            if (critters[i].NodeNumber == 4 || critters[i].NodeNumber == 5)
                            {
                                critters[i].TurnPoint = targetRoad.intersectionNodes[2];
                                critters[i].TurnStartAngle = -(float)Math.PI / 2;
                            }
                            // set turn values for cars stopped on the bottom of the intersection
                            if (critters[i].NodeNumber == 6 || critters[i].NodeNumber == 7)
                            {
                                critters[i].TurnPoint = targetRoad.intersectionNodes[1];
                                critters[i].TurnStartAngle = (float)Math.PI / 2;
                            }
                        }
                        // if a car in position, initiate the turn
                        if (critters[i].StartTurn &&
                            (critters[i].DrawRectangle.Center.X == critters[i].TurnPoint.X || critters[i].DrawRectangle.Center.Y == critters[i].TurnPoint.Y))
                        {
                            // calculate the distance from the turning point
                            opposite = critters[i].DrawRectangle.Center.Y - (int)critters[i].TurnPoint.Y;
                            adjacent = critters[i].DrawRectangle.Center.X - (int)critters[i].TurnPoint.X;
                            // calculate the radius of the turning circle and the starting angle
                            critters[i].TurnRadius = (float)Math.Sqrt(Math.Pow(opposite, 2) + Math.Pow(adjacent, 2));
                            // initiate the turn
                            critters[i].StartTurn = false;
                        }

                        // if a turn is done, lock the car on the correct path
                        if (critters[i].TurnDone)
                        {
                            critters[i].TurnDone = false;
                            for (int l = 4; l < startRoad.intersectionNodes.Count(); l++)
                            {
                                carCrashCheck[4] = new Rectangle(critters[i].DrawRectangle.X - 5, critters[i].DrawRectangle.Y, critters[i].Width + 10, critters[i].Height);
                                if (carCrashCheck[4].Intersects(new Rectangle((int)startRoad.intersectionNodes[l].X, (int)startRoad.intersectionNodes[l].Y, 3, 3)))
                                {
                                    int targetTurnNode;
                                    if (l == 4 || l == 5)
                                    {
                                        if (l == 4)
                                        {
                                            targetTurnNode = 10;
                                        }
                                        else { targetTurnNode = 11; }
                                        critters[i].X = (int)startRoad.intersectionNodes[targetTurnNode].X + 7;
                                        critters[i].Y = (int)startRoad.intersectionNodes[targetTurnNode].Y - 11;
                                        critters[i].XMove = -1;
                                        critters[i].FlipSpriteH = true;

                                    }
                                    else if (l == 6 || l == 7)
                                    {
                                        if (l == 6)
                                        {
                                            targetTurnNode = 8;
                                        }
                                        else { targetTurnNode = 9; }
                                        critters[i].X = (int)startRoad.intersectionNodes[targetTurnNode].X - 48;
                                        critters[i].Y = (int)startRoad.intersectionNodes[targetTurnNode].Y - 11;
                                        critters[i].XMove = 1;
                                    }
                                    else if (l == 10 || l == 11)
                                    {
                                        if (l == 10)
                                        {
                                            targetTurnNode = 6;
                                        }
                                        else { targetTurnNode = 7; }
                                        critters[i].X = (int)startRoad.intersectionNodes[targetTurnNode].X - 10;
                                        critters[i].Y = (int)startRoad.intersectionNodes[targetTurnNode].Y - 63;
                                        critters[i].YMove = -1;
                                        critters[i].FlipRotate = true;
                                        critters[i].Rotation = (float)Math.PI / 2;
                                    }
                                    else if (l == 8 || l == 9)
                                    {
                                        if (l == 8)
                                        {
                                            targetTurnNode = 4;
                                        }
                                        else { targetTurnNode = 5; }
                                        critters[i].X = (int)startRoad.intersectionNodes[targetTurnNode].X - 10;
                                        critters[i].Y = (int)startRoad.intersectionNodes[targetTurnNode].Y + 42;
                                        critters[i].YMove = 1;
                                        critters[i].IsRotated = false;
                                        critters[i].Rotation = (float)Math.PI / 2;
                                    }
                                    critters[i].RadiansTurned = 0;
                                    critters[i].TurnRadius = 0;
                                }
                            }
                        }
                    }

                    #endregion

                }
            }

            #endregion

            #region Critter Spawn

            // clean up inactive crittes and count numbers
            personCount = 0;
            carCount = 0;
            for (int i = critters.Count() - 1; i >= 0; i--)
            {
                // deactivate critters that have wandered off of pathing nodes
                bool onARoad = false;
                foreach (Road roadx in roads)
                {
                    if (!WithinMapRange(critters[i].X, critters[i].Y)) { onARoad = true; break; }
                    if (critters[i].DrawRectangle.Intersects(roadx.DrawRectangle))
                    {
                        onARoad = true;
                        break;
                    }
                }
                if (!onARoad) { critters[i].Active = false; }
                // count active critters
                if (critters[i].Type == Critter.CritterType.Person && critters[i].Active)
                {
                    if (critters[i].X < 0)
                    {
                        critters[i].Active = false;
                    }
                    else { personCount += 1; }
                }
                else if (critters[i].Active) { carCount += 1; }
                // remove inactive critters
                if (!critters[i].Active)
                {
                    critters[i].Animate(gameTime, ref gameObjectsDrawList);
                    critters.RemoveAt(i);
                }
            }

            // only run a spawn check once every three seconds, capping the critter count at 20
            if (elapsedTimeSinceSpawn < spawnTimerLimit)
            {
                elapsedTimeSinceSpawn += gameTime.ElapsedGameTime.Milliseconds;
            } else {
                elapsedTimeSinceSpawn = 0;
                // loop through map edges and search for nodes
                for (int i = 0; i < map.GetLength(0); i++)
                {
                    for (int j = 0; j < map.GetLength(1); j++)
                    {
                        // if this is not the top or bottom of the map, then skip the middle roads
                        if (i != 0 &&
                            i != map.GetLength(0) - 1 &&
                            j == 4)
                        {
                            j = map.GetLength(1) - 1;
                        }
                        // check current mapitem to look for roads (only) and nodes... chimchimcharodes
                        if (map[i, j] != null && map[i, j].GetType() == typeof(Road))
                        {
                            // set the number of nodes to iterate through
                            mapitem = (Road)map[i, j];
                            if (mapitem.Type == Road.RoadType.Road &&
                               !((i == 0 || i == map.GetLength(0) - 1) && (j != 0 && j != map.GetLength(1) - 1) && mapitem.Horizontal))
                            {
                                holdNodes = mapitem.roadNodes;
                                walkNodeCount = 2;
                                int xOffset = 0;
                                int yOffset = 0;
                                int xMove = 0;
                                int yMove = 0;
                                // loop through nodes and add pedestrians and cars as appropriate
                                for (int k = 0; k < holdNodes.Count(); k++)
                                {
                                    int spawnSomeone = Rand.rand.Next(0, 10);
                                    if (spawnSomeone == 0 && holdNodes[k].Y != 0)
                                    {
                                        // spawn pedestrians up to 20
                                        if (k < walkNodeCount &&
                                            personCount < 20 &&
                                            holdNodes[k].Y != 0)
                                        {
                                            // set movement and location on horizontal roads
                                            if (j == 0 && mapitem.Horizontal)
                                            {
                                                xOffset = -20;
                                                yOffset = (int)holdNodes[k].Y;
                                                xMove = 1;
                                            }
                                            else if (j == map.GetLength(1) - 1 && mapitem.Horizontal)
                                            {
                                                xOffset = WINDOW_WIDTH + 20;
                                                yOffset = (int)holdNodes[k].Y;
                                                xMove = -1;
                                            }
                                            // set movement and location on vertical nodes
                                            if (i == 0 && !mapitem.Horizontal)
                                            {
                                                yOffset = -20;
                                                xOffset = (int)holdNodes[k].X;
                                                yMove = 1;
                                            }
                                            else if (i == map.GetLength(0) - 1 && !mapitem.Horizontal)
                                            {
                                                yOffset = WINDOW_HEIGHT + 20;
                                                xOffset = (int)holdNodes[k].X;
                                                yMove = -1;
                                            }
                                            // spawn new critter
                                            Vector2 spawnPoint = new Vector2(xOffset, yOffset);
                                            critters.Add(new Critter(Content, rowhouseSprite[0], spawnPoint, Critter.CritterType.Person));
                                            critters.Last().NodeNumber = k;
                                            critters.Last().StartMapCoordinates = new Vector2(i, j);
                                            critters.Last().TargetMapCoordinates = critters.Last().StartMapCoordinates;
                                            critters.Last().Origin = critters.Last().StartMapCoordinates;
                                            critters.Last().XMove = xMove;
                                            critters.Last().YMove = yMove;
                                        }
                                        else if (carCount < 15 && holdNodes[k].Y != 0 && k >= 2)
                                        {
                                            // spawn new cars                      
                                            critters.Add(new Critter(Content, rowhouseSprite[0], holdNodes[k], Critter.CritterType.Car));
                                            critters.Last().NodeNumber = k;
                                            critters.Last().StartMapCoordinates = new Vector2(i, j);
                                            critters.Last().TargetMapCoordinates = GetNextNodeForCar(critters.Last().StartMapCoordinates, critters.Last().XMove, critters.Last().YMove, true);
                                            critters.Last().Origin = critters.Last().StartMapCoordinates;
                                            // set rotation for cars spawned on roads
                                            if (mapitem.Type == Road.RoadType.Road)
                                            {
                                                SpawnCarOnRoad(mapitem, k);
                                            }
                                            foreach (Critter critterx in critters)
                                            {
                                                if (critterx.DrawRectangle.Intersects(critters.Last().DrawRectangle) && critterx.Origin.X != critters.Last().Origin.X)
                                                {
                                                    critters.Last().Active = false;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            #region TestingCode

            //// test object spawning using mouse
            //if (mouse.LeftButton == ButtonState.Released &&
            //    prevMouseState == ButtonState.Pressed)
            //{
            //    trees.Add(new GameObject(treeSprites[Rand.rand.Next(0, 3)], new Vector2(mouse.X, mouse.Y)));
            //    trees.Last().Width = 40;
            //    trees.Last().Height = 60;
            //    trees.Last().X = mouse.X - 20;
            //    trees.Last().Y = mouse.Y - 30;
            //    trees.Last().Opacity = 1f;
            //    //critters.Add( new Critter(Content, rowhouseSprite[0], new Vector2(mouse.X, mouse.Y), Critter.CritterType.Car) );
            //    //explosions.Add(new AnimatedElement(Content, new Vector2(mouse.X, mouse.Y), AnimatedElement.AnimationType.Fire));
            //}
            //prevMouseState = mouse.LeftButton;

            //// test rotation
            //if (gamepad.Buttons.A == ButtonState.Pressed)
            //{
            //    monster.Rotation += (float)Math.PI / 60;
            //    monster.IsRotated = false;
            //} else {
            //    monster.Rotation = 0;
            //    monster.IsRotated = true;
            //}

            #endregion

            #region Collision and Occlusion Checks

            // attatch collision method the ripple effects
            foreach (AnimatedElement elementx in monster.DestructionEffects)
            {
                if (elementx.Type == AnimatedElement.AnimationType.Ripple && !elementx.eventThreaded)
                {
                    elementx.StepCollisionRectangleEvent += CritterCollisionResolution;
                    elementx.eventThreaded = true;
                }
            }

            // check for collisions, if path is clear then move teddy
            if (monster.AnimationState != Monster0.MonsterAnimationState.Attacking)
            {
                // compute movement and new location
                moveVectorCheck = new Vector2((-axisRotation[0] * gameTime.ElapsedGameTime.Milliseconds * 0.1f),
                                    (-axisRotation[1] * gameTime.ElapsedGameTime.Milliseconds * 0.1f));
                rectangleCheck = new Rectangle(monster.CollisionRectangle.X + (int)moveVectorCheck.X, monster.CollisionRectangle.Y + (int)moveVectorCheck.Y,
                                                monster.CollisionRectangle.Width, monster.CollisionRectangle.Height);

                // check for collisions
                collisionRectangle = CollisionResolutionObject(rectangleCheck, monster.CollisionRectangle, 0);
                float yDiff = collisionRectangle.Y - monster.CollisionRectangle.Center.Y;
                float xDiff = collisionRectangle.X - monster.CollisionRectangle.Center.X;

                if ((collisionRectangle.Width == 0 && collisionRectangle.Height == 0) ||
                    (yDiff / Math.Abs(yDiff) != moveVectorCheck.Y / Math.Abs(moveVectorCheck.Y) &&
                    xDiff / Math.Abs(xDiff) != moveVectorCheck.Y / Math.Abs(moveVectorCheck.X)))
                {
                    monster.Move(moveVectorCheck.X, moveVectorCheck.Y);
                } else {
                    // initiate attack animation
                    if (monster.AnimationState != Monster0.MonsterAnimationState.Walking &&
                        (moveVectorCheck.X != 0 || moveVectorCheck.Y != 0))
                    {
                        monster.AnimationState = Monster0.MonsterAnimationState.Attacking;
                    }
                }
            }

            #endregion

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin(transformMatrix: scalingMatrix);

            //draw background
            foreach (Background backgoundx in backgrounds)
            {
                backgoundx.Draw(spriteBatch);
            }

            //draw game object
            foreach (GameObject gameObject in gameObjectsDrawList)
            {
                if (gameObject != null && gameObject.Active)
                {
                    gameObject.Draw(spriteBatch);
                }
            }

            // draw explosions
            foreach (AnimatedElement explosionx in explosions)
            {
                explosionx.Draw(spriteBatch);
            }

            //// test drawing
            //foreach (Road roadx in roads)
            //{
            //    roadx.testDraw(spriteBatch);
            //}

            //foreach (Critter critterx in critters)
            //{
            //    if (critterx.Type == Critter.CritterType.Car)
            //    {
            //        spriteBatch.Draw(debugRectangleSprite, critterx.DrawRectangle, Color.White);
            //    }
            //}

            foreach (GameObject treex in trees)
            {
                treex.Draw(spriteBatch);
            }

            ////DEBUG: show collision boxes
            //spriteBatch.Draw(debugRectangleSprite, monster.NewDrawRectangle, Color.Black);
            //spriteBatch.Draw(debugRectangleSprite, monster.DrawRectangle, Color.Red);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Private Methods

        /// <summary>
        /// Search the set of rowhouses for a potential collision
        /// </summary>
        /// <param name="drawRectangle">the rectangle that is being checked against the rowhouses</param>
        /// <returns></returns>
        private Rectangle CollisionResolutionObject(Rectangle drawRectangleUpdate, Rectangle drawRectangleCurrent, int currentDamage)
        {
            // define return and check variable            
            Rectangle collisionRectangleX = new Rectangle(0, 0, 0, 0);
            Rectangle collisionRectangleHolder;

            //check rowhouses for collisions
            foreach (Building houseX in buildings)
            {
                //  if we are walking, check collisions against the whole building
                if (monster.AnimationState != Monster0.MonsterAnimationState.Attacking)
                {
                    // check for occlusion
                    if (!houseX.Exploded)
                    {
                        Rectangle occlusionCheck = new Rectangle(monster.CollisionRectangle.X, monster.CollisionRectangle.Y + 10, monster.CollisionRectangle.Width, monster.CollisionRectangle.Height - 10);
                        if (houseX.DrawRectangle.Intersects(occlusionCheck) && houseX.DrawRectangle.Center.Y > monster.DrawRectangle.Y)
                        {
                            houseX.IsOverlapping = true;
                        }
                        else
                        {
                            houseX.IsOverlapping = false;
                        }
                    }
                    else { houseX.IsOverlapping = false; }

                    // check for collision
                    collisionRectangleHolder = Rectangle.Intersect(houseX.CollisionRectangle, drawRectangleUpdate);
                    if (collisionRectangleHolder.Height * collisionRectangleHolder.Width > collisionRectangleX.Height * collisionRectangleX.Width)
                    {
                        collisionRectangleX = collisionRectangleHolder;
                    }
                }

                // if we are attacking, check using the rowhouse base
                if (monster.AnimationState == Monster0.MonsterAnimationState.Attacking)
                {
                    collisionRectangleHolder = Rectangle.Intersect(houseX.DrawRectangle, drawRectangleUpdate);
                    collisionRectangleX = collisionRectangleHolder;
                    // explosion spawning code
                    if (!houseX.Exploded && !houseX.Hit &&
                        collisionRectangleX.Width > 0 &&
                        collisionRectangleX.Height > 0)
                    {
                        explosions.Add(new AnimatedElement(Content, new Vector2(collisionRectangleX.Center.X, collisionRectangleX.Center.Y), AnimatedElement.AnimationType.Explosion1));
                        houseX.DamageTaken += currentDamage;
                        //monster.ExplosionSpawned = true;
                    }
                }
            }
            return collisionRectangleX;
        }

        /// <summary>
        /// Search rowhouses and critters for collision and destroy them (thread through ripple animations)
        /// </summary>
        /// <param name="rippleCollisionRectangle">the rectangle of the ripple doing the damage</param>
        /// <param name="objectRectangle">added for convenience, wholly ancilliary</param>
        /// <param name="Damage">the damage stat of the ripple</param>
        /// <returns></returns>
        private Rectangle CritterCollisionResolution(Rectangle rippleCollisionRectangle, Rectangle objectRectangle, int currentDamage)
        {
            Rectangle emptyRectangle = new Rectangle();
            foreach (Critter critterx in critters)
            {
                if (rippleCollisionRectangle.Intersects(critterx.DrawRectangle))
                {
                    critterx.Squished = true;
                }
            }

            foreach (Building buildingx in buildings)
            {
                if (buildingx.DamageResolved && rippleCollisionRectangle.Intersects(buildingx.DrawRectangle))
                {
                    buildingx.DamageTaken += currentDamage;
                }
            }

            return emptyRectangle;
        }

        /// <summary>
        /// Get the road sprite name based on the dimensions
        /// </summary>
        /// <param name="width">The width of the road</param>
        /// <returns></returns>
        private string GetRoadSprite(Road.RoadWidth width)
        {
            String useThisSprite = "";

            if (width == Road.RoadWidth.TwoLane)
            {
                useThisSprite = "Tile/2LaneTile";
            }
            else
            {
                useThisSprite = "Tile/4LaneTile";
            }

            return useThisSprite;
        }

        /// <summary>
        /// Get the intersection sprite name based on the dimension
        /// </summary>
        /// <param name="width">>The width of the road</param>
        /// <param name="leftSideBuilt">True if there is a road left of the current road</param>
        /// <returns></returns>
        private string GetIntersectionSprite(Road.RoadWidth width, bool leftSideBuilt)
        {
            // string name supprt
            string intersectionSprite = "Tile/";
            int verticalLanes = Rand.rand.Next(1, 3);
            int connections;
            int fourLanetop = Rand.rand.Next(1, 4);

            // set whether the intersection is a 3-way or a 4-way
            if (leftSideBuilt == false)
            {
                connections = 3;
            }
            else { connections = Rand.rand.Next(1, 4); }

            // add to the string name: the horizontal road width (4x2 lane roads will be rotated)
            if (width == Road.RoadWidth.TwoLane || fourLanetop != 1)
            {
                intersectionSprite += "2x";
            }
            else
            {
                intersectionSprite += "4x";
            }

            // add to the string name: the vertical road width (4x2 lane roads will be rotated)
            if (fourLanetop == 1 ||
                width == Road.RoadWidth.FourLane)
            {
                intersectionSprite += "4";
            }
            else
            {
                intersectionSprite += "2";
            }

            // add to the string name: if this is a 3-way
            if (connections == 3 &&
                currentWidth == Road.RoadWidth.TwoLane)
            {
                if (intersectionSprite == "Tile/2x4")
                {
                    intersectionSprite += "-2Three";
                }
                else
                {
                    intersectionSprite += "Three";
                }
            }
            else
            {
                intersectionSprite += "Four";
            }

            intersectionSprite += "Tile";

            return intersectionSprite;
        }

        /// <summary>
        /// Get the intersection sprite being cast down
        /// </summary>
        /// <param name="leftSideBuilt">True if there is a road left of the current road</param>
        /// <param name="spritename">The sprite's name as a string</param>
        /// <returns></returns>
        private string GetCastDownIntersectionSprite(bool leftSideBuilt, string spritename)
        {
            // string name supprt
            string intersectionSprite = "Tile/";
            int connections;

            if (leftSideBuilt == false)
            {
                connections = 3;
            }
            else
            {
                connections = Rand.rand.Next(1, 4);
            }

            // process two lane roads by checking the current width and the spritename of the above intersection
            if (nextWidth == Road.RoadWidth.TwoLane)
            {
                intersectionSprite += "2x";

                if (spritename.Contains("x2") ||
                    (currentWidth == Road.RoadWidth.FourLane &&
                    spritename.Contains("2x")))
                {
                    intersectionSprite += "2";
                }
                else { intersectionSprite += "4"; }
            }

            // process four lane roads by checking the current width and the spritename of the above intersection
            if (nextWidth == Road.RoadWidth.FourLane)
            {
                if ((currentWidth == Road.RoadWidth.TwoLane &&
                    spritename.Contains("x4"))
                    ||
                    (currentWidth == Road.RoadWidth.FourLane &&
                    spritename.Contains("4x")))
                {
                    intersectionSprite += "4x4";
                }
                else { intersectionSprite += "2x4"; }
            }

            if (connections == 3 && nextWidth == Road.RoadWidth.TwoLane)
            {
                if (intersectionSprite.Contains("2x4"))
                {
                    intersectionSprite += "-2";
                }
                intersectionSprite += "Three";
            }
            else
            {
                intersectionSprite += "Four";
            }

            intersectionSprite += "Tile";

            return intersectionSprite;
        }

        /// <summary>
        /// Spawn random intersection
        /// </summary>
        /// <param name="i">The current row being looped through</param>
        /// <param name="j">The current column being looped through</param>
        /// <param name="ioffset">The distance to the next row being used</param>
        /// <param name="isleftSideBuilt">True if there is a road left of the current road</param>
        /// <param name="castdown">Whether or not this sprite is being cast down</param>
        /// <param name="spritename">The sprite's name as a string</param>
        private void SpawnRandomIntersection(int i, int j, int ioffset, bool isleftSideBuilt, bool castdown, string spritename)
        {
            //road generation support
            Road.RoadWidth inputWidth;



            if (castdown)
            {
                // check the previous castdown intersection and adjust the leftside built variable
                if (prevIntersectionName.Contains("Three") &&
                    j > (7 * map.GetLength(1) / 16))
                {
                    isleftSideBuilt = false;
                }
                else
                {
                    isleftSideBuilt = true;
                }

                spritename0 = GetCastDownIntersectionSprite(isleftSideBuilt, spritename);
                prevIntersectionName = spritename0;
                inputWidth = nextWidth;
            }
            else
            {
                spritename0 = spritename;
                inputWidth = currentWidth;
            }

            // check conditions to select the correct constructor the three cases are, build a random intersection, spawn a cast down random intersection,
            //or spawn a cast-down three-way intersection
            if (!isleftSideBuilt)
            {
                if (i + ioffset < map.GetLength(0))
                {
                    roads.Add(new Road(Content, spritename0, new Vector2(j * 80 / roadMapStep, (i + ioffset) * 80), inputWidth, Road.SidewalkFacing.Left));
                    for (int k = 0; k < roadMapStep; k++)
                    {
                        map[i + ioffset, j + k] = roads.Last();
                    }
                }
            }
            else if (spritename0.Contains("Three") &&
                !(spritename0.Contains("4") && currentWidth == Road.RoadWidth.TwoLane))
            {
                if (i + ioffset < map.GetLength(0))
                {
                    roads.Add(new Road(Content, spritename0, new Vector2(j * 80 / roadMapStep, (i + ioffset) * 80), inputWidth, Road.SidewalkFacing.Right));
                    for (int k = 0; k < roadMapStep; k++)
                    {
                        map[i + ioffset, j + k] = roads.Last();
                    }
                }
            }
            else
            {
                if (i + ioffset < map.GetLength(0))
                {
                    roads.Add(new Road(Content, spritename0, new Vector2(j * 80 / roadMapStep, (i + ioffset) * 80), inputWidth, Road.RoadType.Intersection, true));
                    for (int k = 0; k < roadMapStep; k++)
                    {
                        map[i + ioffset, j + k] = roads.Last();
                    }
                }
            }
        }

        /// <summary>
        /// generate a random road width
        /// </summary>
        /// <returns></returns>
        private Road.RoadWidth GetRandomRoadWidth()
        {
            Road.RoadWidth randomRoadWidth;
            whatWidthToBuild = Rand.rand.Next(1, 6);
            if (whatWidthToBuild == 1)
            {
                randomRoadWidth = Road.RoadWidth.FourLane;
            }
            else
            {
                randomRoadWidth = Road.RoadWidth.TwoLane;
            }
            return randomRoadWidth;
        }

        /// <summary>
        /// Generate a random roadlength
        /// </summary>
        /// <returns></returns>
        private int RandomRoadLength()
        {
            int roadlength = 0;
            int lengthPick = Rand.rand.Next(1, 4);
            if (lengthPick == 1)
            {
                roadlength = 6;
            }
            else { roadlength = 5; }

            return roadlength;
        }

        /// <summary>
        /// Set the next road for a critter to walk to
        /// </summary>
        /// <param name="i">The index of the critter in the critters list</param>
        /// <param name="checkStart">the integer to start the loop on</param>
        /// <param name="a">the first map dimension</param>
        /// <param name="b">the second map dimension</param>
        private void SetMovemenForPerson(int i, int checkStart, int a, int b, Vector2 startMapCoordinates)
        {
            while (critters[i].XMove == 0 && critters[i].YMove == 0)
            {
                // loop through direction and check if there is a road to travel to, then set the movement value 
                switch (checkStart)
                {
                    case 0:
                        if (a + 1 < (map.GetLength(0)) &&
                            map[a + 1, b] != null &&
                            map[a + 1, b].GetType() == typeof(Road) &&
                            new Vector2(a + 1, b) != startMapCoordinates)
                        {
                            critters[i].YMove = 1;
                        }
                        break;
                    case 1:
                        if (a - 1 >= 0 &&
                            map[a - 1, b] != null &&
                            map[a - 1, b].GetType() == typeof(Road) &&
                            new Vector2(a - 1, b) != startMapCoordinates)
                        {
                            critters[i].YMove = -1;
                        }
                        break;
                    case 2:
                        if (b + 4 < (map.GetLength(1)) &&
                            map[a, b + 4] != null &&
                            map[a, b + 4].GetType() == typeof(Road) &&
                            new Vector2(a, b + 4) != startMapCoordinates)
                        {
                            critters[i].XMove = 1;
                        }
                        break;
                    case 3:
                        if (b - 4 >= 0 &&
                            map[a, b - 4] != null &&
                            map[a, b - 4].GetType() == typeof(Road) &&
                            new Vector2(a, b - 4) != startMapCoordinates)
                        {
                            critters[i].XMove = -1;
                        }
                        break;
                }
                // loop through all directions                            
                if (checkStart == 3)
                {
                    checkStart = 0;
                }
                else { checkStart++; }
            }
        }

        /// <summary>
        /// Get the next road the car is moving to
        /// </summary>
        /// <param name="currentCoords">The current location of the critter</param>
        /// <param name="xMove">The x-vector movement of the critter</param>
        /// <param name="yMove">The y-vector movement of the critter</param>
        /// <param name="isRoad">True if the current location is a road</param>
        /// <returns></returns>
        private Vector2 GetNextNodeForCar(Vector2 currentCoords, int xMove, int yMove, bool isRoad)
        {
            Vector2 newTarget = currentCoords;

            if (isRoad)
            {
                int xOffset = 0;
                int yOffset = 0;
                if (yMove != 0)
                {
                    yOffset = yMove / Math.Abs(yMove);
                }
                if (xMove != 0)
                {
                    xOffset = 4 * (xMove / Math.Abs(xMove));
                }
                if (WithinMapRange((int)currentCoords.X + yOffset, (int)currentCoords.Y + xOffset) &&
                    map[(int)currentCoords.X + yOffset, (int)currentCoords.Y + xOffset] != null)
                {
                    newTarget = new Vector2(currentCoords.X + yOffset, currentCoords.Y + xOffset);
                }
            }

            return newTarget;
        }

        /// <summary>
        /// Decide where to turn the car whe it is stopped at an intersection
        /// </summary>
        /// <param name="indexOfCar">the index car in the critters array</param>
        private void GetNextRoadForCarAtIntersection(int indexOfCar)
        {
            int xVelocity = 0;
            int yVelocity = 0;
            if (critters[indexOfCar].NodeNumber > 7)
            {
                xVelocity = (int)Math.Pow(-1, Math.Floor((double)critters[indexOfCar].NodeNumber / 2));
            }
            else
            {
                yVelocity = (int)Math.Pow(-1, Math.Floor((double)critters[indexOfCar].NodeNumber / 2));
            }

            if (WithinMapRange((int)critters[indexOfCar].TargetMapCoordinates.X + yVelocity, (int)critters[indexOfCar].TargetMapCoordinates.Y + (4 * xVelocity)) &&
                map[(int)critters[indexOfCar].TargetMapCoordinates.X + yVelocity, (int)critters[indexOfCar].TargetMapCoordinates.Y + (4 * xVelocity)] == null)
            {
                if (WithinMapRange((int)critters[indexOfCar].TargetMapCoordinates.X + xVelocity, (int)critters[indexOfCar].TargetMapCoordinates.Y - (4 * yVelocity)))
                {
                    critters[indexOfCar].StartMapCoordinates = critters[indexOfCar].TargetMapCoordinates;
                    critters[indexOfCar].TargetMapCoordinates = new Vector2(critters[indexOfCar].TargetMapCoordinates.X + xVelocity, critters[indexOfCar].TargetMapCoordinates.Y - (4 * yVelocity));
                }
                critters[indexOfCar].XMove = -yVelocity;
                critters[indexOfCar].YMove = xVelocity;
                critters[indexOfCar].TurnDirection = GameObject.Turn.Right;
                critters[indexOfCar].StartTurn = true;
            }
            else
            {
                if (WithinMapRange((int)critters[indexOfCar].TargetMapCoordinates.X + yVelocity, (int)critters[indexOfCar].TargetMapCoordinates.Y + (4 * xVelocity)))
                {
                    critters[indexOfCar].StartMapCoordinates = critters[indexOfCar].TargetMapCoordinates;
                    critters[indexOfCar].TargetMapCoordinates = new Vector2(critters[indexOfCar].TargetMapCoordinates.X + yVelocity, critters[indexOfCar].TargetMapCoordinates.Y + (4 * xVelocity));
                }
                critters[indexOfCar].TurnDirection = GameObject.Turn.Straight;
                critters[indexOfCar].SpeedUpQueued = true;
            }

            critters[indexOfCar].IntersectionChecking = false;
        }

        /// <summary>
        /// check to see if the arguments are inside the map
        /// </summary>
        /// <param name="i">the first dimension of the map coordinate</param>
        /// <param name="j">the second dimension of the map coordinate</param>
        /// <returns></returns>
        private bool WithinMapRange(int i, int j)
        {
            if (i >= 0 &&
                i < map.GetLength(0) &&
                j >= 0 &&
                j < map.GetLength(1))
            {
                return true;
            }
            else { return false; }
        }

        /// <summary>
        /// Spawn a car using a road and one of its spawn/collision nodes
        /// </summary>
        /// <param name="spawnRoad">toe road to spawn on</param>
        /// <param name="k">the node to spawn on</param>
        private void SpawnCarOnRoad(Road spawnRoad, int k)
        {
            if (spawnRoad.Horizontal)
            {
                if (k == 4 || k == 5)
                {
                    critters.Last().X += 10;
                    critters.Last().FlipSpriteH = true;
                    critters.Last().XMove = -3;
                }
                else if (k == 2 || k == 3)
                {
                    critters.Last().X += 10;
                    critters.Last().FlipSpriteH = false;
                    critters.Last().XMove = 3;
                }
            }
            if (!spawnRoad.Horizontal)
            {
                if (k < 4)
                {
                    critters.Last().YMove = -3;
                    critters.Last().X += 2;
                    critters.Last().Y += 15;
                    critters.Last().FlipRotate = true;
                    critters.Last().Rotation = (float)Math.PI * 3 / 2;
                }
                else if (k < 6)
                {
                    critters.Last().YMove = 3;
                    critters.Last().Y += 15;
                    critters.Last().X += 3;
                    critters.Last().IsRotated = false;
                    critters.Last().Rotation = (float)Math.PI / 2;
                }
            }
        }

        /// <summary>
        /// Spawn a car using an intersection and one of its spawn/collision nodes
        /// </summary>
        /// <param name="spawnRoad">toe road to spawn on</param>
        /// <param name="k">the node to spawn on</param>
        private void SpawnCarOnIntersection(Road spawnRoad, int k)
        {
            if (k == 4 || k == 5)
            {
                critters.Last().YMove = 3;
                critters.Last().Y -= 10;
                critters.Last().X += 3;
                critters.Last().IsRotated = false;
                critters.Last().Rotation = (float)Math.PI / 2;
            }
            else if (k == 6 || k == 7)
            {
                critters.Last().YMove = -3;
                critters.Last().Y += 10;
                critters.Last().X += 3;
                critters.Last().FlipRotate = true;
                critters.Last().Rotation = (float)Math.PI * 3 / 2;
            }
            else if (k == 10 || k == 11)
            {
                critters.Last().XMove = -3;
                critters.Last().X += 20;
                critters.Last().FlipSpriteH = true;
            }
            else if (k == 8 || k == 9)
            {
                critters.Last().XMove = 3;
                critters.Last().X -= 35;
            }
        }

        #endregion

    }
}
