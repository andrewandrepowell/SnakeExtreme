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
using MonoGame.Extended.BitmapFonts;
using System.Text;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.JSInterop;


namespace SnakeExtreme
{
    public interface IObject
    {
        public int Priority { get; set; }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState);
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
    public class CenterScreen
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly Size screenDimensions;
        private readonly RenderTarget2D renderTarget;        
        public CenterScreen(GraphicsDevice graphicsDevice, Size screenDimensions)
        {
            this.graphicsDevice = graphicsDevice;
            this.screenDimensions = screenDimensions;
            Scalar = 1;
            Offset = Vector2.Zero;
            Size = screenDimensions;
            renderTarget = new RenderTarget2D(graphicsDevice, screenDimensions.Width, screenDimensions.Height);
        }
        public void BeginCapture()
        {
            graphicsDevice.SetRenderTarget(renderTarget);
        }
        public void EndCapture()
        {
            graphicsDevice.SetRenderTarget(null);
        }
        public MouseState UpdateMouseState(MouseState mouseState)
        {
            var newMouseState = new MouseState(
                x: (int)((mouseState.X - Offset.X) / Scalar),
                y: (int)((mouseState.Y - Offset.Y) / Scalar),
                scrollWheel: mouseState.ScrollWheelValue,
                leftButton: mouseState.LeftButton,
                middleButton: mouseState.MiddleButton,
                rightButton: mouseState.RightButton,
                xButton1: mouseState.XButton1,
                xButton2: mouseState.XButton2);
            return newMouseState;
        }
        public Vector2 UpdateTouchState(Vector2 touchState)
        {
            return new Vector2(
                x: (touchState.X - Offset.X) / Scalar,
                y: (touchState.Y - Offset.Y) / Scalar);
        }
        public float Scalar { get; private set; }
        public Vector2 Offset { get; private set; }
        public Size Size { get; private set; }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();
            spriteBatch.Draw(
                texture: renderTarget, 
                position: Offset, 
                sourceRectangle: null, 
                color: Color.White, 
                rotation: 0, 
                origin: Vector2.Zero, 
                scale: Scalar, 
                effects: SpriteEffects.None, 
                layerDepth: 0);
            spriteBatch.End();
        }
        public void Update()
        {
            if (BrowserService.DimensionsUpdated)
            {
                {
                    var widthScalar = (float)BrowserService.Dimensions.Width / screenDimensions.Width;
                    var heightScalar = (float)BrowserService.Dimensions.Height / screenDimensions.Height;
                    if (screenDimensions.Height * widthScalar > BrowserService.Dimensions.Height)
                        Scalar = heightScalar;
                    else
                        Scalar = widthScalar;
                }
                {
                    var newWidth = (int)Math.Ceiling(Scalar * screenDimensions.Width);
                    var newHeight = (int)Math.Ceiling(Scalar * screenDimensions.Height);
                    Size = new Size(newWidth, newHeight);
                }
                {
                    var newXOffset = (BrowserService.Dimensions.Width - Size.Width) / 2;
                    var newYOffset = (BrowserService.Dimensions.Height - Size.Height) / 2;
                    Offset = new Vector2(newXOffset, newYOffset);
                }                
            }
        }
    }
    public class ShineEffect : IObject, ITangible
    {
        private readonly AnimatedSprite shineSprite;
        private readonly Effect silhouetteEffect;
        private readonly EffectParameter silhouetteOverlayColorEffectParameter;
        private readonly static Color silhouetteColor = new Color(40, 121, 202);        
        private Vector2 velocityGravity;
        private int waitCount, waitTotal;
        private float shineAlpha = 1;
        private float silhouetteAlpha = 0;
        private bool silhouetteToggle;
        public static void LoadAll(ContentManager content)
        {
            content.Load<SpriteSheet>($"sprite_factory/shine_0.sf", new JsonContentLoader());
            content.Load<Effect>("effects/silhouette_0");
        }
        public ShineEffect(ContentManager content)
        {
            {
                var spriteSheet = content.Load<SpriteSheet>($"sprite_factory/shine_0.sf", new JsonContentLoader());
                shineSprite = new AnimatedSprite(spriteSheet);
                Size = (Size)spriteSheet.TextureAtlas[0].Size;
            }
            {
                silhouetteEffect = content.Load<Effect>("effects/silhouette_0");
                silhouetteOverlayColorEffectParameter = silhouetteEffect.Parameters["OverlayColor"];
            }
            shineSprite.Play("shine_0");
        }
        public enum States { Normal, Vanish, Gone }
        public States State { get; private set; } = States.Normal;
        public void Vanish()
        {
            State = States.Vanish;
            waitCount = 14;
            waitTotal = 15;
            shineAlpha = 1;
        }
        public Vector2 Gravity { get; set; }
        public Vector2 Velocity { get; set; }
        public float Scale { get; set; } = 1;
        public float Alpha { get; set; } = 1;
        public Vector2 Position { get; set; }
        public Size Size { get; }
        public int Priority { get; set; }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {
        }
        public void StrictUpdate()
        {
            float waitRatio = (float)waitCount / waitTotal;

            if (State == States.Vanish)
                shineAlpha = MathHelper.Lerp(0, 1, waitRatio);
            else
                shineAlpha = 1;

            if (silhouetteToggle)
                silhouetteAlpha = 0.5f;
            else
                silhouetteAlpha = 0;

            if (State == States.Vanish && waitCount == 0)
                State = States.Gone;

            if (waitCount > 0)
                waitCount--;

            silhouetteToggle = !silhouetteToggle;

            Position += Velocity + velocityGravity;
            velocityGravity += Gravity;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            if (State != States.Gone)
            {
                shineSprite.Alpha = Alpha * shineAlpha;
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                spriteBatch.Draw(sprite: shineSprite, position: Position, rotation: 0, scale: new Vector2(Scale));
                spriteBatch.End();

                shineSprite.Alpha = Alpha * silhouetteAlpha;
                silhouetteOverlayColorEffectParameter.SetValue(silhouetteColor.ToVector4());
                spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: silhouetteEffect);
                spriteBatch.Draw(sprite: shineSprite, position: Position, rotation: 0, scale: new Vector2(Scale));
                spriteBatch.End();
            }
        }
    }
    public class LightningEffect : IObject, ITangible
    {        
        private readonly AnimatedSprite lightningSprite;
        private readonly AnimatedSprite lightningFadeOutSprite;
        private AnimatedSprite currentSprite;
        private SpriteSheetAnimation currentSpriteSheetAnimation;
        public static void LoadAll(ContentManager content)
        {
            content.Load<SpriteSheet>($"sprite_factory/lightning_0.sf", new JsonContentLoader());
            content.Load<SpriteSheet>($"sprite_factory/lightning_fade_out_0.sf", new JsonContentLoader());
        }
        public LightningEffect(ContentManager content)
        {
            {
                var spriteSheet = content.Load<SpriteSheet>($"sprite_factory/lightning_0.sf", new JsonContentLoader());
                lightningSprite = new AnimatedSprite(spriteSheet);                
                Size = (Size)spriteSheet.TextureAtlas[0].Size;
            }
            {
                var spriteSheet = content.Load<SpriteSheet>($"sprite_factory/lightning_fade_out_0.sf", new JsonContentLoader());
                lightningFadeOutSprite = new AnimatedSprite(spriteSheet);                
            }
            {
                currentSprite = lightningSprite;
                lightningSprite.Play("lightning_0");
            }
        }
        public enum States { Vanish, Gone, Normal };
        public States State { get; private set; } = States.Normal;
        public void Vanish()
        {
            Debug.Assert(currentSprite == lightningSprite);
            Debug.Assert(currentSpriteSheetAnimation == null);
            currentSprite = lightningFadeOutSprite;
            currentSpriteSheetAnimation = currentSprite.Play("lightning_0");
            State = States.Vanish;
        }
        public float Scale { get; set; } = 1;
        public float Alpha { get; set; } = 1;
        public Vector2 Position { get; set; }
        public Size Size { get; }
        public int Priority { get; set; }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {
            if (State == States.Vanish && currentSpriteSheetAnimation.IsComplete)
                State = States.Gone;

            currentSprite.Update(gameTime);
        }
        public void StrictUpdate()
        {
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            currentSprite.Alpha = Alpha;
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(sprite: currentSprite, position: Position, rotation: 0, scale: new Vector2(Scale));
            spriteBatch.End();
        }
    }
    public class LightningObstacle : IObject, ITangible, ILevelObject
    {
        private readonly Ball ball;
        private Point trueLevelPosition;        
        public LightningObstacle(ContentManager content)
        {
            ball = new Ball(content, 1);
            ball.LightningAppear();
            State = States.Appear;
        }
        public enum States { Appear, Normal, Vanish, Gone }
        public States State { get; private set; }
        public void Vanish()
        {
            Debug.Assert(State == States.Normal);
            ball.LightningVanish();
            State = States.Vanish;
        }
        public void Appear()
        {
            Debug.Assert(State == States.Gone);
            ball.LightningAppear();
            State = States.Appear;
        }
        public Vector2 Position
        {
            get => ball.Position;
            set => throw new NotImplementedException();
        }
        public Size Size
        {
            get => ball.Size;
        }
        public int Priority
        {
            get => ball.Priority;
            set => throw new NotImplementedException();
        }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {
            if (State == States.Appear && ball.State == Ball.States.LightningNormal)
                State = States.Normal;

            if (State == States.Vanish && ball.State == Ball.States.Invisible)
                State = States.Gone;

            ball.Update(gameTime, mouseState, keyboardState, touchState);
        }
        public void StrictUpdate()
        {
            ball.StrictUpdate();
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            ball.Draw(spriteBatch);
        }
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
    public class Obstacle : IObject, ITangible, ILevelObject
    {
        private readonly Ball ball;
        private Point trueLevelPosition;
        public Obstacle(ContentManager content)
        {
            ball = new Ball(content, 1);
            ball.LongAppear();
            State = States.Appear;
        }
        public enum States { Appear, Normal, Vanish, Gone }
        public States State { get; private set; }
        public void Vanish()
        {
            Debug.Assert(State == States.Normal);
            ball.LongVanish();
            State = States.Vanish;
        }
        public Vector2 Position
        {
            get => ball.Position;
            set => throw new NotImplementedException();
        }
        public Size Size
        {
            get => ball.Size;
        }
        public int Priority
        {
            get => ball.Priority;
            set => throw new NotImplementedException();
        }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {
            if (State == States.Appear && ball.State == Ball.States.Normal)
                State = States.Normal;

            if (State == States.Vanish && ball.State == Ball.States.Invisible)
                State = States.Gone;

            ball.Update(gameTime, mouseState, keyboardState, touchState);
        }
        public void StrictUpdate()
        {
            ball.StrictUpdate();
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            ball.Draw(spriteBatch);
        }
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
    public class Music : IObject
    {
        private readonly Sound _sound;
        private const int _totalWait = 15;
        private int _currWait;
        private float _nextVolume;
        private float _prevVolume;
        public enum States { Changing, Playing, Stopped }
        public Music(ContentManager content, string asset)
        {
            _sound = new Sound(content, asset);
        }
        public States State { get; private set; } = States.Stopped;
        public int Priority { get => 0; set => throw new NotImplementedException(); }
        public float Volume
        {
            get => _sound.Volume; 
            set
            {
                Debug.Assert(State != States.Changing);
                _sound.Volume = value;
            }
        }
        public bool Playing
        {
            get => _sound.Playing;
        }
        public void SmoothChange(float volume)
        {
            Debug.Assert(State == States.Playing);
            _nextVolume = volume;
            _prevVolume = _sound.Volume;
            _currWait = _totalWait - 1;
            State = States.Changing;
        }
        public void Play()
        {
            Debug.Assert(State == States.Stopped);
            _sound.Play();
            State = States.Playing;
        }
        public void Stop()
        {
            Debug.Assert(State == States.Playing);
            _sound.Stop();
            State = States.Stopped;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
        }

        public void StrictUpdate()
        {
            if (State == States.Changing)
            {
                float ratio = (float)_currWait / _totalWait;
                _sound.Volume = MathHelper.SmoothStep(_nextVolume, _prevVolume, ratio);
            }

            if (State == States.Changing && _currWait == 0)
                State = States.Playing;

            if (_currWait > 0)
                _currWait--;
        }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {
            if (State == States.Playing && !_sound.Playing)
                _sound.Play();
        }
    }
    public class Sound
    {
        private readonly SoundEffectInstance soundEffectInstance;
        private readonly static Random random = new Random();
        private float trueVolume;
        public Sound(ContentManager content, string asset)
        {
            try
            {
                soundEffectInstance = content.Load<SoundEffect>(asset).CreateInstance();
                Mode = Modes.AudioAvailable;
            }
            catch (NoAudioHardwareException)
            {
                Mode = Modes.NoAudio;
            }
            Volume = 0.5f;
        }
        public enum Modes { NoAudio, AudioAvailable }
        public Modes Mode { get; }
        public void Play(bool randomPitch = false)
        {
            if (Mode == Modes.AudioAvailable)
            {
                soundEffectInstance.Pitch = 
                    (randomPitch ? random.NextSingle() - 0.5f : 0);
                // It is extremely important to stop prior to playing
                // Stop puts the SoundEffectInstance into its Stopped state
                // which causes Play to run PlatformPlay under-the-hood.
                // This is important because PlatformPlay, after traversing several
                // nested calls into nkast.Wasm.Audio.Audio.8.0.0.js, will run
                // JavaScript function nkAudioScheduledSourceNode.Start.
                // That function will refresh the AudioContext used by SoundEffectInstance
                // which is needed to prevent the "The AudioContext was not allowed to start
                // error. It must be resumed (or created) after a user gesture on the page."
                soundEffectInstance.Stop(); 
                soundEffectInstance.Play();
            }
        } 
        public void Stop()
        {
            if (Playing)
            {
                soundEffectInstance.Stop();
            }
        }
        public bool Playing 
        { 
            get
            {                
                if (Mode == Modes.AudioAvailable)
                {
                    return soundEffectInstance.State == SoundState.Playing;
                }
                else
                {
                    return false;
                }
            }
        }
        public float Volume 
        {
            get => trueVolume;
            set
            {
                Debug.Assert(0 <= value && value <= 1);
                trueVolume = value;
                if (Mode == Modes.AudioAvailable)
                {
                    soundEffectInstance.Volume = Utility.cerp(0, 1, trueVolume, Utility.PowSpeedTable);
                }
            }
        }
    }
    public static class Utility
    {
        public static readonly IEnumerable<float> PowSpeedTable = GetPowTable(b: (float)Math.Exp(0), resolution: 64);
        public static Point Multiply(this Point point, int scalar) =>
            new Point(point.X * scalar, point.Y * scalar);
        public static IEnumerable<float> GetPowTable(float b = 2, int resolution = 32)
        {
            Trace.Assert(resolution > 0);
            foreach (var i in Enumerable.Range(0, resolution))
            {
                yield return (float)Math.Pow(i, b);
            }
        }
        public static IEnumerable<float> CumulativeSum(this IEnumerable<float> values)
        {
            float total = 0;
            foreach (var value in values)
            {
                total += value;
                yield return total;
            }
        }
        public static float cerp(float lowValue, float highValue, float amount, IEnumerable<float> speedTable)
        {
            Debug.Assert(amount >= 0 && amount <= 1);
            Debug.Assert(speedTable.All((x) => x >= 0));

            var positionTable = speedTable.CumulativeSum().ToArray();
            var reversed = lowValue > highValue;
            if (reversed)
            {
                amount = 1 - amount;
                (lowValue, highValue) = (highValue, lowValue);
            }
            var distance = highValue - lowValue;
            var index = (int)Math.Round(amount * (positionTable.Length - 1));
            var result = distance * ((positionTable[index] - positionTable.First()) / positionTable.Last()) + lowValue;
            return result;
        }
        public static IEnumerable<T> Chain<T>(this IEnumerable<T> curr, IEnumerable<T> other)
        {
            return new IEnumerable<T>[] { curr, other }.SelectMany(x => x);
        }
    }
    public class Board : IObject, ITangible
    {
        private readonly AnimatedSprite panelSprite;
        private readonly BitmapFont textBitmapFont;
        private const int textYOffset = 78;
        private const int textXOffset = 16;
        private readonly List<StringBuilder> textLines = new();        
        private string trueText;
        private Vector2 truePosition, panelDrawPosition;
        private List<Vector2> textDrawPositions = new();
        private int waitCount, waitTotal = 1;
        private const float boardHeightMax = 64;
        private float boardHeightOffset = boardHeightMax;
        private float boardAlpha = 0;
        private void updateDrawPosition()
        {
            panelDrawPosition = truePosition + new Vector2(0, -boardHeightOffset);
            textDrawPositions.Clear();
            for (int i = 0; i < textLines.Count; i++)
            {
                textDrawPositions.Add(new Vector2(
                    truePosition.X + textXOffset, 
                    truePosition.Y + textYOffset - boardHeightOffset + i * textBitmapFont.LineHeight));
            }
        }
        public Board(ContentManager content)
        {
            {
                var spriteSheet = content.Load<SpriteSheet>("sprite_factory/panel_1.sf", new JsonContentLoader());
                panelSprite = new AnimatedSprite(spriteSheet);
                panelSprite.Origin = Vector2.Zero;
                panelSprite.Play("panel_0");
                Size = (Size)spriteSheet.TextureAtlas[0].Size;
            }
            {
                textBitmapFont = content.Load<BitmapFont>("fonts/montserrat_1");                
            }
            
        }
        public string Text
        {
            get => trueText;
            set
            {
                trueText = value;

                // Break the text appear to fill in board.
                textLines.Clear();
                var lineWidth = Size.Width - 2 * textXOffset;
                var currLine = new StringBuilder();
                foreach (var token in trueText.Split(' '))
                {
                    if (token == "<n>")
                    {
                        textLines.Add(currLine);
                        currLine = new StringBuilder();
                    }
                    else
                    {
                        var tokenWidth = textBitmapFont.MeasureString(token).Width;
                        var currLineWidth = textBitmapFont.MeasureString(currLine).Width;
                        if (tokenWidth + currLineWidth > lineWidth)
                        {
                            textLines.Add(currLine);
                            currLine = new StringBuilder();
                        }
                        Debug.Assert(tokenWidth <= lineWidth);
                        currLine.Append(token + " ");
                    }
                }
                textLines.Add(currLine);

                updateDrawPosition();
            }
                
        }
        public enum States { Open, Opened, Close, Closed }
        public void Open()
        {
            waitCount = 29;
            waitTotal = 30;
            boardHeightOffset = boardHeightMax;
            boardAlpha = 0;
            State = States.Open;
        }
        public void Close()
        {
            waitCount = 29;
            waitTotal = 30;
            boardHeightOffset = 0;
            boardAlpha = 1;
            State = States.Close;
        }
        public States State { get; private set; } = States.Closed;
        public Vector2 Position 
        {
            get => truePosition;
            set
            {
                truePosition = value;
                updateDrawPosition();
            }
        }
        public Size Size { get; }
        public int Priority { get; set; }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {
            panelSprite.Update(gameTime);
        }
        public void StrictUpdate()
        {
            // Update properties
            float waitRatio = (float)waitCount / waitTotal;

            if (State == States.Open)
                boardHeightOffset = MathHelper.Lerp(0, boardHeightMax, waitRatio);
            else if (State == States.Close)
                boardHeightOffset = MathHelper.Lerp(boardHeightMax, 0, waitRatio);
            else if (State == States.Opened)
                boardHeightOffset = 0;
            else if (State == States.Closed)
                boardHeightOffset = boardHeightMax;

            updateDrawPosition();

            if (State == States.Open)
                boardAlpha = MathHelper.Lerp(1, 0, waitRatio);
            else if (State == States.Close)
                boardAlpha = MathHelper.Lerp(0, 1, waitRatio);
            else if (State == States.Opened)
                boardAlpha = 1;
            else if (State == States.Closed)
                boardAlpha = 0;

            // Update states
            if (State == States.Open && waitCount == 0)
                State = States.Opened;
            if (State == States.Close && waitCount == 0)
                State = States.Closed;

            // Update counter
            if (waitCount > 0)
                waitCount--;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            panelSprite.Alpha = boardAlpha;
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(sprite: panelSprite, position: panelDrawPosition);
            foreach ((var textLine, var textDrawPosition) in textLines.Zip(textDrawPositions))
                spriteBatch.DrawString(
                    font: textBitmapFont,
                    text: textLine,
                    position: textDrawPosition,
                    color: Color.Black * boardAlpha);
            spriteBatch.End();
        }
    }
    public class AnyKey : IObject
    {
        public bool Selected { get; private set; } = false;
        public bool Pressed { get; private set; } = false;
        public bool Released { get; private set; } = false;
        public int Priority { get => 0; set => throw new NotImplementedException(); }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {
            if (!Selected && (mouseState.LeftButton == ButtonState.Pressed ||
                mouseState.RightButton == ButtonState.Pressed ||
                keyboardState.GetPressedKeyCount() > 0 ||
                touchState.HasValue))
            {
                Selected = true;
                Pressed = true;
            }
            else
            {
                Pressed = false;
            }
            if (Selected && (mouseState.LeftButton != ButtonState.Pressed &&
                mouseState.RightButton != ButtonState.Pressed &&
                keyboardState.GetPressedKeyCount() == 0 &&
                !touchState.HasValue))
            {
                Selected = false;
                Released = true;
            }
            else
            {
                Released = false;
            }
        }
        public void StrictUpdate()
        {
        }
        public void Draw(SpriteBatch spriteBatch)
        {
        }
    }
    public class Dimmer : IObject, ITangible
    {
        private readonly Effect silhouetteEffect;
        private readonly EffectParameter overlayColorSilhouetteEffectParameter;
        private readonly Texture2D dimTexture;
        private float currAlpha;
        private const float maxAlpha = 0.75f;
        private int waitCount;
        private int waitTotal = 1;
        public Dimmer(ContentManager content, int width, int height)
        {
            Debug.Assert(width > 0);
            Debug.Assert(height > 0);
            Size = new Size(width, height);
            {
                dimTexture = new Texture2D(content.GetGraphicsDevice(), width, height);
                var data = Enumerable.Repeat(Color.Black, width * height).ToArray();
                dimTexture.SetData(data);
            }
            {
                silhouetteEffect = content.Load<Effect>("effects/silhouette_0");
                overlayColorSilhouetteEffectParameter = silhouetteEffect.Parameters["OverlayColor"];
            }
        }
        public void Dim()
        {
            waitCount = 14;
            waitTotal = 15;
            currAlpha = 0;
            State = States.Dim;
        }
        public void Brighten()
        {
            waitCount = 14;
            waitTotal = 15;
            currAlpha = maxAlpha;
            State = States.Brighten;
        }
        public enum States { None, Dim, Dimmed, Brighten }
        public States State { get; private set; } = States.None;
        public Color DimColor { get; set; } = Color.Black;
        public Vector2 Position { get; set; }
        public Size Size { get; }
        public int Priority { get; set; }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {            
        }
        public void StrictUpdate()
        {
            float waitRatio = (float)waitCount / waitTotal;

            if (State == States.Dim)
                currAlpha = MathHelper.Lerp(maxAlpha, 0, waitRatio);
            else if (State == States.Brighten)
                currAlpha = MathHelper.Lerp(0, maxAlpha, waitRatio);
            else if (State == States.None)
                currAlpha = 0;
            else if (State == States.Dimmed)
                currAlpha = maxAlpha;

            if (State == States.Dim && waitCount == 0)
                State = States.Dimmed;
            if (State == States.Brighten && waitCount == 0)
                State = States.None;

            if (waitCount > 0)
                waitCount--;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            if (State != States.None)
            {
                overlayColorSilhouetteEffectParameter.SetValue(DimColor.ToVector4());
                spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: silhouetteEffect);
                spriteBatch.Draw(texture: dimTexture, position: Position, color: Color.White * currAlpha);
                spriteBatch.End();
            }
        }
    }
    public class Panel : IObject, ITangible
    {        
        private readonly AnimatedSprite panelSprite;
        private readonly AnimatedSprite messageSprite;
        private readonly BitmapFont textBitmapFont;
        private readonly static Vector2 textDrawOffset = new Vector2(13, 13);
        private readonly static ReadOnlyDictionary<Modes, string> messageNameMap = new(new Dictionary<Modes, string>()
        {
            { Modes.CurrentScore, "current_score_0" },
            { Modes.HighScore, "high_score_0" },
        });
        private Vector2 truePosition;
        private Vector2 textDrawPosition;
        private int trueValue;
        private string stringValue;
        private float flashAlpha;
        private int waitCount;
        private int waitTotal = 1;
        public Panel(ContentManager content, Modes mode)
        {
            Mode = mode;
            Value = 0;
            {
                var spriteSheet = content.Load<SpriteSheet>("sprite_factory/panel_0.sf", new JsonContentLoader());
                panelSprite = new AnimatedSprite(spriteSheet);
                panelSprite.Origin = Vector2.Zero;
                panelSprite.Play("panel_0");
                Size = (Size)spriteSheet.TextureAtlas[0].Size;
            }
            {
                var spriteSheet = content.Load<SpriteSheet>("sprite_factory/texts_1.sf", new JsonContentLoader());
                messageSprite = new AnimatedSprite(spriteSheet);
                messageSprite.Origin = Vector2.Zero;
                messageSprite.Play(messageNameMap[mode]);
            }
            {
                textBitmapFont = content.Load<BitmapFont>("fonts/montserrat_0");
            }
        }
        public int Value 
        {
            get => trueValue;
            set
            {
                Debug.Assert(value >= 0);
                trueValue = value;
                stringValue = $"{trueValue}";
            }
        }
        public enum Modes { CurrentScore, HighScore };
        public Modes Mode { get; }
        public enum States { Normal, Flash }
        public States State { get; private set; } = States.Normal;
        public void Flash()
        {
            waitCount = 29;
            waitTotal = 30;
            flashAlpha = 0;
            State = States.Flash;
        }
        public Vector2 Position 
        {
            get => truePosition;
            set
            {
                truePosition = value;
                textDrawPosition = truePosition + textDrawOffset;
            }
        }
        public Size Size { get; }
        public int Priority { get => (int)Position.Y; set => throw new NotImplementedException(); }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {
            panelSprite.Update(gameTime);
            messageSprite.Update(gameTime);
        }
        public void StrictUpdate()
        {            
            if (State == States.Flash)
            {
                int halfTotal = waitTotal / 2;
                if (waitCount >= halfTotal)
                {
                    float waitRatio = (float)(waitCount - halfTotal) / halfTotal;
                    flashAlpha = MathHelper.Lerp(1, 0, waitRatio);                    
                }
                else
                {
                    float waitRatio = (float)waitCount / halfTotal;
                    flashAlpha = MathHelper.Lerp(0, 1, waitRatio);
                }
            }
            else
            {
                flashAlpha = 0;
            }

            if (State == States.Flash && waitCount == 0)
                State = States.Normal;

            if (waitCount > 0)
                waitCount--;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(sprite: panelSprite, position: truePosition);
            spriteBatch.Draw(sprite: messageSprite, position: truePosition);
            spriteBatch.DrawString(font: textBitmapFont, text: stringValue, position: textDrawPosition, color: Color.Black);
            spriteBatch.DrawString(font: textBitmapFont, text: stringValue, position: textDrawPosition, color: Color.White * flashAlpha);
            spriteBatch.End();
        }
    }
    public class ShineFood : IObject, ITangible, ILevelObject
    {
        private readonly Ball ball;
        private Point trueLevelPosition;
        public ShineFood(ContentManager content)
        {
            ball = new Ball(content, 4);
            ball.ShineAppear();
            State = States.Appear;
        }
        public enum States { Appear, Normal, Vanish, Gone }
        public States State { get; private set; }
        public void Vanish()
        {
            Debug.Assert(State == States.Normal);
            ball.ShineVanish();
            State = States.Vanish;
        }
        public Vector2 Position
        {
            get => ball.Position;
            set => throw new NotImplementedException();
        }
        public Size Size
        {
            get => ball.Size;
        }
        public int Priority
        {
            get => ball.Priority;
            set => throw new NotImplementedException();
        }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {
            if (State == States.Appear && ball.State == Ball.States.ShineNormal)
                State = States.Normal;

            if (State == States.Vanish && ball.State == Ball.States.Invisible)
                State = States.Gone;

            ball.Update(gameTime, mouseState, keyboardState, touchState);
        }
        public void StrictUpdate()
        {
            ball.StrictUpdate();
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            ball.Draw(spriteBatch);
        }
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
    public class Food : IObject, ITangible, ILevelObject
    {
        private readonly Ball ball;
        private Point trueLevelPosition;
        public Food(ContentManager content)
        {
            ball = new Ball(content, 4);
            ball.LongAppear();
            State = States.Appear;
        }
        public enum States { Appear, Normal, Vanish, Gone }
        public States State { get; private set; }
        public void Vanish()
        {
            Debug.Assert(State == States.Normal);
            ball.LongVanish();
            State = States.Vanish;
        }
        public Vector2 Position
        {
            get => ball.Position;
            set => throw new NotImplementedException();
        }
        public Size Size
        {
            get => ball.Size;
        }
        public int Priority
        {
            get => ball.Priority;
            set => throw new NotImplementedException();
        }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {
            if (State == States.Appear && ball.State == Ball.States.Normal)            
                State = States.Normal;

            if (State == States.Vanish && ball.State == Ball.States.Invisible)
                State = States.Gone;

            ball.Update(gameTime, mouseState, keyboardState, touchState);
        }
        public void StrictUpdate()
        {
            ball.StrictUpdate();
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            ball.Draw(spriteBatch);
        }
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
    public class Snake : IObject, ITangible, ILevelObject
    {
        private readonly List<SnakeBody> bodies = new();
        private readonly ContentManager content;    
        private readonly Dictionary<Point, SnakeBody> levelPositionBodyMap = new();
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
        public enum Modes { Normal, Shine }
        public enum Directions { Up, Down, Left, Right }
        public enum States { Normal, Appear, Move, Vanish, Gone }
        public States State { get; private set; } = States.Normal;
        public Modes Mode { get; set; } = Modes.Normal;
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
            Mode = Modes.Normal;
            State = States.Normal;
        }
        public int Count { get => bodies.Count; }
        public bool Headless { get => Count == 0; }
        public SnakeBody Head { get => bodies[0]; }
        public SnakeBody Tail { get => bodies.Last(); }        
        public IEnumerable<SnakeBody> Bodies { get => bodies; }
        public IReadOnlyDictionary<Point, SnakeBody> LevelPositionBodyMap { get => levelPositionBodyMap; } 
        public void CreateHead(Point startLevelPosition, int extraBodies = 0, Directions extraBodiesDirection = Directions.Down)
        {
            Debug.Assert(Headless);
            Debug.Assert(Mode == Modes.Normal);
            Debug.Assert(extraBodies >= 0);
            Debug.Assert(levelPositionBodyMap.Count == 0);
            bodies.Add(new SnakeBody(content) 
            { 
                LevelPosition = startLevelPosition, 
                MovementMode = SnakeBody.MovementModes.Shift 
            });            
            Head.LongAppear();
            levelPositionBodyMap[startLevelPosition] = Head;
            for (int i = 0; i < extraBodies; i++)
            {
                var newBody = new SnakeBody(content)
                {
                    LevelPosition = startLevelPosition + DirectionPoints[extraBodiesDirection].Multiply(i + 1),
                    MovementMode = SnakeBody.MovementModes.Shift
                };
                newBody.LongAppear();
                bodies.Add(newBody);
                levelPositionBodyMap[newBody.LevelPosition] = newBody;
            }            
            State = States.Appear;
        }
        public SnakeBody Move(bool growTail = false)
        {
            Debug.Assert(!Headless);
            Debug.Assert(State == States.Normal);
            Debug.Assert(bodies.All(x => 
                (x.State == Ball.States.Normal || x.State == Ball.States.ShineNormal) &&
                (x.MovementMode == SnakeBody.MovementModes.Shift && x.MovementState == SnakeBody.MovementStates.Stopped)));

            if (Mode == Modes.Shine && Head.State == Ball.States.Normal)
                Head.ShineAppear();
            if (Mode == Modes.Normal && Head.State == Ball.States.ShineNormal)
                Head.QuickAppear();

            SnakeBody newTail = null;
            if (growTail)
            {
                newTail = new SnakeBody(content);
                bodies.Add(newTail);
            }

            levelPositionBodyMap.Clear();
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
                    bodies[i].MovementMode = SnakeBody.MovementModes.Shift;
                }
                levelPositionBodyMap[bodies[i].LevelPosition] = bodies[i];
            }
            State = States.Move;

            return newTail;
        }
        public void Vanish()
        {
            Debug.Assert(!Headless);
            Debug.Assert(State == States.Normal);
            Debug.Assert(bodies.All(x => x.State == Ball.States.Normal || x.State == Ball.States.ShineNormal));
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
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {
            if ((State == States.Move || State == States.Appear) && bodies.All(x=>
                (x.State == Ball.States.Normal || x.State == Ball.States.ShineNormal) &&
                (x.MovementState == SnakeBody.MovementStates.Stopped)))
            {
                State = States.Normal;
            }
            if (State == States.Vanish && bodies.All(x => x.State == Ball.States.Invisible))
            {
                levelPositionBodyMap.Clear();
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
        private Point startLevelPosition;
        private Point nextLevelPosition;
        private MovementModes trueMovementMode = MovementModes.Normal;
        private int moveWait;
        private const int moveTotal = 4;
        private void updateBallPosition() =>
            ball.Position = new Vector2(trueLevelPosition.X* Size.Width, trueLevelPosition.Y* Size.Height);
        public SnakeBody(ContentManager content)
        {            
            ball = new Ball(content, ballId);
        }
        public enum MovementModes { Normal, Shift }
        public enum MovementStates { Stopped, Move }
        public MovementModes MovementMode 
        {
            get => trueMovementMode;
            set
            {                
                Debug.Assert(MovementState == MovementStates.Stopped);
                trueMovementMode = value;
            }
        }
        public MovementStates MovementState { get; private set; } = MovementStates.Stopped;
        public Ball.States State { get => ball.State; }
        public void ShineAppear() => ball.ShineAppear();
        public void ShineVanish() => ball.ShineVanish();
        public void QuickVanish() => ball.QuickVanish();
        public void QuickAppear() => ball.QuickAppear();
        public void LongAppear() => ball.LongAppear();
        public void LongVanish() => ball.LongVanish();
        public void UpdateFloatHeight(SnakeBody body) => ball.UpdateFloatHeight(body.ball);
        public int Priority { get => (int)Position.Y; set => throw new NotImplementedException(); }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {            
            ball.Update(gameTime, mouseState, keyboardState, touchState);
        }
        public void StrictUpdate()
        {
            if (MovementMode == MovementModes.Shift && MovementState == MovementStates.Move)
            {
                var moveRatio = (float)moveWait / moveTotal;
                ball.Position = new Vector2(
                    MathHelper.SmoothStep(nextLevelPosition.X * Size.Width, startLevelPosition.X * Size.Width, moveRatio),
                    MathHelper.SmoothStep(nextLevelPosition.Y * Size.Height, startLevelPosition.Y * Size.Height, moveRatio));
            }

            if (MovementMode == MovementModes.Shift && MovementState == MovementStates.Move && moveWait == 0)
            {
                trueLevelPosition = nextLevelPosition;
                updateBallPosition();
                MovementState = MovementStates.Stopped;
            }

            if (moveWait > 0)
                moveWait--;

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
                Debug.Assert(MovementState == MovementStates.Stopped);
                if (MovementMode == MovementModes.Normal)
                {
                    trueLevelPosition = value;
                    updateBallPosition();
                }
                else if (MovementMode == MovementModes.Shift)
                {
                    startLevelPosition = trueLevelPosition;
                    nextLevelPosition = value;
                    moveWait = moveTotal - 1;
                    MovementState = MovementStates.Move;

                }
            }
        }
    }
    public class Ball : IObject, ITangible
    {
        private readonly static Random random = new Random();
        private readonly ContentManager content;
        private readonly AnimatedSprite ballSprite;
        private readonly AnimatedSprite shadowSprite;
        private readonly Effect silhouetteEffect;
        private readonly EffectParameter silhouetteOverlayColorEffectParameter;
        private Color silhouetteColor = Color.Black;
        private const int totalBalls = 5;
        private readonly static Size size = new Size(32, 32);
        private readonly static Vector2 drawOffset = new Vector2(16, 16);
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
        private int effectCount;
        private int effectTotal = 1;
        private float shadowScale = 1.0f;
        private const float shadowAlpha = 0.5f;
        private float ballScale = 1.0f;
        private float ballAlpha = 1.0f;
        private float silhouetteAlpha = 0.0f;
        private float ballHeight = 0f;
        private const float ballMaxHeight = 32;
        private readonly List<LightningEffect> lightningEffects = new();
        private readonly List<ShineEffect> shineEffects = new();
        private readonly static Color lightningSilhouetteColor = new Color(182, 229, 254);
        private readonly static Color shineSilhouetteColor = new Color(40, 121, 202);
        private readonly static List<Vector2> shinePossibleDirections = Enumerable
            .Range(0, 8)
            .Select(x => (x + 4) * MathHelper.TwoPi / 32)
            .Select(x => new Vector2((float)Math.Cos(x), (float)-Math.Sin(x)))
            .ToList();
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
        public static void LoadAll(ContentManager content)
        {
            for (int i = 0; i < totalBalls; i++)
                content.Load<SpriteSheet>($"sprite_factory/power_balls_{1 + i}.sf", new JsonContentLoader());
            content.Load<SpriteSheet>($"sprite_factory/shadow_0.sf", new JsonContentLoader());
            content.Load<Effect>("effects/silhouette_0");
            LightningEffect.LoadAll(content);
            ShineEffect.LoadAll(content);
        }
        public Ball(ContentManager content, int id)
        {
            Trace.Assert(id >= 0 && id < totalBalls);
            ID = id;
            this.content = content;
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
                shadowSprite.Play("shadow_0");
            }
            {
                silhouetteEffect = content.Load<Effect>("effects/silhouette_0");
                silhouetteOverlayColorEffectParameter = silhouetteEffect.Parameters["OverlayColor"];                
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
        public enum States 
        { 
            Normal, QuickVanish, QuickAppear, LongVanish, LongAppear, Invisible, 
            LightningAppear, LightningNormal, LightningVanish,
            ShineAppear, ShineNormal, ShineVanish
        }
        public States State { get; private set; } = States.Normal;
        public void ShineAppear()
        {
            State = States.ShineAppear;
            waitCount = 3;
            waitTotal = 4;            
            effectTotal = 30;
            shadowScale = 1;
            ballScale = 1;
            ballAlpha = 1;
            ballHeight = 0;
            silhouetteAlpha = 1;
            silhouetteColor = shineSilhouetteColor;
        }
        public void ShineVanish()
        {
            State = States.ShineVanish;
            waitCount = 3;
            waitTotal = 4;
            effectTotal = 30;
            shadowScale = 1;
            ballScale = 1;
            ballAlpha = 0;
            ballHeight = 0;
            silhouetteAlpha = 1;
            silhouetteColor = shineSilhouetteColor;
        }
        public void LightningAppear()
        {
            State = States.LightningAppear;
            waitCount = 3;
            waitTotal = 4;            
            effectTotal = 30;
            shadowScale = 1;
            ballScale = 1;
            ballAlpha = 1;
            ballHeight = 0;
            silhouetteAlpha = 1;
            silhouetteColor = lightningSilhouetteColor;
        }
        public void LightningVanish()
        {
            State = States.LightningVanish;
            waitCount = 3;
            waitTotal = 4;
            effectTotal = 30;
            shadowScale = 1;
            ballScale = 1;
            ballAlpha = 0;
            ballHeight = 0;
            silhouetteAlpha = 1;
            silhouetteColor = lightningSilhouetteColor;
        }
        public void QuickVanish()
        {
            State = States.QuickVanish;
            waitCount = 3;
            waitTotal = 4;
            shadowScale = 1;
            ballScale = 1;
            ballAlpha = 1;
            ballHeight = 0;
            silhouetteAlpha = 0;
            silhouetteColor = Color.Black;
        }
        public void QuickAppear()
        {
            State = States.QuickAppear;
            waitCount = 3;
            waitTotal = 4;
            shadowScale = 0;
            ballScale = 0;
            ballAlpha = 1;
            ballHeight = 0;
            silhouetteAlpha = 0;            
            silhouetteColor = Color.Black;
        }
        public void LongVanish()
        {
            State = States.LongVanish;
            waitCount = 7;
            waitTotal = 8;
            shadowScale = 1;
            ballScale = 1;
            ballAlpha = 1;            
            ballHeight = 0;
            silhouetteAlpha = 0;
            silhouetteColor = Color.Black;
        }
        public void LongAppear()
        {
            State = States.LongAppear;
            waitCount = 7;
            waitTotal = 8;
            shadowScale = 0;
            ballScale = 1;
            ballAlpha = 0;
            ballHeight = ballMaxHeight;
            silhouetteAlpha = 1;            
            silhouetteColor = Color.Black;
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
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {
            // Clear out any finished effects.
            lightningEffects.RemoveAll(x => x.State == LightningEffect.States.Gone);
            shineEffects.RemoveAll(x => x.State == ShineEffect.States.Gone);

            // Perform all other game object updates.
            ballSprite.Update(gameTime);
            shadowSprite.Update(gameTime);
            foreach (var lightningEffect in lightningEffects)
                lightningEffect.Update(gameTime, mouseState, keyboardState, touchState);
            foreach (var shineEffect in shineEffects)
                shineEffect.Update(gameTime, mouseState, keyboardState, touchState);
        }
        public void StrictUpdate()
        {
            // Update properties affected by state.
            float waitRatio = (float)waitCount / waitTotal;
            float waitHighRatio = MathHelper.Clamp(2 * waitRatio - 1, 0, 1);
            float waitLowRatio = MathHelper.Clamp(2 * waitRatio, 0, 1);

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

            if (State == States.LongVanish || State == States.LightningVanish || State == States.ShineVanish)
                ballAlpha = MathHelper.Lerp(0, 1, waitRatio);
            else if (State == States.LongAppear || State == States.LightningAppear || State == States.ShineAppear)
                ballAlpha = MathHelper.Lerp(1, 0, waitRatio);
            else 
                ballAlpha = 1f;

            if (State == States.LongVanish)
                silhouetteAlpha = MathHelper.Lerp(1, 0, waitHighRatio);
            else if (State == States.LongAppear)
                silhouetteAlpha = MathHelper.Lerp(0, 1, waitLowRatio);
            else if (State == States.LightningAppear || State == States.LightningVanish)
                silhouetteAlpha = waitCount % 2;
            else if (State == States.ShineAppear || State == States.ShineVanish)
                silhouetteAlpha = waitCount % 2;
            else if (State == States.LightningNormal)
            {
                int waitHalf = waitTotal / 2;                
                if (waitCount >= waitHalf)                
                    silhouetteAlpha = MathHelper.Lerp(1, 0, (float)(waitCount - waitHalf) / waitHalf);                
                else                
                    silhouetteAlpha = MathHelper.Lerp(0, 1, (float)waitCount / waitHalf);                
            }
            else if (State == States.ShineNormal)
            {
                int waitHalf = waitTotal / 2;                
                if (waitCount >= waitHalf)
                    silhouetteAlpha = MathHelper.Lerp(1f, 0, (float)(waitCount - waitHalf) / waitHalf);
                else
                    silhouetteAlpha = MathHelper.Lerp(0, 1f, (float)waitCount / waitHalf);
            }
            else
                silhouetteAlpha = 0f;            

            if (State == States.LightningAppear || State == States.LightningNormal || State == States.LightningVanish)
                silhouetteColor = lightningSilhouetteColor;
            else if (State == States.ShineAppear || State == States.ShineNormal || State == States.ShineVanish)
                silhouetteColor = shineSilhouetteColor;
            else
                silhouetteColor = Color.Black;

            if (State == States.LongVanish)
                ballHeight = MathHelper.Lerp(ballMaxHeight, 0, waitRatio);
            else if (State == States.LongAppear)
                ballHeight = MathHelper.Lerp(0, ballMaxHeight, waitRatio);
            else
                ballHeight = 0;

            // Add effects.
            if ((State == States.LightningAppear || State == States.LightningNormal || State == States.LightningVanish) &&
                effectCount % 15 == 0)
            {
                var lightningEffect = new LightningEffect(content)
                {
                    Position = ballDrawPosition + new Vector2(
                        (random.NextSingle() - 0.5f) * Size.Width,
                        (random.NextSingle() - 0.5f) * Size.Height),
                    Scale = 0.75f
                };
                lightningEffect.Vanish();
                lightningEffects.Add(lightningEffect);
            }

            if ((State == States.ShineAppear || State == States.ShineNormal || State == States.ShineVanish) &&
                effectCount % 10 == 0)
            {
                var direction = shinePossibleDirections[random.Next(shinePossibleDirections.Count)];
                var shineEffect = new ShineEffect(content)
                {
                    Position = ballDrawPosition + 8 * direction,
                    Gravity = new Vector2(0, 0.25f),
                    Velocity = direction * 1.5f,
                    Scale = 0.25f + (random.NextSingle() -0.5f) * 0.25f
                };
                shineEffect.Vanish();
                shineEffects.Add(shineEffect);
            }

            // Update Counters
            if (waitCount > 0)
                waitCount--;
            else if (State == States.LightningNormal || State == States.ShineNormal)
                waitCount = waitTotal - 1;

            if (effectCount > 0)
                effectCount--;
            else if (State == States.LightningAppear || State == States.LightningNormal || State == States.LightningVanish ||
                     State == States.ShineAppear || State == States.ShineNormal || State == States.ShineVanish)
                effectCount = effectTotal - 1;

            // Update FSM states.
            if ((State == States.QuickVanish || State == States.LongVanish || State == States.LightningVanish || State == States.ShineVanish) && waitCount == 0)
                State = States.Invisible;

            if ((State == States.QuickAppear || State == States.LongAppear) && waitCount == 0)
                State = States.Normal;

            if (State == States.LightningAppear && waitCount == 0)
            {
                waitCount = 29;
                waitTotal = 30;
                State = States.LightningNormal;
            }

            if (State == States.ShineAppear && waitCount == 0)
            {
                waitCount = 59;
                waitTotal = 60;
                State = States.ShineNormal;
            }

            // Update float height
            updateFloatHeight();

            // Since float height changes every strict update
            // need to always update draw positions.
            updateDrawPositions();

            foreach (var shineEffect in shineEffects)
                shineEffect.StrictUpdate();
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            if (State != States.Invisible)
            {
                shadowSprite.Alpha = shadowAlpha * ballAlpha;
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                spriteBatch.Draw(sprite: shadowSprite, position: shadowDrawPosition, rotation: 0, scale: new Vector2(shadowScale));
                spriteBatch.End();

                ballSprite.Alpha = ballAlpha;
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                spriteBatch.Draw(sprite: ballSprite, position: ballDrawPosition, rotation: 0, scale: new Vector2(ballScale));
                spriteBatch.End();

                ballSprite.Alpha = ballAlpha * silhouetteAlpha;
                silhouetteOverlayColorEffectParameter.SetValue(silhouetteColor.ToVector4());
                spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: silhouetteEffect);
                spriteBatch.Draw(sprite: ballSprite, position: ballDrawPosition, rotation: 0, scale: new Vector2(ballScale));
                spriteBatch.End();

                foreach (var lightningEffect in lightningEffects)
                    lightningEffect.Draw(spriteBatch);
                foreach (var shineEffects in shineEffects)
                    shineEffects.Draw(spriteBatch);
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
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
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
        private readonly static ReadOnlyDictionary<Modes, string> keyMap = new(new Dictionary<Modes, string>()
        {
            { Modes.Up, "ArrowUp" },
            { Modes.Down, "ArrowDown" },
            { Modes.Left, "ArrowLeft" },
            { Modes.Right, "ArrowRight" },
            { Modes.Pause, "Escape" },
            { Modes.Start, "Enter" },
        });
        private bool serviceKeysPressedSelectOccurred = false;        
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
            {
                BrowserService.ServiceKeysPressedSet.Add(ServiceKeysPressed);
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
        public void ServiceKeysPressed(IReadOnlyCollection<string> keysPressed)
        {
            if (!Selected && keysPressed.Contains(keyMap[Mode]))
            {                
                Selected = true;
                Pressed = true;
                serviceKeysPressedSelectOccurred = true;
            }
            else if (Selected && !keysPressed.Contains(keyMap[Mode]))
            {                
                Selected = false;
                Released = true;
                serviceKeysPressedSelectOccurred = false;                
            }
        }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, Vector2? touchState)
        {            
            if (!Selected && ((
                mouseState.LeftButton == ButtonState.Pressed && 
                mouseState.Position.X >= Position.X && mouseState.X < (Position.X + Size.Width) &&
                mouseState.Position.Y >= Position.Y && mouseState.Y < (Position.Y + Size.Height)) || (                
                touchState.HasValue &&
                touchState.Value.X >= Position.X && touchState.Value.X < (Position.X + Size.Width) &&
                touchState.Value.Y >= Position.Y && touchState.Value.Y < (Position.Y + Size.Height))))
            {                              
                Selected = true;
                Pressed = true;                
            }            
            else
            {
                Pressed = false;
            }

            if (Selected && !serviceKeysPressedSelectOccurred && ((
                mouseState.LeftButton != ButtonState.Pressed) &&                 
                !touchState.HasValue))
            {                                
                Selected = false;
                Released = true;
            }
            else
            {
                Released = false;
            }

            if (Selected)
                visualSprite.Play("selected_0");
            else
                visualSprite.Play("unselected_0");

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
            spriteBatch.Draw(sprite: messageSprite, position: Position);
            spriteBatch.End();
        }
    }
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class SnakeExtremeGame : Game
    {
        private class LightningObject
        {
            public LightningObstacle obstacle;
            public int turnWait;
            public bool removedByShine;
        }
        private readonly static Random random = new Random();
        private GraphicsDeviceManager graphics;        
        private SpriteBatch spriteBatch;
        private TiledMap levelTiledMap;
        private TiledMapRenderer levelTiledMapRenderer;
        private List<IObject> gameObjects = new();
        private Button upButton, downButton, leftButton, rightButton, startButton, pauseButton;
        private Snake snake;
        private Food food, newFood;
        private List<Obstacle> obstacles = new();
        private List<LightningObject> lightningObjects = new();
        private List<ShineFood> shineFoods = new();
        private Obstacle removeObstacle;
        private LightningObject removeLightningObject;
        private ShineFood removeShineFood;
        private Panel currentScorePanel, highScorePanel;
        private Dimmer dimmer;
        private Board board;
        private AnyKey anyKey;
        private Sound foodSound, destroySound, pauseSound, obstacleSound, moveSound;
        private Sound shineFoodAppearSound, shineFoodPickUpSound, shineFoodRemoveObstacle;
        private Music backgroundMusic;
        private Queue<Snake.Directions> newDirections = new();
        private const float musicMaxVolume = 0.2f;
        private const float musicLowVolume = 0.1f;
        private const int maxNewDirections = 3;
        private float strictTimePassed;
        private const float strictTimeAmount = (float)1 / 30;
        private const int minWait = 3;
        private const int maxWait = 6;
        private const int foodPerReachingMinWait = 40;
        private const int scorePerLevelUpdate = 5;
        private const int obstacleStartThreshold = 1 * scorePerLevelUpdate;
        private const int obstaclesPerLevelUpdate = 4;
        private const int lightningStartThreshold = 2 * scorePerLevelUpdate;
        private const int lightningPerLevelUpdate = 4;
        private const int lightningActiveTurns = 5;
        private const int shineStartThreshold = 3 * scorePerLevelUpdate;
        private const int shineMaxAmount = 2;
        private const int extraBodies = 3;
        private int waitCount;
        private int waitTotal;
        private bool gameDestroy = false;
        private List<Point> levelCorners = new();
        private List<Point> levelPossiblePositions = new();
        private CenterScreen centerScreen;
        private List<float> deltaTimes = new();
        private List<float> updateTimes = new();
        private Stopwatch updateStopwatch = new();        
        private const int deltaTimesPerUPS = 2 * 60;
        private const int updateTimesPerUPS = 2 * 60;
        private Point getRandomLevelPosition(IEnumerable<Point> additionalPositions = null)
        {
            if (additionalPositions == null)
                additionalPositions = [];
            var availablePositions = levelPossiblePositions.Except(gameObjects.OfType<ILevelObject>().Select(x => x.LevelPosition).Chain(additionalPositions)).ToList();
            return availablePositions[random.Next(availablePositions.Count)];
        }
        private void updateLightningObjects()
        {
            foreach (var obj in lightningObjects)
            {
                var obstacleBodyOverlap = snake.LevelPositionBodyMap.ContainsKey(obj.obstacle.LevelPosition);
                if (obj.turnWait == 0 && !obj.removedByShine)
                {
                    if (obj.obstacle.State == LightningObstacle.States.Normal)
                    {
                        obj.obstacle.Vanish();
                        obj.turnWait = lightningActiveTurns;
                    }
                    else if (obj.obstacle.State == LightningObstacle.States.Gone &&
                             (!obstacleBodyOverlap || snake.Tail.LevelPosition == obj.obstacle.LevelPosition))
                    {
                        obj.obstacle.Appear();
                        obj.turnWait = lightningActiveTurns;
                    }
                }
                else
                {
                    obj.turnWait--;
                }
            }
        }
        private void moveSnake(bool growTail = false)
        {
            var snakeBody = snake.Move(growTail: growTail);
            if (snakeBody != null)
                gameObjects.Add(snakeBody);
            moveSound.Play();
        }
        public SnakeExtremeGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }
        public enum FoodStates { Normal, NewFood }
        public enum GameStates { Start, Create, Wait, Action, Destroy }
        public enum ShineStates { Normal, NewShine, RemoveObstacle }
        public enum PauseStates { Resumed, Pause, Paused, Resume }
        public GameStates GameState { get; private set; } = GameStates.Start;
        public FoodStates FoodState { get; private set; } = FoodStates.Normal;
        public PauseStates PauseState { get; private set; } = PauseStates.Resumed;
        public ShineStates ShineState { get; private set; } = ShineStates.Normal;

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
            Ball.LoadAll(Content);

            levelTiledMap = Content.Load<TiledMap>("tiled_project/level_1");
            levelTiledMapRenderer = new TiledMapRenderer(GraphicsDevice, levelTiledMap);
            var maskLayer = levelTiledMap.TileLayers.Where(x => x.Name == "mask_0").First();                      
            for (int x = 0; x < levelTiledMap.Width; x++)
            {
                for (int y = 0; y < levelTiledMap.Height; y++)
                {
                    var tile = maskLayer.GetTile((ushort)x, (ushort)y);
                    var position = new Vector2(x * levelTiledMap.TileWidth, y * levelTiledMap.TileHeight);
                    if (tile.GlobalIdentifier == 4097)
                    {
                        levelCorners.Add(new Point(x, y));
                    }
                    else if (tile.GlobalIdentifier == 4098)
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
                    else if (tile.GlobalIdentifier == 4105)
                    {
                        currentScorePanel = new Panel(Content, Panel.Modes.CurrentScore)
                        {
                            Position = position
                        };
                        gameObjects.Add(currentScorePanel);
                    }
                    else if (tile.GlobalIdentifier == 4106)
                    {
                        highScorePanel = new Panel(Content, Panel.Modes.HighScore)
                        {
                            Position = position
                        };
                        gameObjects.Add(highScorePanel);
                    }
                }
            }

            Debug.Assert(upButton != null);
            Debug.Assert(downButton != null);
            Debug.Assert(leftButton != null);
            Debug.Assert(rightButton != null);
            Debug.Assert(currentScorePanel != null);
            Debug.Assert(highScorePanel != null);
            Debug.Assert(levelCorners.Count == 2);

            {                
                var minX = levelCorners.Select(x => x.X).Min();
                var maxX = levelCorners.Select(x => x.X).Max();
                var minY = levelCorners.Select(x => x.Y).Min();
                var maxY = levelCorners.Select(x => x.Y).Max();
                for (int x = minX; x <= maxX; x++)                
                    for (int y = minY; y <= maxY; y++)                    
                        levelPossiblePositions.Add(new Point(x, y));                                    
            }

            anyKey = new AnyKey();
            gameObjects.Add(anyKey);

            dimmer = new Dimmer(Content, levelTiledMap.WidthInPixels, levelTiledMap.HeightInPixels)
            {
                Priority = levelTiledMap.WidthInPixels * levelTiledMap.HeightInPixels
            };
            gameObjects.Add(dimmer);

            board = new Board(Content)
            {
                Priority = dimmer.Priority + 1,
                Text = "Welcome to Snake Extreme! <n> <n> " +
                       "Use arrow keys / buttons to direct the snake. Avoid your tail, level boundaries, and the obstacle spheres! " +
                       "Eat food spheres to gain points and earn a high score! <n> <n> " +
                       "Use Enter / start button to start or end the game. " +
                       "Use Escape / pause button to pause the game. <n> <n> " +
                       "Hitting any key resumes!  <n> <n> " +
                       "Credits: <n> " +
                       "Andrew Powell - Game Designer / Programmer - andrewandrepowell.itch.io <n> " +
                       "Rafael Matos - Level Tile Assets, Sphere / Torch Assets - rafaelmatos.itch.io <n> " +
                       "Butter Milk - GUI Element Assets - butterymilk.itch.io <n> " +
                       "Julieta Ulanosvsky - Montserrat Font Asset - github.com/JulietaUla <n> " +
                       "Joel Francis Burford - Sound Effects - joelfrancisburford.itch.io <n> " +
                       "Ansimuz - Music - ansimuz.itch.io"
            };
            board.Position = new Vector2(
                (levelTiledMap.WidthInPixels - board.Size.Width) / 2,
                (levelTiledMap.HeightInPixels - board.Size.Height) / 2);
            gameObjects.Add(board);

            moveSound = new Sound(Content, "sounds/move_0") { Volume = 0.25f };
            foodSound = new Sound(Content, "sounds/food_0");
            destroySound = new Sound(Content, "sounds/destroy_0");
            pauseSound = new Sound(Content, "sounds/pause_0");
            obstacleSound = new Sound(Content, "sounds/obstacle_0");            
            shineFoodPickUpSound = new Sound(Content, "sounds/shine_food_0");
            shineFoodAppearSound = new Sound(Content, "sounds/shine_food_1");
            shineFoodRemoveObstacle = new Sound(Content, "sounds/shine_food_2");
            backgroundMusic = new Music(Content, "sounds/music_0");
            backgroundMusic.Volume = musicLowVolume;
            backgroundMusic.Play();
            gameObjects.Add(backgroundMusic);

            snake = new Snake(Content);
            gameObjects.Add(snake);

            centerScreen = new CenterScreen(GraphicsDevice, new Size(levelTiledMap.WidthInPixels, levelTiledMap.HeightInPixels));

            dimmer.Dim();
            board.Open();
            pauseSound.Play();                     
            PauseState = PauseStates.Pause;            
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
            // Start the stop watch to track how long the update is taking.
            // updateStopwatch.Start();

            // Acquire input states.
            MouseState mouseState = centerScreen.UpdateMouseState(Mouse.GetState());
            KeyboardState keyboardState = Keyboard.GetState();
            Vector2? touchState = (BrowserService.Touches.Count > 0 ?
                centerScreen.UpdateTouchState(BrowserService.Touches.Dequeue()) : null);

            // Service buttons
            {
                Snake.Directions? newDirection = null;
                if (upButton.Pressed)
                    newDirection = Snake.Directions.Up;
                if (downButton.Pressed)
                    newDirection = Snake.Directions.Down;
                if (leftButton.Pressed)
                    newDirection = Snake.Directions.Left;
                if (rightButton.Pressed)
                    newDirection = Snake.Directions.Right;
                if (newDirection.HasValue && 
                    PauseState == PauseStates.Resumed && 
                    newDirections.Count < maxNewDirections &&
                    (GameState == GameStates.Start || GameState == GameStates.Create ||
                     GameState == GameStates.Wait || GameState == GameStates.Action))
                {
                    newDirections.Enqueue(newDirection.Value);
                }
            }

            if (startButton.Pressed)
            {
                if (PauseState == PauseStates.Resumed)
                {
                    if (GameState == GameStates.Start)
                    {
                        Debug.Assert(snake.Headless);
                        Debug.Assert(food == null);
                        Console.WriteLine($"Game Objects: {gameObjects.Count}");

                        var startLevelPosition = new Point(
                            (int)levelCorners.Select(x => x.X).Average(),
                            (int)levelCorners.Select(x => x.Y).Average());
                        snake.CreateHead(startLevelPosition, extraBodies, Snake.Directions.Down);
                        snake.Direction = Snake.Directions.Up;                        
                        foreach (var body in snake.Bodies)
                            gameObjects.Add(body);

                        food = new Food(Content) { LevelPosition = getRandomLevelPosition() };
                        gameObjects.Add(food);

                        currentScorePanel.Value = 0;
                        currentScorePanel.Flash();                        

                        GameState = GameStates.Create;
                    }
                    else
                    {
                        gameDestroy = true;
                    }
                }
            }

            if (pauseButton.Pressed)
            {
                if (PauseState == PauseStates.Resumed)
                {
                    dimmer.Dim();
                    board.Open();
                    pauseSound.Play();
                    backgroundMusic.SmoothChange(musicLowVolume);
                    PauseState = PauseStates.Pause;
                }
            }

            if (anyKey.Pressed)
            {
                if (PauseState == PauseStates.Paused)
                {                    
                    dimmer.Brighten();
                    board.Close();
                    pauseSound.Play();
                    backgroundMusic.SmoothChange(musicMaxVolume);
                    PauseState = PauseStates.Resume;                    
                }
            }

            // Service state changes.
            if (PauseState == PauseStates.Pause && dimmer.State == Dimmer.States.Dimmed && board.State == Board.States.Opened && backgroundMusic.State == Music.States.Playing)
                PauseState = PauseStates.Paused;

            if (PauseState == PauseStates.Resume && dimmer.State == Dimmer.States.None && board.State == Board.States.Closed && backgroundMusic.State == Music.States.Playing)
                PauseState = PauseStates.Resumed;

            if (PauseState == PauseStates.Resumed && GameState == GameStates.Create && snake.State == Snake.States.Normal && food.State == Food.States.Normal)
            {
                waitTotal = maxWait;
                waitCount = waitTotal - 1;
                GameState = GameStates.Wait;
            }

            if (PauseState == PauseStates.Resumed && GameState == GameStates.Wait && waitCount == 0)
            {
                snake.Direction = (newDirections.Count > 0 ? newDirections.Dequeue() : snake.Direction);
                var snakeNextLevelPosition = snake.LevelPosition + Snake.DirectionPoints[snake.Direction];

                var minX = levelCorners.Select(x => x.X).Min();
                var maxX = levelCorners.Select(x => x.X).Max();
                var minY = levelCorners.Select(x => x.Y).Min();
                var maxY = levelCorners.Select(x => x.Y).Max();

                var aboutToCollideWithObstacle =
                    obstacles.Any(x => x.LevelPosition == snakeNextLevelPosition) ||
                    lightningObjects.Any(x => (x.obstacle.State == LightningObstacle.States.Normal && x.obstacle.LevelPosition == snakeNextLevelPosition && x.turnWait != 0) ||
                                              (x.obstacle.State == LightningObstacle.States.Gone && x.obstacle.LevelPosition == snakeNextLevelPosition && x.turnWait == 0));

                // Check for game over condition.
                if (snakeNextLevelPosition.X < minX || snakeNextLevelPosition.X > maxX ||
                    snakeNextLevelPosition.Y < minY || snakeNextLevelPosition.Y > maxY ||
                    snake.Bodies.Any(x => x.LevelPosition == snakeNextLevelPosition && x != snake.Tail) ||
                    (aboutToCollideWithObstacle && snake.Mode == Snake.Modes.Normal) ||
                    gameDestroy)
                {
                    gameDestroy = false;

                    snake.Vanish();
                    food.Vanish();
                    foreach (var food in shineFoods)
                        food.Vanish();
                    foreach (var obstacle in obstacles)
                        obstacle.Vanish();
                    foreach (var x in lightningObjects)
                        if (x.obstacle.State == LightningObstacle.States.Normal)
                            x.obstacle.Vanish();

                    if (currentScorePanel.Value > highScorePanel.Value)
                    {
                        highScorePanel.Value = currentScorePanel.Value;
                        highScorePanel.Flash();
                    }

                    destroySound.Play();

                    GameState = GameStates.Destroy;
                }
                // Check for food.
                else if (snakeNextLevelPosition == food.LevelPosition)
                {
                    // Move / grow the snake.
                    moveSnake(growTail: (int)(levelPossiblePositions.Count * 0.40f) >= snake.Count);

                    // Generate food.
                    Debug.Assert(newFood == null);
                    food.Vanish();
                    newFood = new Food(Content) { LevelPosition = getRandomLevelPosition([snakeNextLevelPosition]) };
                    gameObjects.Add(newFood);
                    foodSound.Play();

                    // Update score.
                    currentScorePanel.Value += 1;
                    currentScorePanel.Flash();

                    // Time to generate interactable spheres.
                    if (currentScorePanel.Value % scorePerLevelUpdate == 0)
                    {
                        var obstacleCreated = false;
                        var shineFoodCreated = false;

                        // Generate obstacle.
                        if (currentScorePanel.Value >= obstacleStartThreshold &&
                            (int)(levelPossiblePositions.Count * 0.20f) >= obstacles.Count)
                        {
                            for (int i = 0; i < obstaclesPerLevelUpdate; i++)
                            {
                                var obstacle = new Obstacle(Content) { LevelPosition = getRandomLevelPosition([snakeNextLevelPosition]) };
                                obstacles.Add(obstacle);
                                gameObjects.Add(obstacle);
                            }
                            obstacleCreated = true;
                        }

                        // Generate lightning obstacle.
                        if (currentScorePanel.Value >= lightningStartThreshold &&
                            (int)(levelPossiblePositions.Count * 0.20f) >= obstacles.Count)
                        {
                            for (int i = 0; i < lightningPerLevelUpdate; i++)
                            {
                                var obj = new LightningObject()
                                {
                                    obstacle = new LightningObstacle(Content) { LevelPosition = getRandomLevelPosition([snakeNextLevelPosition]) },
                                    turnWait = lightningActiveTurns,
                                    removedByShine = false
                                };
                                lightningObjects.Add(obj);
                                gameObjects.Add(obj.obstacle);
                            }
                            obstacleCreated = true;
                        }

                        // Generate shine food.
                        if (currentScorePanel.Value >= shineStartThreshold &&
                            shineFoods.Count < shineMaxAmount)
                        {
                            var shineFood = new ShineFood(Content) { LevelPosition = getRandomLevelPosition([snakeNextLevelPosition]) };
                            shineFoods.Add(shineFood);
                            gameObjects.Add(shineFood);
                            shineFoodCreated = true;
                        }

                        // Play sounds if conditions were met.
                        if (obstacleCreated)
                            obstacleSound.Play();
                        if (shineFoodCreated)
                            shineFoodAppearSound.Play();
                    }

                    updateLightningObjects();

                    // Update state
                    FoodState = FoodStates.NewFood;
                    GameState = GameStates.Action;
                }
                else
                {
                    Debug.Assert(removeShineFood == null);
                    removeShineFood = shineFoods.Where(x => x.LevelPosition == snakeNextLevelPosition).FirstOrDefault();

                    // Devour the obstacle / lose shine.
                    if (aboutToCollideWithObstacle && snake.Mode == Snake.Modes.Shine)
                    {
                        Debug.Assert(removeObstacle == null);
                        Debug.Assert(removeLightningObject == null);
                        removeObstacle = obstacles.Where(x => x.LevelPosition == snakeNextLevelPosition).FirstOrDefault();
                        removeLightningObject = lightningObjects.Where(x => x.obstacle.LevelPosition == snakeNextLevelPosition).FirstOrDefault();
                        Debug.Assert((removeObstacle != null ? 1 : 0) + (removeLightningObject != null ? 1 : 0) == 1);

                        if (removeObstacle != null)
                        {
                            removeObstacle.Vanish();
                        }
                        else if (removeLightningObject != null)
                        {
                            removeLightningObject.removedByShine = true;
                            if (removeLightningObject.obstacle.State == LightningObstacle.States.Normal)
                                removeLightningObject.obstacle.Vanish();
                        }

                        shineFoodRemoveObstacle.Play();

                        snake.Mode = Snake.Modes.Normal;
                        ShineState = ShineStates.RemoveObstacle;
                    }
                    // Pick up shine.
                    else if (removeShineFood != null)
                    {
                        removeShineFood.Vanish();

                        shineFoodPickUpSound.Play();

                        snake.Mode = Snake.Modes.Shine;
                        ShineState = ShineStates.NewShine;
                    }
                    // Shine state not impacted.
                    else
                    {
                        ShineState = ShineStates.Normal;
                    }

                    // Move the snake.
                    moveSnake(growTail: false);

                    // Update the counts on the lightning objects.
                    updateLightningObjects();

                    // Set the next state.
                    FoodState = FoodStates.Normal;
                    GameState = GameStates.Action;
                }
            }

            if (PauseState == PauseStates.Resumed &&
                GameState == GameStates.Action &&
                FoodState == FoodStates.Normal)
            {
                if (ShineState == ShineStates.RemoveObstacle)
                {
                    if (removeObstacle != null && removeObstacle.State == Obstacle.States.Gone)
                    {
                        obstacles.Remove(removeObstacle);
                        gameObjects.Remove(removeObstacle);
                        removeObstacle = null;
                        ShineState = ShineStates.Normal;
                    }
                    else if (removeLightningObject != null && removeLightningObject.obstacle.State == LightningObstacle.States.Gone)
                    {
                        Debug.Assert(removeLightningObject.removedByShine);
                        lightningObjects.Remove(removeLightningObject);
                        gameObjects.Remove(removeLightningObject.obstacle);
                        removeLightningObject = null;
                        ShineState = ShineStates.Normal;
                    }                    
                }
                else if (ShineState == ShineStates.NewShine && removeShineFood.State == ShineFood.States.Gone)
                {
                    shineFoods.Remove(removeShineFood);
                    gameObjects.Remove(removeShineFood);
                    removeShineFood = null;
                    ShineState = ShineStates.Normal;
                }
            }

            if (PauseState == PauseStates.Resumed && 
                GameState == GameStates.Action &&
                ShineState == ShineStates.Normal &&
                snake.State == Snake.States.Normal && 
                obstacles.All(x => x.State == Obstacle.States.Normal) &&
                lightningObjects.All(x => x.obstacle.State == LightningObstacle.States.Normal || (x.obstacle.State == LightningObstacle.States.Gone)) &&
                shineFoods.All(x => x.State == ShineFood.States.Normal))
            {
                if (FoodState == FoodStates.NewFood && food.State == Food.States.Gone && newFood.State == Food.States.Normal)
                {
                    gameObjects.Remove(food);
                    food = newFood;
                    newFood = null;

                    waitTotal = (int)MathHelper.Clamp(MathHelper.Lerp(maxWait, minWait, 
                        (float)currentScorePanel.Value / foodPerReachingMinWait), minWait, maxWait);                    

                    waitCount = waitTotal - 1;
                    GameState = GameStates.Wait;
                }
                else if (FoodState == FoodStates.Normal)
                {
                    Debug.Assert(food.State == Food.States.Normal);
                    Debug.Assert(newFood == null);

                    waitCount = waitTotal - 1;
                    GameState = GameStates.Wait;
                }
            }

            if (PauseState == PauseStates.Resumed && GameState == GameStates.Destroy && 
                snake.State == Snake.States.Gone && 
                food.State == Food.States.Gone && 
                obstacles.All(x => x.State == Obstacle.States.Gone) &&
                lightningObjects.All(x => x.obstacle.State == LightningObstacle.States.Gone))
            {
                newDirections.Clear();

                foreach (var body in snake.Bodies)
                    gameObjects.Remove(body);
                snake.Clear();
                snake.Mode = Snake.Modes.Normal;

                gameObjects.Remove(food);
                food = null;

                foreach (var food in shineFoods)
                    gameObjects.Remove(food);
                shineFoods.Clear();

                foreach (var obstacle in obstacles)
                    gameObjects.Remove(obstacle);
                obstacles.Clear();

                foreach (var obj in lightningObjects)
                    gameObjects.Remove(obj.obstacle);
                lightningObjects.Clear();

                GameState = GameStates.Start;
            }

            // Update center screen
            centerScreen.Update();

            // Perform level and game object updates.
            {
                levelTiledMapRenderer.Update(gameTime);
                foreach (var gameObject in gameObjects)
                    gameObject.Update(gameTime, mouseState, keyboardState, touchState);
            }

            // Perform strict time updates.
            {
                float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;                
                strictTimePassed += deltaTime;
                for (; strictTimePassed >= strictTimeAmount; strictTimePassed -= strictTimeAmount)
                {                    
                    foreach (var gameObject in gameObjects)
                        gameObject.StrictUpdate();
                    if (waitCount > 0)
                        waitCount--;
                }
            }

            // Finish recording the update time elapsed.
            //    {
            //        updateStopwatch.Stop();
            //        var updateTime = (float)updateStopwatch.Elapsed.TotalSeconds;
            //        updateTimes.Add(updateTime);
            //        if (updateTimes.Count == updateTimesPerUPS)
            //        {                                        
            //            Console.WriteLine($"Upate Times Average: {updateTimes.Average()}, Min: {updateTimes.Min()}, Max: {updateTimes.Max()}");
            //            updateTimes.Clear();
            //        }
            //        updateStopwatch.Reset();

            //        deltaTimes.Add((float)gameTime.ElapsedGameTime.TotalSeconds);
            //        if (deltaTimes.Count == deltaTimesPerUPS)
            //        {
            //            Console.WriteLine($"Current Delatimes Average: {deltaTimes.Average()}, Update Rate: {1 / deltaTimes.Average()}");
            //            deltaTimes.Clear();
            //        }
            //    }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // TODO: Add your drawing code here
            centerScreen.BeginCapture();
            GraphicsDevice.Clear(Color.Black);
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            levelTiledMapRenderer.Draw();
            foreach (var gameObject in gameObjects.OrderBy(x => x.Priority))
                gameObject.Draw(spriteBatch);            
            centerScreen.EndCapture();

            GraphicsDevice.Clear(Color.Black);
            centerScreen.Draw(spriteBatch);

            base.Draw(gameTime);
        }
    }
}
