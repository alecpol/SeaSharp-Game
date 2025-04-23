using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameEnemyTest.Core
{
    public class WaveManager
    {
        public List<Enemy> ActiveEnemies { get; } = new List<Enemy>();
        public int EnemiesToSpawn { get; private set; }
        private float _spawnTimer;
        private int currentWave = 0;

        public void StartNewWave(int currentWave)
        {
            currentWave++;
            EnemiesToSpawn = 5 + currentWave * 2;
        }

        public void Update(GameTime gameTime, Texture2D enemyTexture, Player player)
        {

            if (EnemiesToSpawn > 0)
            {
                _spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_spawnTimer > 1.0f) // Spawn every 1 second
                {
                    ActiveEnemies.Add(new Enemy
                    {
                        Position = new Vector2(new Random().Next(0, 800), 0),
                        Texture = enemyTexture
                    });
                    EnemiesToSpawn--;
                    _spawnTimer = 0;
                }

            }
            else if (ActiveEnemies.Count == 0)
            {
                currentWave++;
                StartNewWave(currentWave);
            }

            /*foreach (var enemy in ActiveEnemies)
            {
                enemy.Update(gameTime, player);
            } */

            ActiveEnemies.RemoveAll(enemy => enemy.IsDead);
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var enemy in ActiveEnemies)
            {
                enemy.Draw(spriteBatch);
            }

        }
    }
}
