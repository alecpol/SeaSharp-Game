using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameEnemyTest.Core;

namespace GameEnemyTest
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Player _player;
        //private Map _map;
        private GameLog _gameLog;
        private WaveManager _waveManager;
        private Texture2D _enemyTexture;
        
        private bool _isGameOver = false;
        private bool _gameStarted = false;

        private SpriteFont _font;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            //_map = new Map(GraphicsDevice);
            _graphics.PreferredBackBufferWidth = 1280; 
            _graphics.PreferredBackBufferHeight = 700;
            _graphics.ApplyChanges();
            
        }

        protected override void Initialize()
        {
            _player = new Player();
            //_map = new Map(GraphicsDevice);
            _gameLog = new GameLog();
            _waveManager = new WaveManager();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            //_map = new Map(GraphicsDevice);
            //_map.LoadContent(Content);

            _player.Texture = CreateColoredTexture(Color.Red);

            _enemyTexture = CreateColoredTexture(Color.Blue);

            _font = Content.Load<SpriteFont>("font");
            
            _waveManager = new WaveManager();
            _waveManager.StartNewWave(0);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboard = Keyboard.GetState();

            if (!_gameStarted)
            {
                if (keyboard.IsKeyDown(Keys.Enter))
                {
                    _gameStarted = true;
                }
                return;
            }

            if (_isGameOver)
                return;

            //_map.Update(gameTime);
            if (keyboard.IsKeyDown(Keys.Escape))
                Exit();

            _player.Update(gameTime, _waveManager.ActiveEnemies, _gameLog);
            _waveManager.Update(gameTime, _enemyTexture, _player);

            foreach (var enemy in _waveManager.ActiveEnemies)
            {
                enemy.Update(gameTime, _player);
                if (enemy.Bounds.Intersects(_player.Bounds))
                {
                    _isGameOver = true;
                    _gameLog.AddEntry("Game Over!");
                    break;
                }
            }

            for (int i = _waveManager.ActiveEnemies.Count - 1; i >= 0; i--)
            {
                if (_waveManager.ActiveEnemies[i].IsDead)
                {
                    _player.RegisterKill();
                    _waveManager.ActiveEnemies.RemoveAt(i);
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            
            //_spriteBatch.Begin();

            if (!_gameStarted)
            {
                _spriteBatch.Begin();
                _spriteBatch.DrawString(_font, "WASD to move", new Vector2(400, 250), Color.White);
                _spriteBatch.DrawString(_font, "SPACE bar to attack", new Vector2(400, 280), Color.White);
                _spriteBatch.DrawString(_font, "An enemy touch you ends the game", new Vector2(400, 310), Color.White);
                _spriteBatch.DrawString(_font, "Press ENTER to start", new Vector2(400, 360), Color.Yellow);
                _spriteBatch.End();
                return;
            }

            _spriteBatch.Begin();
            //_map.Draw(_spriteBatch);
            _player.Draw(_spriteBatch);

            foreach (var enemy in _waveManager.ActiveEnemies)
            {
                if (!enemy.IsDead)
                    enemy.Draw(_spriteBatch);
            }

            _gameLog.Draw(_spriteBatch, _font);

            if (_isGameOver)
            {
                _spriteBatch.DrawString(_font, "GAME OVER", new Vector2(500, 300), Color.Red, 0, Vector2.Zero, 2f, SpriteEffects.None, 0f);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private Texture2D CreateColoredTexture(Color color)
        {
            Texture2D texture = new Texture2D(GraphicsDevice, 32, 32); 
            Color[] data = new Color[32 * 32];
            for (int i = 0; i < data.Length; ++i) data[i] = color;
            texture.SetData(data);
            return texture;
        }
    }
}
