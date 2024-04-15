using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;


namespace SnakeExtreme
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class SnakeExtremeGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Color clearColor = Color.Pink;
        //Texture2D levelTexture2D;
        TiledMap tiledMap;
        TiledMapRenderer tiledMapRenderer;

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
            //levelTexture2D = Content.Load<Texture2D>("level_0");
            tiledMap = Content.Load<TiledMap>("tiled_project/level_0");
            tiledMapRenderer = new TiledMapRenderer(graphicsDevice: GraphicsDevice, map: tiledMap);
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
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

            if (keyboardState.IsKeyDown(Keys.Escape) ||
                keyboardState.IsKeyDown(Keys.Back) ||
                gamePadState.Buttons.Back == ButtonState.Pressed)
            {
                try { Exit(); }
                catch (PlatformNotSupportedException) { /* ignore */ }
            }

            if (keyboardState.IsKeyDown(Keys.A))
                clearColor = Color.Blue;
            if (keyboardState.IsKeyDown(Keys.S))
                clearColor = Color.Red;

            // TODO: Add your update logic here
            tiledMapRenderer.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(clearColor);

            // TODO: Add your drawing code here
            tiledMapRenderer.Draw();
            base.Draw(gameTime);
        }
    }
}
