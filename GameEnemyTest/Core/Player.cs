//File: Player.cs
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameEnemyTest.Core
{
    public class Player
    {
        public Vector2 Position { get; private set; }
        public Texture2D Texture { get; set; }
        private float _speed = 200f;

        private int _killCount;
        private float _baseSpeed = 200f;

        public Rectangle AttackBounds { get; private set; }
        public bool ExplosiveBulletsActive { get; private set; }
        public float AttackSpeed { get; private set; } = 1.0f;
        private float _attackTimer = 0f;
        private float _attackDuration = 0.2f; // Attack lasts for 0.2 seconds
        private bool _isAttacking = false;

        public bool IsDashing { get; private set; }
        private float _dashTimer;
        private float _dashSpeed = 800f;

        public int KillCount => _killCount;

        public Rectangle Bounds => new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            Texture.Width,
            Texture.Height
        );

        public void LoadContent(ContentManager content)
        {
            // Load player texture (ensure "player" is added to Content/Textures)
            //_texture = content.Load<Texture2D>("player");

        }

        public void RegisterKill()
        {
            _killCount++;

            // Killstreak bonuses
            if (_killCount >= 40)
                ExplosiveBulletsActive = true;
            else if (_killCount >= 25)
                AttackSpeed = 0.3f;
            else if (_killCount >= 10)
                _speed = _baseSpeed * 1.5f;
        }

        public void ResetOnDeath()
        {
            _killCount = 0;
            _speed = _baseSpeed;
        }

        public void Dash(Vector2 direction)
        {
            if (!IsDashing)
            {
                IsDashing = true;
                _dashTimer = 0.2f; // Dash duration (0.2 seconds)
                Position += direction * _dashSpeed * _dashTimer;
            }
        }

        public Rectangle GetSuperMeleeBounds()
        {
            // Larger hitbox when dashing
            return IsDashing
                ? new Rectangle((int)Position.X - 50, (int)Position.Y - 50, Texture.Width + 100, Texture.Height + 100)
                : new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height);
        }


        public void Update(GameTime gameTime, List<Enemy> enemies, GameLog gameLog)
        {
            //var keyboard = Keyboard.GetState();
            KeyboardState keyboard = Keyboard.GetState();
            Vector2 direction = Vector2.Zero;

            if (keyboard.IsKeyDown(Keys.W)) direction.Y -= 1;
            if (keyboard.IsKeyDown(Keys.S)) direction.Y += 1;
            if (keyboard.IsKeyDown(Keys.A)) direction.X -= 1;
            if (keyboard.IsKeyDown(Keys.D)) direction.X += 1;
            
            // Normalize diagonal movement
            if (direction != Vector2.Zero)
                direction.Normalize();

            Vector2 newPosition = Position + direction * _speed * (float)gameTime.ElapsedGameTime.TotalSeconds;

            newPosition.X = MathHelper.Clamp(newPosition.X, 100, 1280 - Texture.Width);
            newPosition.Y = MathHelper.Clamp(newPosition.Y, 100, 700 - Texture.Height);

            Position = newPosition;

            if (keyboard.IsKeyDown(Keys.Space) && !_isAttacking)
            {
                Attack();
            }

            if (_isAttacking)
            {
                _attackTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_attackTimer >= _attackDuration)
                {
                    _isAttacking = false;
                    _attackTimer = 0f;
                    AttackBounds = Rectangle.Empty;
                }
            }

            foreach (var enemy in enemies)
            {
                if (_isAttacking && AttackBounds.Intersects(enemy.Bounds))
                {
                    enemy.IsDead = true;
                    gameLog?.AddEntry($"Enemy hit at {enemy.Position}");
                }
            }


            if (IsDashing)
            {
                _dashTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_dashTimer <= 0) IsDashing = false;
            }
        }
        private void Attack()
        {
            _isAttacking = true;
            AttackBounds = new Rectangle((int)Position.X + Texture.Width, (int)Position.Y + Texture.Height / 4, 50, Texture.Height / 2);
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position, Color.White);

            if (_isAttacking)
            {
                Texture2D attackTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                attackTexture.SetData(new[] { Color.Orange });

                spriteBatch.Draw(attackTexture, AttackBounds, Color.Orange * 0.8f); 
            }
        }

    }
}
