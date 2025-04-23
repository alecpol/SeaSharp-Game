using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace GameEnemyTest.Core
{
    public class GameLog
    {
        private List<string> _logEntries = new List<string>();

        public void AddEntry(string message)
        {
            _logEntries.Add(message);
            if (_logEntries.Count > 10) _logEntries.RemoveAt(0); // Keep only 10 entries
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            Vector2 position = new Vector2(10, 10);
            foreach (var entry in _logEntries)
            {
                spriteBatch.DrawString(font, entry, position, Color.White);
                position.Y += 20; // Move down for next entry
            }
        }
    }
}
