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


namespace SnakeExtreme
{
    public interface IObject
    {
        public int Priority { get; }
        public void Update(GameTime gameTime);
        public void Draw(SpriteBatch spriteBatch);
    }
    public interface ITangible
    {
        public Vector2 Position { get; set; }
        public Size Size { get; }
    }
    public class Torch : IObject, ITangible
    {        
        private readonly AnimatedSprite sprite;
        public Torch(ContentManager content)
        {            
            var spriteSheet = content.Load<SpriteSheet>("sprite_factory/fire_candelabrum_0.sf", new JsonContentLoader());            
            sprite = new AnimatedSprite(spriteSheet);
            sprite.Origin = new Vector2(x: 0, y: 64) - sprite.Origin;
            sprite.Play("flame_0");
        }
        public Vector2 Position { get; set; }
        public Size Size { get => new Size(width: 32, height: 32); }
        public int Priority { get => (int)Position.Y; }
        public void Update(GameTime gameTime)
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
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class SnakeExtremeGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        TiledMap tiledMap;
        TiledMapRenderer tiledMapRenderer;
        List<IObject> gameObjects;
        SpriteSheet spriteSheet;
        AnimatedSprite sprite;

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
            tiledMap = Content.Load<TiledMap>("tiled_project/level_0");
            tiledMapRenderer = new TiledMapRenderer(graphicsDevice: GraphicsDevice, map: tiledMap);
            var mask0Layer = tiledMap.TileLayers.Where(x => x.Name == "mask_0").First();
            gameObjects = new();
            for (int x = 0; x < tiledMap.Width; x++)
            {
                for (int y = 0; y < tiledMap.Height; y++)
                {
                    var tile = mask0Layer.GetTile((ushort)x, (ushort)y);                    
                    if (tile.GlobalIdentifier == 4098)
                    {
                        var torch = new Torch(Content)
                        {
                            Position = new Vector2(x: x * tiledMap.TileWidth, y: y * tiledMap.TileHeight)
                        };
                        gameObjects.Add(torch);
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
            tiledMapRenderer.Update(gameTime);
            foreach (var gameObject in gameObjects)
                gameObject.Update(gameTime);
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
            tiledMapRenderer.Draw();
            foreach (var gameObject in gameObjects.OrderBy(x => x.Priority))
                gameObject.Draw(spriteBatch);
            base.Draw(gameTime);
        }
    }
}
