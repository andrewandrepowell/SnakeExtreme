using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Content;
using MonoGame.Extended.Serialization;
using MonoGame.Extended.Sprites;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended;
using System.Collections.ObjectModel;


namespace SnakeExtreme
{
    public interface IObject
    {
        public int Priority { get; set; }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState);
        public void StrictUpdate();
        public void Draw(SpriteBatch spriteBatch);
    }
    public interface ITangible
    {
        public Vector2 Position { get; set; }
        public Size Size { get; }
    }
    public interface ILevelObject
    {
        public Point LevelPosition { get; set; }
    }
    public class Snake : IObject, ITangible, ILevelObject
    {
        private readonly List<SnakeBody> bodies = new();
        private readonly ContentManager content;        
        private bool growTail = false;
        private Directions trueDirection = Directions.Up;
        public Snake(ContentManager content)
        {            
            this.content = content;
        }
        public readonly static ReadOnlyDictionary<Directions, Point> DirectionPoints = new(new Dictionary<Directions, Point>()
        {
            { Directions.Up, new Point(0, -1) },
            { Directions.Down, new Point(0, 1) },
            { Directions.Left, new Point(-1, 0) },
            { Directions.Right, new Point(1, 0) },
        });
        public enum Directions { Up, Down, Left, Right }
        public enum States { Normal, Appear, Move, Vanish, Gone }
        public States State { get; private set; } = States.Normal;
        public Directions Direction 
        {
            get => trueDirection;
            set
            {
                if (Headless || 
                    Count == 1 || 
                    (value == Directions.Up && Direction != Directions.Down) ||
                    (value == Directions.Down && Direction != Directions.Up) ||
                    (value == Directions.Left && Direction != Directions.Right) ||
                    (value == Directions.Right && Direction != Directions.Left))
                {
                    trueDirection = value;
                }
            }
        } 
        public void Clear()
        {
            Debug.Assert(State == States.Gone);
            bodies.Clear();
            State = States.Normal;
        }
        public int Count { get => bodies.Count; }
        public bool Headless { get => Count == 0; }
        public SnakeBody Head { get => bodies[0]; }
        public SnakeBody Tail { get => bodies.Last(); }
        public bool NewTailAvailable { get; private set; } = false;
        public IEnumerable<SnakeBody> Bodies { get => bodies; }
        public void CreateHead(Point startLevelPosition)
        {
            Debug.Assert(Headless);            
            bodies.Add(new SnakeBody(content) { LevelPosition = startLevelPosition });
            Head.LongAppear();
            State = States.Appear;
        }
        public void Move(bool growTail = false)
        {
            Debug.Assert(!Headless);
            Debug.Assert(State == States.Normal);
            Debug.Assert(bodies.All(x => x.State == Ball.States.Normal));
            this.growTail = growTail;
            foreach (var body in bodies)
                body.QuickVanish();
            State = States.Move;
        }
        public void Vanish()
        {
            Debug.Assert(!Headless);
            Debug.Assert(State == States.Normal);
            Debug.Assert(bodies.All(x => x.State == Ball.States.Normal));
            foreach (var body in bodies)
                body.LongVanish();
            State = States.Vanish;
        }
        public Vector2 Position
        {
            get => Head.Position;
            set => throw new NotImplementedException();
        }
        public Point LevelPosition
        {
            get => Head.LevelPosition;
            set => throw new NotImplementedException();
        }
        public Size Size { get => Head.Size; }
        public int Priority { get => (Headless ? 0 : (int)Position.Y); set => throw new NotImplementedException(); }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {
            if (State == States.Move && bodies.All(x=>x.State == Ball.States.Invisible))
            {
                if (growTail)
                {
                    bodies.Add(new SnakeBody(content));
                    NewTailAvailable = true;
                }
                for (int i = bodies.Count - 1; i >= 0; i--)
                {                    
                    if (i == 0)
                        bodies[i].LevelPosition += DirectionPoints[Direction];
                    else
                        bodies[i].LevelPosition = bodies[i - 1].LevelPosition;
                    if (growTail && i == bodies.Count - 1)
                    {
                        bodies[i].LongAppear();
                        bodies[i].UpdateFloatHeight(bodies[i - 1]);
                    }
                    else
                    {
                        bodies[i].QuickAppear();
                    }
                }
            }
            else
            {
                NewTailAvailable = false;
            }
            if ((State == States.Move || State == States.Appear) && bodies.All(x=>x.State == Ball.States.Normal))
            {
                State = States.Normal;
            }
            if (State == States.Vanish && bodies.All(x => x.State == Ball.States.Invisible))
            {
                State = States.Gone;
            }
        }
        public void StrictUpdate()
        {            
        }
        public void Draw(SpriteBatch spriteBatch)
        {            
        }
    }
    public class SnakeBody : IObject, ITangible, ILevelObject
    {
        private const int ballId = 3;
        private readonly Ball ball;
        private Point trueLevelPosition;
        public SnakeBody(ContentManager content)
        {            
            ball = new Ball(content, ballId);
        }
        public Ball.States State { get => ball.State; }
        public void QuickVanish() => ball.QuickVanish();
        public void QuickAppear() => ball.QuickAppear();
        public void LongAppear() => ball.LongAppear();
        public void LongVanish() => ball.LongVanish();
        public void UpdateFloatHeight(SnakeBody body) => ball.UpdateFloatHeight(body.ball);
        public int Priority { get => (int)Position.Y; set => throw new NotImplementedException(); }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {            
            ball.Update(gameTime, mouseState, keyboardState);
        }
        public void StrictUpdate()
        {
            ball.StrictUpdate();
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            ball.Draw(spriteBatch);
        }
        public Vector2 Position 
        { 
            get => ball.Position; 
            set => throw new NotImplementedException(); 
        }
        public Size Size { get => ball.Size; }
        public Point LevelPosition 
        {
            get => trueLevelPosition;
            set
            {
                trueLevelPosition = value;
                ball.Position = new Vector2(trueLevelPosition.X * Size.Width, trueLevelPosition.Y * Size.Height);
            }
        }
    }
    public class Ball : IObject, ITangible
    {
        private readonly AnimatedSprite ballSprite;
        private readonly AnimatedSprite shadowSprite;
        private readonly Effect silhouetteEffect;
        private readonly EffectParameter overlayColorSilhouetteEffectParameter;
        private readonly static Size size = new Size(32, 32);
        private readonly static Vector2 drawOffset = new Vector2(-16, -16);
        private readonly static Vector2 minBallShadowOffset = new Vector2(0, -8);
        private Vector2 truePosition;
        private Vector2 ballDrawPosition;
        private Vector2 shadowDrawPosition;
        private float floatHeight = 0;
        private const float floatMaxHeight = 16;
        private const float floatDeltaHeight = 0.5f;
        private bool floatUpDirection = true;
        private int waitCount;
        private int waitTotal = 1;
        private float shadowScale = 1.0f;
        private float ballScale = 1.0f;
        private float ballAlpha = 1.0f;
        private float silhouetteAlpha = 0.0f;
        private float ballHeight = 0f;
        private const float ballMaxHeight = 32;
        private void updateDrawPositions()
        {
            ballDrawPosition = truePosition + drawOffset + minBallShadowOffset + new Vector2(0, -(floatHeight + ballHeight));
            shadowDrawPosition = truePosition + drawOffset;
        }
        private void updateFloatHeight()
        {
            if (floatUpDirection)
            {
                floatHeight += floatDeltaHeight;
                if (floatHeight >= floatMaxHeight)
                {
                    floatHeight = floatMaxHeight;
                    floatUpDirection = false;
                }
            }
            else
            {
                floatHeight -= floatDeltaHeight;
                if (floatHeight <= 0)
                {
                    floatHeight = 0;
                    floatUpDirection = true;
                }
            }
        }
        public Ball(ContentManager content, int id)
        {
            Trace.Assert(id >= 0 && id <= 4);
            ID = id;
            {
                var spriteSheet = content.Load<SpriteSheet>($"sprite_factory/power_balls_{1 + id}.sf", new JsonContentLoader());
                ballSprite = new AnimatedSprite(spriteSheet);
                ballSprite.Origin = new Vector2(48, 48);
                ballSprite.Play("glow_0");
            }
            {
                var spriteSheet = content.Load<SpriteSheet>($"sprite_factory/shadow_0.sf", new JsonContentLoader());
                shadowSprite = new AnimatedSprite(spriteSheet);
                shadowSprite.Origin = new Vector2(16, 16);
                shadowSprite.Alpha = 0.5f;
                shadowSprite.Play("shadow_0");
            }
            {
                silhouetteEffect = content.Load<Effect>("effects/silhouette_0");
                overlayColorSilhouetteEffectParameter = silhouetteEffect.Parameters["OverlayColor"];
                overlayColorSilhouetteEffectParameter.SetValue(Color.Black.ToVector4());
            }
        }
        public void UpdateFloatHeight(Ball other)
        {
            floatHeight = other.floatHeight;
            floatUpDirection = other.floatUpDirection;
            updateFloatHeight();
            updateFloatHeight();
            updateDrawPositions();
        }
        public enum States { Normal, QuickVanish, QuickAppear, LongVanish, LongAppear, Invisible }
        public States State { get; private set; } = States.Normal;
        public void QuickVanish()
        {
            State = States.QuickVanish;
            waitCount = 7;
            waitTotal = 8;
            shadowScale = 1;
            ballScale = 1;
            ballAlpha = 1;
            silhouetteAlpha = 0;
            ballHeight = 0;
        }
        public void QuickAppear()
        {
            State = States.QuickAppear;
            waitCount = 7;
            waitTotal = 8;
            shadowScale = 0;
            ballScale = 0;
            ballAlpha = 1;
            silhouetteAlpha = 0;
            ballHeight = 0;
        }
        public void LongVanish()
        {
            State = States.LongVanish;
            waitCount = 15;
            waitTotal = 16;
            shadowScale = 1;
            ballScale = 1;
            ballAlpha = 1;
            silhouetteAlpha = 0;
            ballHeight = 0;
        }
        public void LongAppear()
        {
            State = States.LongAppear;
            waitCount = 15;
            waitTotal = 16;
            shadowScale = 0;
            ballScale = 1;
            ballAlpha = 0;
            silhouetteAlpha = 1;
            ballHeight = ballMaxHeight;
        }
        public int ID { get; }        
        public Vector2 Position 
        {
            get => truePosition;
            set
            {
                truePosition = value;
                updateDrawPositions();
            }
        }
        public Size Size { get => size; }
        public int Priority { get => (int)Position.Y; set => throw new NotImplementedException(); }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {                                   
            ballSprite.Update(gameTime);
            shadowSprite.Update(gameTime);            
        }
        public void StrictUpdate()
        {
            // Update properties affected by state.
            float waitRatio = (float)waitCount / waitTotal;

            if ((State == States.QuickVanish || State == States.LongVanish))
                shadowScale = MathHelper.Lerp(0, 1, waitRatio);
            else if ((State == States.QuickAppear || State == States.LongAppear))
                shadowScale = MathHelper.Lerp(1, 0, waitRatio);
            else
                shadowScale = 1;

            if (State == States.QuickVanish)
                ballScale = MathHelper.Lerp(0, 1, waitRatio);
            else if (State == States.QuickAppear)
                ballScale = MathHelper.Lerp(1, 0, waitRatio);
            else
                ballScale = 1.0f;

            if (State == States.LongVanish)
                ballAlpha = MathHelper.Lerp(0, 1, waitRatio);
            else if (State == States.LongAppear)
                ballAlpha = MathHelper.Lerp(1, 0, waitRatio);
            else 
                ballAlpha = 1f;

            if (State == States.LongVanish)
                silhouetteAlpha = MathHelper.Lerp(1, 0, waitRatio);
            else if (State == States.LongAppear)
                silhouetteAlpha = MathHelper.Lerp(0, 1, waitRatio);
            else
                silhouetteAlpha = 0f;

            if (State == States.LongVanish)
                ballHeight = MathHelper.Lerp(ballMaxHeight, 0, waitRatio);
            else if (State == States.LongAppear)
                ballHeight = MathHelper.Lerp(0, ballMaxHeight, waitRatio);
            else
                ballHeight = 0;

            // Update Counters
            if (waitCount > 0)
                waitCount--;

            // Update FSM states.
            if ((State == States.QuickVanish || State == States.LongVanish) && waitCount == 0)
                State = States.Invisible;

            if ((State == States.QuickAppear || State == States.LongAppear) && waitCount == 0)
                State = States.Normal;

            // Update float height
            updateFloatHeight();

            // Since float height changes every strict update
            // need to always update draw positions.
            updateDrawPositions();
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            if (State != States.Invisible)
            {
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                spriteBatch.Draw(sprite: shadowSprite, position: shadowDrawPosition, rotation: 0, scale: new Vector2(shadowScale));
                spriteBatch.End();

                ballSprite.Alpha = ballAlpha;
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                spriteBatch.Draw(sprite: ballSprite, position: ballDrawPosition, rotation: 0, scale: new Vector2(ballScale));
                spriteBatch.End();

                ballSprite.Alpha = ballAlpha * silhouetteAlpha;
                spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: silhouetteEffect);
                spriteBatch.Draw(sprite: ballSprite, position: ballDrawPosition, rotation: 0, scale: new Vector2(ballScale));
                spriteBatch.End();
            }
        }
    }
    public class Torch : IObject, ITangible
    {        
        private readonly static Size size = new Size(width: 32, height: 32);
        private readonly static Random random = new Random();
        private readonly AnimatedSprite torchSprite;
        private readonly AnimatedSprite shadowSprite;
        public Torch(ContentManager content)
        {
            {
                var spriteSheet = content.Load<SpriteSheet>("sprite_factory/fire_candelabrum_0.sf", new JsonContentLoader());
                torchSprite = new AnimatedSprite(spriteSheet);
                torchSprite.Origin = new Vector2(x: 0, y: 64);
                torchSprite.Play("flame_0");
            }
            {
                var spriteSheet = content.Load<SpriteSheet>($"sprite_factory/shadow_0.sf", new JsonContentLoader());
                shadowSprite = new AnimatedSprite(spriteSheet);                
                shadowSprite.Origin = Vector2.Zero;
                shadowSprite.Alpha = 0.5f;
                var spriteSheetAnimation = shadowSprite.Play("shadow_0");
                shadowSprite.Update(spriteSheetAnimation.FrameDuration * random.Next(6));
            }
        }
        public Vector2 Position { get; set; }
        public Size Size { get => size; }
        public int Priority { get => (int)Position.Y; set => throw new NotImplementedException(); }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {            
            torchSprite.Update(gameTime);
            shadowSprite.Update(gameTime);
        }
        public void StrictUpdate()
        {
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(sprite: shadowSprite, position: Position);
            spriteBatch.End();

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(sprite: torchSprite, position: Position);
            spriteBatch.End();
        }
    }
    public class Button : IObject, ITangible
    {
        private readonly AnimatedSprite visualSprite;
        private readonly AnimatedSprite messageSprite;
        private readonly static ReadOnlyDictionary<Modes, string> visualAssetMap = new(new Dictionary<Modes, string>()
        {
            { Modes.Up, "sprite_factory/button_0.sf" },
            { Modes.Down, "sprite_factory/button_0.sf" },
            { Modes.Left, "sprite_factory/button_0.sf" },
            { Modes.Right, "sprite_factory/button_0.sf" },
            { Modes.Pause, "sprite_factory/button_1.sf" },
            { Modes.Start, "sprite_factory/button_1.sf" },
        });
        private readonly static ReadOnlyDictionary<Modes, string> messageAssetMap = new(new Dictionary<Modes, string>()
        {
            { Modes.Up, "sprite_factory/arrows_0.sf" },
            { Modes.Down, "sprite_factory/arrows_0.sf" },
            { Modes.Left, "sprite_factory/arrows_0.sf" },
            { Modes.Right, "sprite_factory/arrows_0.sf" },
            { Modes.Pause, "sprite_factory/texts_0.sf" },
            { Modes.Start, "sprite_factory/texts_0.sf" },
        });
        private readonly static ReadOnlyDictionary<Modes, string> nameMap = new(new Dictionary<Modes, string>()
        {
            { Modes.Up, "up_0" },
            { Modes.Down, "down_0" },
            { Modes.Left, "left_0" },
            { Modes.Right, "right_0" },
            { Modes.Pause, "pause_0" },
            { Modes.Start, "start_0" },
        });
        private readonly static ReadOnlyDictionary<Modes, Keys> keyMap = new(new Dictionary<Modes, Keys>()
        {
            { Modes.Up, Keys.Up },
            { Modes.Down, Keys.Down },
            { Modes.Left, Keys.Left },
            { Modes.Right, Keys.Right },
            { Modes.Pause, Keys.Escape },
            { Modes.Start, Keys.Enter },
        });
        public Button(ContentManager content, Modes mode)
        {
            Mode = mode;
            {
                var spriteSheet = content.Load<SpriteSheet>(visualAssetMap[mode], new JsonContentLoader());
                visualSprite = new AnimatedSprite(spriteSheet);
                visualSprite.Origin = Vector2.Zero;
                visualSprite.Play("unselected_0");
                Size = (Size)spriteSheet.TextureAtlas[0].Size;
            }
            {
                var spriteSheet = content.Load<SpriteSheet>(messageAssetMap[mode], new JsonContentLoader());
                messageSprite = new AnimatedSprite(spriteSheet);
                messageSprite.Origin = Vector2.Zero;
                messageSprite.Play(nameMap[mode]);
            }            
        }
        public enum Modes { Up, Down, Left, Right, Pause, Start }        
        public Modes Mode { get; }
        public bool Selected { get; private set; } = false;
        public bool Pressed { get; private set; } = false;
        public bool Released { get; private set; } = false;
        public Vector2 Position { get; set; }
        public Size Size { get; }
        public int Priority { get => (int)Position.Y; set => throw new NotImplementedException(); }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {            
            if (!Selected && ((
                mouseState.LeftButton == ButtonState.Pressed && 
                mouseState.Position.X >= Position.X && mouseState.X < (Position.X + Size.Width) &&
                mouseState.Position.Y >= Position.Y && mouseState.Y < (Position.Y + Size.Height)) || (
                keyboardState.IsKeyDown(keyMap[Mode]))))
            {
                visualSprite.Play("selected_0");
                Selected = true;
                Pressed = true;                
            }
            else
            {
                Pressed = false;
            }

            if (Selected && ((
                mouseState.LeftButton != ButtonState.Pressed) && 
                keyboardState.IsKeyUp(keyMap[Mode])))
            {
                visualSprite.Play("unselected_0");
                Selected = false;
                Released = true;
            }
            else
            {
                Released = false;
            }

            {                
                visualSprite.Update(gameTime);
                messageSprite.Update(gameTime);
            }
        }
        public void StrictUpdate()
        {

        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(sprite: visualSprite, position: Position);
            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(sprite: messageSprite, position: Position);
            spriteBatch.End();
        }
    }
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class SnakeExtremeGame : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private TiledMap levelTiledMap;
        private TiledMapRenderer levelTiledMapRenderer;
        private List<IObject> gameObjects;
        private Button upButton, downButton, leftButton, rightButton, startButton, pauseButton;
        private float strictTimePassed;
        private const float strictTimeAmount = (float)1 / 30;
        private Snake snake;
        private int waitCount;
        private bool growTail = false;

        public SnakeExtremeGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();

        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {           
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: Use this.Content to load your game content here
            levelTiledMap = Content.Load<TiledMap>("tiled_project/level_0");
            levelTiledMapRenderer = new TiledMapRenderer(graphicsDevice: GraphicsDevice, map: levelTiledMap);
            var maskLayer = levelTiledMap.TileLayers.Where(x => x.Name == "mask_0").First();            
            gameObjects = new();
            for (int x = 0; x < levelTiledMap.Width; x++)
            {
                for (int y = 0; y < levelTiledMap.Height; y++)
                {
                    var tile = maskLayer.GetTile((ushort)x, (ushort)y);
                    var position = new Vector2(x * levelTiledMap.TileWidth, y * levelTiledMap.TileHeight);
                    if (tile.GlobalIdentifier == 4098)
                    {
                        var torch = new Torch(Content)
                        {
                            Position = position
                        };
                        gameObjects.Add(torch);
                    } 
                    else if (tile.GlobalIdentifier == 4099)
                    {
                        upButton = new Button(Content, Button.Modes.Up)
                        {
                            Position = position
                        };
                        gameObjects.Add(upButton);
                    }
                    else if (tile.GlobalIdentifier == 4100)
                    {
                        downButton = new Button(Content, Button.Modes.Down)
                        {
                            Position = position
                        };
                        gameObjects.Add(downButton);
                    }
                    else if (tile.GlobalIdentifier == 4101)
                    {
                        rightButton = new Button(Content, Button.Modes.Right)
                        {
                            Position = position
                        };
                        gameObjects.Add(rightButton);
                    }
                    else if (tile.GlobalIdentifier == 4102)
                    {
                        leftButton = new Button(Content, Button.Modes.Left)
                        {
                            Position = position
                        };
                        gameObjects.Add(leftButton);
                    }
                    else if (tile.GlobalIdentifier == 4103)
                    {
                        startButton = new Button(Content, Button.Modes.Start)
                        {
                            Position = position
                        };
                        gameObjects.Add(startButton);
                    }
                    else if (tile.GlobalIdentifier == 4104)
                    {
                        pauseButton = new Button(Content, Button.Modes.Pause)
                        {
                            Position = position
                        };
                        gameObjects.Add(pauseButton);
                    }
                }
            }

            snake = new Snake(Content);
            gameObjects.Add(snake);
            //Console.WriteLine($"CWD: {System.AppDomain.CurrentDomain.BaseDirectory}");
            //spriteSheet = Content.Load<SpriteSheet>("sprite_factory/fire_candelabrum_0.sf", new JsonContentLoader());
            //sprite = new AnimatedSprite(spriteSheet);
            //sprite.Play("flame_0");

            //var mask0Layer = tiledMap.TileLayers.Where(x => x.Name == "mask_0").First();
            //var tile = mask0Layer.GetTile(10, 10);
            //Console.WriteLine($"Tile: {tile.GlobalIdentifier}");
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
            MouseState mouseState = Mouse.GetState();
            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState gamePadState = default;
            try { gamePadState = GamePad.GetState(PlayerIndex.One); }
            catch (NotImplementedException) { /* ignore gamePadState */ }

            //if (keyboardState.IsKeyDown(Keys.Escape) ||
            //    keyboardState.IsKeyDown(Keys.Back) ||
            //    gamePadState.Buttons.Back == ButtonState.Pressed)
            //{
            //    try { Exit(); }
            //    catch (PlatformNotSupportedException) { /* ignore */ }
            //}

            // TODO: Add your update logic here


            levelTiledMapRenderer.Update(gameTime);
            foreach (var gameObject in gameObjects)
                gameObject.Update(gameTime, mouseState, keyboardState);

            if (upButton.Pressed)
                snake.Direction = Snake.Directions.Up;
            if (downButton.Pressed)
                snake.Direction = Snake.Directions.Down;
            if (leftButton.Pressed)
                snake.Direction = Snake.Directions.Left;
            if (rightButton.Pressed)
                snake.Direction = Snake.Directions.Right;
            if (startButton.Pressed)
            {
                if (snake.Headless)
                {
                    snake.CreateHead(new Point(10, 10));                    
                    gameObjects.Add(snake.Head);
                }
                else if (snake.State == Snake.States.Normal)
                {
                    snake.Vanish();
                }
            }
            if (snake.State == Snake.States.Gone)
            {
                foreach (var body in snake.Bodies)
                    gameObjects.Remove(body);
                snake.Clear();
            }
            if (pauseButton.Pressed && !snake.Headless)
            {
                growTail = true;
            }
            if (waitCount == 0 && !snake.Headless && snake.State == Snake.States.Normal)
            {
                waitCount = 30;
                snake.Move(growTail);
                growTail = false;
            }
            if (snake.NewTailAvailable)
                gameObjects.Add(snake.Tail);


            {
                float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                strictTimePassed += deltaTime;
                for (; strictTimePassed >= strictTimeAmount; strictTimePassed -= strictTimeAmount)
                {
                    if (waitCount > 0)
                        waitCount--;
                    foreach (var gameObject in gameObjects)
                        gameObject.StrictUpdate();
                }
            }



            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            levelTiledMapRenderer.Draw();
            foreach (var gameObject in gameObjects.OrderBy(x => x.Priority))
                gameObject.Draw(spriteBatch);
            base.Draw(gameTime);
        }
    }
}
