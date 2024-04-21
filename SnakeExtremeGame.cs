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
using Microsoft.Extensions.Primitives;
using Microsoft.Xna.Framework.Input.Touch;


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
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
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
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {
            if (State == States.Appear && ball.State == Ball.States.LightningNormal)
                State = States.Normal;

            if (State == States.Vanish && ball.State == Ball.States.Invisible)
                State = States.Gone;

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
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {
            if (State == States.Appear && ball.State == Ball.States.Normal)
                State = States.Normal;

            if (State == States.Vanish && ball.State == Ball.States.Invisible)
                State = States.Gone;

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
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
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
        private readonly Sound blankSound;
        public AnyKey(ContentManager content)
        {
            blankSound = new Sound(content, "sounds/move_0") { Volume = 0 };
        }
        public bool Selected { get; private set; } = false;
        public bool Pressed { get; private set; } = false;
        public bool Released { get; private set; } = false;
        public int Priority { get => 0; set => throw new NotImplementedException(); }
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {
            if (!Selected && (mouseState.LeftButton == ButtonState.Pressed ||
                mouseState.RightButton == ButtonState.Pressed ||
                keyboardState.GetPressedKeyCount() > 0))
            {
                Selected = true;
                Pressed = true;

                // This here is a hack. I have no idea why, but playing an empty sound, i.e. volume is 0,
                // causes the "The AudioContext was not allowed to start" issue fixed upon launch.
                // Need to research more as to why this works.
                blankSound.Play();
            }
            else
            {
                Pressed = false;
            }
            if (Selected && (mouseState.LeftButton != ButtonState.Pressed &&
                mouseState.RightButton != ButtonState.Pressed &&
                keyboardState.GetPressedKeyCount() == 0))
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
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
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
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
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
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {
            if (State == States.Appear && ball.State == Ball.States.Normal)            
                State = States.Normal;

            if (State == States.Vanish && ball.State == Ball.States.Invisible)
                State = States.Gone;

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
        private float shadowScale = 1.0f;
        private const float shadowAlpha = 0.5f;
        private float ballScale = 1.0f;
        private float ballAlpha = 1.0f;
        private float silhouetteAlpha = 0.0f;
        private float ballHeight = 0f;
        private const float ballMaxHeight = 32;
        private readonly List<LightningEffect> lightningEffects = new();
        private readonly static Color lightningSilhouetteColor = new Color(182, 229, 254);
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
            LightningAppear, LightningNormal, LightningVanish
        }
        public enum Modes { Normal, Lightning }
        public States State { get; private set; } = States.Normal;
        public Modes Mode { get; private set; } = Modes.Normal;
        public void LightningAppear()
        {
            State = States.LightningAppear;
            waitCount = 7;
            waitTotal = 8;
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
            waitCount = 7;
            waitTotal = 8;
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
        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {
            // Clear out any lightning effects that completed.
            lightningEffects.RemoveAll(x => x.State == LightningEffect.States.Gone);

            // Perform all other game object updates.
            ballSprite.Update(gameTime);
            shadowSprite.Update(gameTime);
            foreach (var lightningEffect in lightningEffects)
                lightningEffect.Update(gameTime, mouseState, keyboardState);
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

            if (State == States.LongVanish || State == States.LightningVanish)
                ballAlpha = MathHelper.Lerp(0, 1, waitRatio);
            else if (State == States.LongAppear || State == States.LightningAppear)
                ballAlpha = MathHelper.Lerp(1, 0, waitRatio);
            else 
                ballAlpha = 1f;

            if (State == States.LongVanish)
                silhouetteAlpha = MathHelper.Lerp(1, 0, waitHighRatio);
            else if (State == States.LongAppear)
                silhouetteAlpha = MathHelper.Lerp(0, 1, waitLowRatio);
            else if (State == States.LightningAppear || State == States.LightningVanish)
                silhouetteAlpha = waitCount % 2;
            else if (State == States.LightningNormal)
            {
                int waitHalf = waitTotal / 2;
                int waitDiff = waitCount - waitHalf;
                if (waitDiff >= waitHalf)                
                    silhouetteAlpha = MathHelper.Lerp(1, 0, (float)waitDiff / waitHalf);                
                else                
                    silhouetteAlpha = MathHelper.Lerp(0, 1, (float)waitCount / waitHalf);                
            }
            else
                silhouetteAlpha = 0f;

            if (State == States.LightningAppear || State == States.LightningNormal || State == States.LightningVanish)
                silhouetteColor = lightningSilhouetteColor;
            else
                silhouetteColor = Color.Black;

            if (State == States.LongVanish)
                ballHeight = MathHelper.Lerp(ballMaxHeight, 0, waitRatio);
            else if (State == States.LongAppear)
                ballHeight = MathHelper.Lerp(0, ballMaxHeight, waitRatio);
            else
                ballHeight = 0;

            if ((State == States.LightningAppear || State == States.LightningNormal || State == States.LightningVanish) &&
                waitCount % 15 == 0)
            {
                var lightningEffect = new LightningEffect(content)
                {
                    Position = ballDrawPosition + new Vector2(
                        (random.NextSingle() - 0.5f) * Size.Width,
                        (random.NextSingle() - 0.5f) * Size.Height),
                    Scale = 0.5f
                };
                lightningEffect.Vanish();
                lightningEffects.Add(lightningEffect);
            }

            // Update Counters
            if (waitCount > 0)
                waitCount--;
            else if (State == States.LightningNormal)
                waitCount = waitTotal - 1;

            // Update FSM states.
            if ((State == States.QuickVanish || State == States.LongVanish || State == States.LightningVanish) && waitCount == 0)
                State = States.Invisible;

            if ((State == States.QuickAppear || State == States.LongAppear) && waitCount == 0)
                State = States.Normal;

            if (State == States.LightningAppear && waitCount == 0)
                State = States.LightningNormal;

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
        private Panel currentScorePanel, highScorePanel;
        private Dimmer dimmer;
        private Board board;
        private AnyKey anyKey;
        private Sound moveSound, foodSound, destroySound, pauseSound, obstacleSound;
        private Snake.Directions newDirection;
        private float strictTimePassed;
        private const float strictTimeAmount = (float)1 / 30;
        private const int minWait = 3;
        private const int maxWait = 10;
        private const int scorePerLevelUpdate = 5;
        private const int obstacleStartThreshold = 1 * scorePerLevelUpdate;
        private const int obstaclesPerLevelUpdate = 2;
        private const int lightningStartThreshold = 2 * scorePerLevelUpdate;
        private const int lightningPerLevelUpdate = 2;
        private const int lightningActiveTurns = 5;
        private int waitCount;
        private int waitTotal;
        private bool gameDestroy = false;
        private List<Point> levelCorners = new();
        private List<Point> levelPossiblePositions = new();     
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
                if (obj.turnWait == 0)
                {
                    if (obj.obstacle.State == LightningObstacle.States.Normal)
                    {
                        obj.obstacle.Vanish();
                        obj.turnWait = lightningActiveTurns;
                    }
                    else if (obj.obstacle.State == LightningObstacle.States.Gone &&
                             snake.Bodies.All(x => x.LevelPosition != obj.obstacle.LevelPosition || x == snake.Tail))
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
        public SnakeExtremeGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }
        public enum FoodStates { Normal, NewFood }
        public enum GameStates { Start, Create, Wait, Action, Destroy }
        public enum PauseStates { Resumed, Pause, Paused, Resume }
        public GameStates GameState { get; private set; } = GameStates.Start;
        public FoodStates FoodState { get; private set; } = FoodStates.Normal;
        public PauseStates PauseState { get; private set; } = PauseStates.Resumed;

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

            levelTiledMap = Content.Load<TiledMap>("tiled_project/level_0");
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

            anyKey = new AnyKey(Content);
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
                       "Joel Francis Burford - Sound Effects - joelfrancisburford.itch.io <n> "
            };
            board.Position = new Vector2(
                (levelTiledMap.WidthInPixels - board.Size.Width) / 2,
                (levelTiledMap.HeightInPixels - board.Size.Height) / 2);
            gameObjects.Add(board);

            moveSound = new Sound(Content, "sounds/move_0");
            foodSound = new Sound(Content, "sounds/food_0");
            destroySound = new Sound(Content, "sounds/destroy_0");
            pauseSound = new Sound(Content, "sounds/pause_0");
            obstacleSound = new Sound(Content, "sounds/obstacle_0");

            snake = new Snake(Content);
            gameObjects.Add(snake);

            dimmer.Dim();
            board.Open();
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
            MouseState mouseState = Mouse.GetState();
            KeyboardState keyboardState = Keyboard.GetState();

            // TODO: Add your update logic here

            // Service buttons
            if (upButton.Pressed)
                newDirection = Snake.Directions.Up;
            if (downButton.Pressed)
                newDirection = Snake.Directions.Down;
            if (leftButton.Pressed)
                newDirection = Snake.Directions.Left;
            if (rightButton.Pressed)
                newDirection = Snake.Directions.Right;

            if (startButton.Pressed)
            {
                if (PauseState == PauseStates.Resumed)
                {
                    if (GameState == GameStates.Start)
                    {
                        Debug.Assert(snake.Headless);
                        Debug.Assert(food == null);

                        var startLevelPosition = new Point(
                            (int)levelCorners.Select(x => x.X).Average(),
                            (int)levelCorners.Select(x => x.Y).Average());
                        snake.CreateHead(startLevelPosition);
                        snake.Direction = Snake.Directions.Up;
                        newDirection = Snake.Directions.Up;
                        gameObjects.Add(snake.Head);

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
                    PauseState = PauseStates.Resume;                    
                }
            }

            // Service state changes.
            if (PauseState == PauseStates.Pause && dimmer.State == Dimmer.States.Dimmed && board.State == Board.States.Opened)
                PauseState = PauseStates.Paused;

            if (PauseState == PauseStates.Resume && dimmer.State == Dimmer.States.None && board.State == Board.States.Closed)
                PauseState = PauseStates.Resumed;

            if (PauseState == PauseStates.Resumed && GameState == GameStates.Create && snake.State == Snake.States.Normal && food.State == Food.States.Normal)
            {
                waitTotal = maxWait;
                waitCount = waitTotal - 1;
                GameState = GameStates.Wait;
            }

            if (PauseState == PauseStates.Resumed && GameState == GameStates.Wait && waitCount == 0)
            {
                snake.Direction = newDirection;
                var snakeNextLevelPosition = snake.LevelPosition + Snake.DirectionPoints[snake.Direction];

                var minX = levelCorners.Select(x => x.X).Min();
                var maxX = levelCorners.Select(x => x.X).Max();
                var minY = levelCorners.Select(x => x.Y).Min();
                var maxY = levelCorners.Select(x => x.Y).Max();                

                if (snakeNextLevelPosition.X < minX || snakeNextLevelPosition.X > maxX ||
                    snakeNextLevelPosition.Y < minY || snakeNextLevelPosition.Y > maxY ||
                    snake.Bodies.Any(x=> x.LevelPosition == snakeNextLevelPosition && x != snake.Tail) ||
                    obstacles.Any(x => x.LevelPosition == snakeNextLevelPosition) ||  
                    lightningObjects.Any(x => (x.obstacle.State == LightningObstacle.States.Normal && x.obstacle.LevelPosition == snakeNextLevelPosition && x.turnWait != 0) ||
                                              (x.obstacle.State == LightningObstacle.States.Gone && x.obstacle.LevelPosition == snakeNextLevelPosition && x.turnWait == 0)) ||
                    gameDestroy)
                {
                    gameDestroy = false;

                    snake.Vanish();
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
                else if (snakeNextLevelPosition == food.LevelPosition)
                {                    
                    // Move / grow the snake.
                    snake.Move(growTail: (int)(levelPossiblePositions.Count * 0.40f) >= snake.Count);
                    moveSound.Play(randomPitch: true);

                    // Generate food.
                    Debug.Assert(newFood == null);
                    food.Vanish();
                    newFood = new Food(Content) { LevelPosition = getRandomLevelPosition([snakeNextLevelPosition]) };
                    gameObjects.Add(newFood);
                    foodSound.Play();

                    // Update score.
                    currentScorePanel.Value += 1;
                    currentScorePanel.Flash();

                    // Time to generate some hazards.
                    if (currentScorePanel.Value % scorePerLevelUpdate == 0)
                    {
                        // Generate obstacle.
                        if (currentScorePanel.Value >= obstacleStartThreshold &&                            
                            (int)(levelPossiblePositions.Count * 0.25f) >= obstacles.Count)
                        {
                            for (int i = 0; i < obstaclesPerLevelUpdate; i++)
                            {
                                var obstacle = new Obstacle(Content) { LevelPosition = getRandomLevelPosition([snakeNextLevelPosition]) };
                                obstacles.Add(obstacle);
                                gameObjects.Add(obstacle);
                            }                            
                        }

                        // Generate lightning obstacle.
                        if (currentScorePanel.Value >= lightningStartThreshold &&
                            (int)(levelPossiblePositions.Count * 0.25f) >= obstacles.Count)
                        {
                            for (int i = 0; i < lightningPerLevelUpdate; i++)
                            {
                                var obj = new LightningObject()
                                {
                                    obstacle = new LightningObstacle(Content) { LevelPosition = getRandomLevelPosition([snakeNextLevelPosition]) },
                                    turnWait = lightningActiveTurns
                                };
                                lightningObjects.Add(obj);
                                gameObjects.Add(obj.obstacle);
                            }
                        }

                        obstacleSound.Play();
                    }

                    updateLightningObjects();

                    // Update state
                    FoodState = FoodStates.NewFood;
                    GameState = GameStates.Action;
                }
                else
                {                    
                    snake.Move(growTail: false);

                    moveSound.Play(randomPitch: true);

                    updateLightningObjects();

                    FoodState = FoodStates.Normal;
                    GameState = GameStates.Action;                    
                }                 
            }

            if (PauseState == PauseStates.Resumed && GameState == GameStates.Action && 
                snake.State == Snake.States.Normal && 
                obstacles.All(x => x.State == Obstacle.States.Normal) &&
                lightningObjects.All(x => x.obstacle.State == LightningObstacle.States.Normal || x.obstacle.State == LightningObstacle.States.Gone))
            {
                if (FoodState == FoodStates.NewFood && food.State == Food.States.Gone && newFood.State == Food.States.Normal)
                {
                    gameObjects.Remove(food);
                    food = newFood;
                    newFood = null;

                    waitTotal = (int)MathHelper.Lerp(maxWait, minWait, 
                        (float)currentScorePanel.Value / ((scorePerLevelUpdate * (maxWait - minWait))));                    

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
                foreach (var body in snake.Bodies)
                    gameObjects.Remove(body);
                snake.Clear();

                gameObjects.Remove(food);
                food = null;

                foreach (var obstacle in obstacles)
                    gameObjects.Remove(obstacle);
                obstacles.Clear();

                foreach (var obj in lightningObjects)
                    gameObjects.Remove(obj.obstacle);
                lightningObjects.Clear();

                GameState = GameStates.Start;
            }

            if (snake.NewTailAvailable)
                gameObjects.Add(snake.Tail);

            // Perform level and game object updates.
            {
                levelTiledMapRenderer.Update(gameTime);
                foreach (var gameObject in gameObjects)
                    gameObject.Update(gameTime, mouseState, keyboardState);
            }

            // Perform strict time updates.
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
