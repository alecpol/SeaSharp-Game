using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
namespace GameEnemyTest.Core
{
    public class Enemy
    {
        public Texture2D Texture { get; set; }
        public Vector2 Position { get; set; }
        private float _speed = 100f;
        public bool IsDead { get; set; }

        public Rectangle Bounds => new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            Texture.Width,
            Texture.Height
         );

        public void LoadContent(ContentManager content)
        {
            //_texture = content.Load<Texture2D>("enemy");

        }

        public void Update(GameTime gameTime, Player player)
        {
            if (IsDead) return;
            
            // Basic AI: Move toward player
            Vector2 direction = player.Position - Position;
            if (direction != Vector2.Zero)
                direction.Normalize();

            Position += direction * _speed * (float)gameTime.ElapsedGameTime.TotalSeconds;

        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsDead) return;
            spriteBatch.Draw(Texture, Position, Color.White);
        }
    }
}
