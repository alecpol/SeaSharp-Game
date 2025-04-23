using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GameEnemyTest.Core
{
    public class Map
    {
        private Texture2D _tileTexture;
        private int[,] _tiles;

        private GraphicsDevice _graphicsDevice;

        // Set the map dimensions to fit the screen size of 1280x700
        private const int TileSize = 64; // Size of each tile
        private const int MapWidth = 1280 / TileSize; // Number of tiles horizontally
        private const int MapHeight = 700 / TileSize; // Number of tiles vertically

        public int PixelWidth => MapWidth * TileSize;
        public int PixelHeight => MapHeight * TileSize;


        public void LoadContent(ContentManager content)
        {
            //_tileTexture = content.Load<Texture2D>("tile");
            _tileTexture = new Texture2D(_graphicsDevice, 1, 1);
            _tileTexture.SetData(new[] { Color.Gray });
            GenerateMap();
        }

        public Map(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
        }

        private void GenerateMap()
        {
            _tiles = new int[MapWidth, MapHeight]; // Create map based on the window size

            var random = new Random();
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    _tiles[x, y] = random.Next(0, 2); // 0 = empty, 1 = floor
                }
            }

        }

        public void Update(GameTime gameTime)
        {
            // Add map-specific logic here (e.g., traps, hazards)
            // For now, leave it empty if no updates are needed.
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    if (_tiles[x, y] == 1) // Only draw floor tiles
                    {
                        Vector2 position = new Vector2(x * TileSize, y * TileSize); // Tile positioning
                        spriteBatch.Draw(_tileTexture, position, Color.White);
                    }
                }
            }
        }
    }
}
