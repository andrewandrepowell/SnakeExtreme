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
        public void Draw(SpriteBatch spriteBatch);
    }
    public interface ITangible
    {
        public Vector2 Position { get; set; }
        public Size Size { get; }
    }
    public enum ButtonTypes { Up, Down, Left, Right, Pause, Start }
    public class Torch : IObject, ITangible
    {        
        private readonly AnimatedSprite sprite;
        public Torch(ContentManager content)
        {            
            var spriteSheet = content.Load<SpriteSheet>("sprite_factory/fire_candelabrum_0.sf", new JsonContentLoader());            
            sprite = new AnimatedSprite(spriteSheet);
            sprite.Origin = new Vector2(x: 0, y: 64);
            sprite.Play("flame_0");
        }
        public Vector2 Position { get; set; }
        public Size Size { get => new Size(width: 32, height: 32); }
        public int Priority { get => (int)Position.Y; set => throw new NotImplementedException(); }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {            
            sprite.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(sprite: sprite, position: Position);
            spriteBatch.End();
        }
    }
    public class Button : IObject, ITangible
    {
        private readonly AnimatedSprite visualSprite;
        private readonly AnimatedSprite messageSprite;
        private readonly static ReadOnlyDictionary<ButtonTypes, string> visualAssetMap = new(new Dictionary<ButtonTypes, string>()
        {
            { ButtonTypes.Up, "sprite_factory/button_0.sf" },
            { ButtonTypes.Down, "sprite_factory/button_0.sf" },
            { ButtonTypes.Left, "sprite_factory/button_0.sf" },
            { ButtonTypes.Right, "sprite_factory/button_0.sf" },
            { ButtonTypes.Pause, "sprite_factory/button_1.sf" },
            { ButtonTypes.Start, "sprite_factory/button_1.sf" },
        });
        private readonly static ReadOnlyDictionary<ButtonTypes, string> messageAssetMap = new(new Dictionary<ButtonTypes, string>()
        {
            { ButtonTypes.Up, "sprite_factory/arrows_0.sf" },
            { ButtonTypes.Down, "sprite_factory/arrows_0.sf" },
            { ButtonTypes.Left, "sprite_factory/arrows_0.sf" },
            { ButtonTypes.Right, "sprite_factory/arrows_0.sf" },
            { ButtonTypes.Pause, "sprite_factory/texts_0.sf" },
            { ButtonTypes.Start, "sprite_factory/texts_0.sf" },
        });
        private readonly static ReadOnlyDictionary<ButtonTypes, string> nameMap = new(new Dictionary<ButtonTypes, string>()
        {
            { ButtonTypes.Up, "up_0" },
            { ButtonTypes.Down, "down_0" },
            { ButtonTypes.Left, "left_0" },
            { ButtonTypes.Right, "right_0" },
            { ButtonTypes.Pause, "pause_0" },
            { ButtonTypes.Start, "start_0" },
        });
        private readonly static ReadOnlyDictionary<ButtonTypes, Keys> keyMap = new(new Dictionary<ButtonTypes, Keys>()
        {
            { ButtonTypes.Up, Keys.Up },
            { ButtonTypes.Down, Keys.Down },
            { ButtonTypes.Left, Keys.Left },
            { ButtonTypes.Right, Keys.Right },
            { ButtonTypes.Pause, Keys.Escape },
            { ButtonTypes.Start, Keys.Enter },
        });
        public Button(ContentManager content, ButtonTypes buttonType)
        {
            ButtonType = buttonType;
            {
                var spriteSheet = content.Load<SpriteSheet>(visualAssetMap[buttonType], new JsonContentLoader());
                visualSprite = new AnimatedSprite(spriteSheet);
                visualSprite.Origin = Vector2.Zero;
                visualSprite.Play("unselected_0");
                Size = (Size)spriteSheet.TextureAtlas[0].Size;
            }
            {
                var spriteSheet = content.Load<SpriteSheet>(messageAssetMap[buttonType], new JsonContentLoader());
                messageSprite = new AnimatedSprite(spriteSheet);
                messageSprite.Origin = Vector2.Zero;
                messageSprite.Play(nameMap[buttonType]);
            }
            Console.WriteLine($"Origin: {visualSprite.Origin}");
        }
        public ButtonTypes ButtonType { get; }
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
                keyboardState.IsKeyDown(keyMap[ButtonType]))))
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
                keyboardState.IsKeyUp(keyMap[ButtonType])))
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
                float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                visualSprite.Update(deltaTime);
                messageSprite.Update(deltaTime);
            }
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
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        TiledMap levelTiledMap;
        TiledMapRenderer levelTiledMapRenderer;
        List<IObject> gameObjects;
        Button upButton, downButton, leftButton, rightButton, startButton, pauseButton;

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
                        upButton = new Button(Content, ButtonTypes.Up)
                        {
                            Position = position
                        };
                        gameObjects.Add(upButton);
                    }
                    else if (tile.GlobalIdentifier == 4100)
                    {
                        downButton = new Button(Content, ButtonTypes.Down)
                        {
                            Position = position
                        };
                        gameObjects.Add(downButton);
                    }
                    else if (tile.GlobalIdentifier == 4101)
                    {
                        rightButton = new Button(Content, ButtonTypes.Right)
                        {
                            Position = position
                        };
                        gameObjects.Add(rightButton);
                    }
                    else if (tile.GlobalIdentifier == 4102)
                    {
                        leftButton = new Button(Content, ButtonTypes.Left)
                        {
                            Position = position
                        };
                        gameObjects.Add(leftButton);
                    }
                    else if (tile.GlobalIdentifier == 4103)
                    {
                        startButton = new Button(Content, ButtonTypes.Start)
                        {
                            Position = position
                        };
                        gameObjects.Add(startButton);
                    }
                    else if (tile.GlobalIdentifier == 4104)
                    {
                        pauseButton = new Button(Content, ButtonTypes.Pause)
                        {
                            Position = position
                        };
                        gameObjects.Add(pauseButton);
                    }
                }
            }


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
