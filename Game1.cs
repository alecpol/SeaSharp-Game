using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Tutorial012.Controls;
using System.ComponentModel;
using ytgame;
using System.Diagnostics;
using System.Threading;



namespace RougeLike
{
    static class Extensions
    {
        public static float ToAngle(this Vector2 vector)
        {
            return (float)Math.Atan2(vector.Y, vector.X);
        }
        
        public static Vector2 ScaleTo(this Vector2 vector, float length)
        {
            return vector * (length / vector.Length());
        }

        public static Point ToPoint(this Vector2 vector)
        {
            return new Point((int)vector.X, (int)vector.Y);
        }

        public static float NextFloat(this Random rand, float minValue, float maxValue)
        {
            return (float)rand.NextDouble() * (maxValue - minValue) + minValue;
        }

        public static Vector2 NextVector2(this Random rand, float minLength, float maxLength)
        {
            double theta = rand.NextDouble() * 2 * Math.PI;
            float length = rand.NextFloat(minLength, maxLength);
            return new Vector2(length * (float)Math.Cos(theta), length * (float)Math.Sin(theta));
        }
    }
    public class GameRoot : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch spriteBatch;

        public static GameRoot Instance { get; private set; }
        public static Viewport Viewport { get { return Instance.GraphicsDevice.Viewport; } }
        public static Vector2 ScreenSize { get { return new Vector2(Viewport.Width, Viewport.Height); } }
        public static GameTime GameTime { get; private set; }

        enum PowerupType
    {
        Diamond,
        Rapid,
        Speed,
        Missile,
        Nuke,
        ExtraLife,      
        Invincibility,  
        ForceField, 
        Null
    }

        enum GameStates
        {
            Menu,
            Game,
            Paused,
            GameOver,
        }

        GameStates gameState;

        abstract class Entity
        {
            protected Texture2D image;
            protected Color color = Color.White;

            public Vector2 Position, Velocity;
            public float Orientation;
            public float Radius = 20;
            public bool IsExpired;

            

            public Vector2 Size
            {
                get
                {
                    return image == null ? Vector2.Zero : new Vector2(image.Width, image.Height);
                }
            }

            public abstract void Update();

            public virtual void Draw(SpriteBatch spriteBatch)
            {
                
                spriteBatch.Draw(image, Position, null, color, Orientation, Size / 2f, 1f, 0, 0);
            }
        }

        static class EntityManager
        {
            static List<Entity> entities = new List<Entity>();

            static bool isUpdating;
            static List<Entity> addedEntities = new List<Entity>();

            public static List<Enemy> enemies = new List<Enemy>();
            static List<Bullet> bullets = new List<Bullet>();
            static List<Missile> missiles = new List<Missile>();
            public static List<Powerup> powerups = new List<Powerup>();
            static List<Explosion> explosions = new List<Explosion>();
            static List<Boss> bosses = new List<Boss>();
            static List<BossBullet> bossBullets = new List<BossBullet>();

            private static void AddEntity(Entity entity)
            {
                entities.Add(entity);
                if (entity is Bullet)
                {
                    bullets.Add(entity as Bullet);
                }
                else if (entity is Enemy)
                {
                    enemies.Add(entity as Enemy);
                }
                else if (entity is Missile)
                {
                    missiles.Add(entity as Missile);
                }
                else if (entity is Powerup)
                {
                    powerups.Add(entity as Powerup);
                }
                else if(entity is Explosion)
                {
                    explosions.Add(entity as Explosion);
                }
                else if(entity is Boss)
                {
                    bosses.Add(entity as Boss);
                }
                else if(entity is BossBullet)
                {
                    bossBullets.Add(entity as BossBullet);
                }
            }

            public static int Count { get { return entities.Count;} }

            public static void Add(Entity entity)
            {
                if (!isUpdating)
                {
                    AddEntity(entity);
                }
                else
                {
                    addedEntities.Add(entity);
                }
            }

            public static void Update()
            {
                isUpdating = true;
                HandleCollisions();


                foreach (var entity in entities)
                        entity.Update();

                isUpdating = false;

                foreach (var entity in addedEntities)
                        AddEntity(entity);

                addedEntities.Clear();

                entities = entities.Where(x => !x.IsExpired).ToList();

                bullets = bullets.Where(x => !x.IsExpired).ToList();
                missiles = missiles.Where(x => !x.IsExpired).ToList();
                enemies = enemies.Where(x => !x.IsExpired).ToList();
                powerups = powerups.Where(x => !x.IsExpired).ToList();

                
            }

            public static void Draw(SpriteBatch spriteBatch)
            {
                foreach (var entity in entities)
                    entity.Draw(spriteBatch);
            }

            private static bool IsColliding(Entity a, Entity b)
            {
                float radius = a.Radius + b.Radius;
                return !a.IsExpired && !b.IsExpired && Vector2.DistanceSquared(a.Position, b.Position) < radius * radius;
            }

            static void HandleCollisions()
            {
                // handle collisions between enemies
                for (int i = 0; i < enemies.Count; i++)
                {
                    for(int j = i + 1; j < enemies.Count; j++)
                    {
                        if (IsColliding(enemies[i], enemies[j]))
                        {
                            enemies[i].HandleCollision(enemies[j]);
                            enemies[j].HandleCollision(enemies[i]);
                        }
                    }
                }

                // handle collisions between bullets and enemies
                for (int i = 0; i < enemies.Count; i++)
                {
                    for (int j = 0; j < bullets.Count; j++)
                    {
                        if (IsColliding(enemies[i], bullets[j]))
                        {
                            enemies[i].WasShot();
                            bullets[j].IsExpired = true;
                            if (PlayerStatus.ExplosiveBullets)
                            {
                                EntityManager.Add(new Explosion(enemies[i].Position, 1f));
                            }
                            
                        }
                    }
                }

                // handle collisions between missiles and enemies (literally just a bullet that doesn't disappear lol)
                for (int i = 0; i < enemies.Count; i++)
                {
                    for (int j = 0; j < missiles.Count; j++)
                    {
                        if (IsColliding(enemies[i], missiles[j]))
                        {
                            enemies[i].WasShot();
                        }
                    }


                }


                // handle collisions between the player and enemies
                for (int i = 0;i < enemies.Count; i++)
                {
                    if (enemies[i].IsActive && IsColliding(Playership.Instance, enemies[i]) && !enemies[i].IsDown && !Playership.Instance.Invincibility)
                    {
                        if (Playership.Instance.ForceFieldActive)
                        {
                        // Shield destroys the enemy on contact
                        enemies[i].WasShot(); // kill enemy
                        }
                        if (!enemies[i].IsGrapple)
                        {
                            Playership.Instance.Kill();
                            
                            for (int j = 0; j < enemies.Count; j++)
                            {
                                if (enemies[i].HookEntity != null)
                                {
                                    enemies[i].HookEntity.UnhookEnemy();
                                }
                            }
                            
                            break;
                        }
                        else
                        {
                            enemies[i].HookEntity.UnhookEnemy();
                            enemies[i].WasUngrappled();
                            
                        }
                    }
                    
                    
                }

                //handle collisions between the player and powerups
                for (int i = 0; i < powerups.Count; i++)
                {
                    if(IsColliding(Playership.Instance, powerups[i]))
                    {
                        switch (powerups[i].type)
                        {
                            case PowerupType.Diamond:
                                Playership.Instance.powerup = PowerupType.Diamond;
                                Playership.Instance.powerupTimeRemaining = 60 * 8;
                                break;

                            case PowerupType.Missile:
                                Playership.Instance.powerup = PowerupType.Missile;
                                Playership.Instance.powerupTimeRemaining = 60 * 8;
                                break;

                            case PowerupType.Rapid:
                                Playership.Instance.powerup = PowerupType.Rapid;
                                Playership.Instance.powerupTimeRemaining = 60 * 8;
                                break;

                            case PowerupType.Speed:
                                Playership.Instance.speedRemaining = 60 * 60;
                                break;

                            case PowerupType.Nuke:
                                for (int j = 0; j < enemies.Count; j++)
                                {
                                    enemies[j].WasShot();
                                }
                                break;

                            case PowerupType.ExtraLife:
                                PlayerStatus.AddLife();
                                break;

                            case PowerupType.Invincibility:
                                Playership.Instance.ActivateInvincibility(60 * 5);
                                break;

                            case PowerupType.ForceField:
                                Playership.Instance.ActivateForceField(60 * 8);
                                break;

                        }
                        powerups[i].IsExpired = true;
                    }
                }

                //Explosions and enemies
                for (int i = 0; i < enemies.Count; i++)
                {
                    for(int j = 0; j < explosions.Count; j++)
                    {
                        if (IsColliding(enemies[i], explosions[j]))
                        {
                            enemies[i].WasShot();
                        }
                    }
                }

                //Explosions and bosses
                for(int i = 0; i < bosses.Count; i++)
                {
                    for(int j = 0; j < explosions.Count; j++)
                    {
                        if (IsColliding(bosses[i], explosions[j]) && !explosions[j].isVisual && bosses[i].IsActive)
                        {
                            bosses[i].TakeDamage(5);
                            explosions[j].isVisual = true;
                        }
                    }
                }

                //Player bullets and bosses
                for(int i = 0; i < bosses.Count; i++)
                {
                    for (int j = 0; j < bullets.Count; j++)
                    {
                        if (IsColliding(bosses[i], bullets[j]) && bosses[i].IsActive)
                        {
                            bosses[i].TakeDamage(5);
                            if (PlayerStatus.ExplosiveBullets)
                            {
                                EntityManager.Add(new Explosion(bullets[j].Position, 1f));
                            }
                            bullets[j].IsExpired = true;

                        }
                    }
                }

                //Boss bullets and player
                for (int i = 0; i < bossBullets.Count; i++)
                {
                    if (Playership.Instance.ForceFieldActive)
                    {
                    // Bullet is absorbed by the force field
                    bossBullets[i].IsExpired = true;
                    }
                    if (IsColliding(bossBullets[i], Playership.Instance) && !Playership.Instance.Invincibility)
                    {
                        Playership.Instance.Kill();
                    }
                }

                //Boss and player
                for (int i = 0; i < bosses.Count; i++)
                {
                    if (IsColliding(bosses[i], Playership.Instance) && !bosses[i].isDead && !Playership.Instance.Invincibility && bosses[i].IsActive)
                    {
                        Playership.Instance.Kill();
                    }
                }

                //Boss and missiles
                for (int i = 0; i < bosses.Count; i++)
                {
                    for (int j = 0; j < missiles.Count; j++)
                    {
                        if (IsColliding(bosses[i], missiles[j]) && bosses[i].IsActive)
                        {
                            bosses[i].TakeDamage(10);
                            missiles[j].IsExpired = true;
                        }
                    }


                }
            }

            
            public static void HandleMelee(Melee entity)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (IsColliding(enemies[i], entity))
                    {
                        if (enemies[i].IsGrapple || entity.Super)
                        {
                            
                            enemies[i].SuperHit(entity.aimAngle);
                            entity.IsExpired = true;
                            
                        }
                        else
                        {
                            enemies[i].WasShot();
                            entity.IsExpired = true;
                        }
                        
                    }
                }

                //Boss and melee
                for(int i = 0; i < bosses.Count; i++)
                {
                    if (IsColliding(bosses[i], entity) && bosses[i].IsActive){
                        bosses[i].TakeDamage(10);
                        entity.IsExpired = true;
                    }
                }
            }

            public static void HandleHook(Hook hook)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (IsColliding(enemies[i], hook))
                    {
                        if (!hook.hooked && !hook.reeling)
                        {
                            hook.HookEnemy(enemies[i]);
                        }
                    }
                }
            }

            public static void DestroyPowerups()
            {
                for (int i = 0; i < powerups.Count; i++)
                {
                    powerups[i].IsExpired = true;
                }
            }

            public static void DestroyAllEnemies()
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    EntityManager.Add(new Explosion(enemies[i].Position, 1f));
                    enemies[i].IsExpired = true;
                    
                }
            }
        }

        static class Art
        {
            public static Texture2D Player { get; private set; }
            public static Texture2D Seeker { get; private set; }
            public static Texture2D Wanderer { get; private set; }
            public static Texture2D Bullet { get; private set; }
            public static Texture2D Pointer { get; private set; }
            public static SpriteFont Font { get; private set; }
            public static Texture2D Melee { get; private set; }
            public static Texture2D Hook { get; private set; }
            public static Texture2D HookLine { get; private set; }
            public static Texture2D Missile { get; private set; } 
            public static Texture2D Speed { get; private set; }
            public static Texture2D Nuke { get; private set; }
            public static Texture2D RapidFire { get; private set; }
            public static Texture2D Diamond { get; private set; }
            public static Texture2D MissileBullet { get; private set; }
            public static Texture2D MeleeAnimated { get; private set; }

            public static Texture2D Explosion { get; private set; }
            public static Texture2D Button { get; private set; }
            public static SpriteFont WaveFont { get; private set; }
            public static Texture2D Boss { get; private set; }
            public static Texture2D BossBullet { get; private set; }
            public static Texture2D ExtraLife { get; private set; }
            public static Texture2D Invincibility { get; private set; }
            public static Texture2D ForceField { get; private set; }

            public static void Load(ContentManager content)
            {
                Player = content.Load<Texture2D>("Player");
                Seeker = content.Load<Texture2D>("Seeker");
                Wanderer = content.Load<Texture2D>("Wanderer");
                Bullet = content.Load<Texture2D>("Bullet");
                Pointer = content.Load<Texture2D>("Pointer");
                Font = content.Load<SpriteFont>("Font");
                Melee = content.Load<Texture2D>("Melee");
                Hook = content.Load<Texture2D>("hook");
                HookLine = content.Load<Texture2D>("hookLine");
                Missile = content.Load<Texture2D>("missile");
                Speed = content.Load<Texture2D>("speed");
                Nuke = content.Load<Texture2D>("nuke");
                RapidFire = content.Load<Texture2D>("rapidFire");
                Diamond = content.Load<Texture2D>("diamond");
                MissileBullet = content.Load<Texture2D>("missileBullet");
                MeleeAnimated = content.Load<Texture2D>("meleeAnimated");
                Explosion = content.Load<Texture2D>("explosion");
                Button = content.Load<Texture2D>("Controls/button2");
                WaveFont = content.Load<SpriteFont>("waveFont");
                Boss = content.Load<Texture2D>("boss");
                BossBullet = content.Load<Texture2D>("bossBullet");
                ExtraLife = content.Load<Texture2D>("extraLife");
                Invincibility = content.Load<Texture2D>("invincibility");
                ForceField = content.Load<Texture2D>("forceField");

                
            }

           
        }

        static class MathUtil
        {
            public static Vector2 FromPolar(float angle, float magnitude)
            {
                return magnitude * new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            }
        }

        class Playership : Entity
        {

            const int cooldownFrames = 30;
            const int dashCooldown = 60;
            int dashCooldownRemaining = 0;
            int dashDuration = 0;
            int cooldownRemaining = 0;

            const int meleeCooldown = 30;
            int meleeCooldownRemaining = 0;

            const int hookCooldown = 60;
            int hookCooldownRemaining = 0;

            public int powerupTimeRemaining { get; set; }

            public PowerupType powerup = PowerupType.Null;

            public int speedRemaining {  get; set; }

            public float speed = 4;
            
            static Random rand = new Random();

            int framesUntilRespawn = 0;
            public bool IsDead { get { return framesUntilRespawn > 0; } }

            public bool Invincibility { get; set; }
            const int invincibilityDuration = 60;
            int invincibilityRemaining = 0;
            int forceFieldRemaining = 0;
            public bool ForceFieldActive { get; set; }

            public void ActivateInvincibility(int durationFrames)
            {
                Invincibility = true;
                invincibilityRemaining = durationFrames;
            
            }
            public void ActivateForceField(int durationFrames)
            {
                ForceFieldActive = true;
                forceFieldRemaining = durationFrames;
            }
            private static Playership instance;
            public static Playership Instance
            {
                get
                {
                    if (instance == null)
                        instance = new Playership();
                    return instance;
                }
            }

            private Playership()
            {
                image = Art.Player;
                Position = GameRoot.ScreenSize / 2;
                Radius = 10;
            }

            public override void Update()
            {
                if (IsDead)
                {
                    framesUntilRespawn--;
                    return;
                }

                if (PlayerStatus.Speed1)
                {
                    Velocity = (speed * 1.25f) * Input.GetMovementDirection();
                }
                else
                {
                    Velocity = speed * Input.GetMovementDirection();
                }
                

                Position += Velocity;
                Vector2 posMin;
                posMin.X = 20;
                posMin.Y = 260;

                Vector2 posMax;
                posMax.X = ScreenSize.X - 20;
                posMax.Y = ScreenSize.Y - 60;
                Position = Vector2.Clamp(Position, posMin, posMax);

                if (Velocity.LengthSquared() > 0)
                {
                    Orientation = Velocity.ToAngle();
                }

                var aim = Input.GetAimDirection();
                if (aim.LengthSquared() > 0 && cooldownRemaining <= 0 && Mouse.GetState().LeftButton == ButtonState.Pressed)
                {

                    if (powerup == PowerupType.Diamond)
                    {
                        cooldownRemaining = 15;
                        float aimAngleDiamond = aim.ToAngle();

                        Vector2 vel1 = MathUtil.FromPolar(aimAngleDiamond - .5f, 11f);
                        Vector2 vel2 = MathUtil.FromPolar(aimAngleDiamond, 11f);
                        Vector2 vel3 = MathUtil.FromPolar(aimAngleDiamond + .5f, 11f);

                        EntityManager.Add(new Bullet(Position, vel1));
                        EntityManager.Add(new Bullet(Position, vel2));
                        EntityManager.Add(new Bullet(Position, vel3));
                    }
                    else if (powerup == PowerupType.Missile)
                    {
                        cooldownRemaining = 15;
                        float aimAngle = aim.ToAngle();
                        Vector2 vel = MathUtil.FromPolar(aimAngle, 11f);
                        EntityManager.Add(new Missile(Position, vel));
                    }
                    else if (powerup == PowerupType.Rapid)
                    {
                        cooldownRemaining = 3;
                        float aimAngle = aim.ToAngle();
                        Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0, 0, aimAngle);
                        Vector2 vel = MathUtil.FromPolar(aimAngle, 11f);
                        EntityManager.Add(new Bullet(Position, vel));
                    }
                    else
                    {
                        if (PlayerStatus.FiringSpeed1)
                        {
                            cooldownRemaining = cooldownFrames - 10;
                        }
                        else
                        {
                            cooldownRemaining = cooldownFrames;
                        }
                        
                        float aimAngle = aim.ToAngle();
                        Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0, 0, aimAngle);

                        //We don't want random spread for default gun, but maybe later
                        //float randomSpread = rand.NextFloat(-0.04f, 0.04f) + rand.NextFloat(-0.04f, 0.04f);

                        Vector2 vel = MathUtil.FromPolar(aimAngle, 11f);

                        //Vector2 offset = Vector2.Transform(new Vector2(25, -8), aimQuat);

                        EntityManager.Add(new Bullet(Position, vel));

                        /*
                         * This code is to add two bullets, but we don't want that for default gun
                        offset = Vector2.Transform(new Vector2(25, 8), aimQuat);
                        EntityManager.Add(new Bullet(Position + offset, vel));
                        */

                    }

                }

                

                if (Input.WasDashButtonPressed() && dashCooldownRemaining <= 0)
                {
                    speed += 8f;
                    dashDuration = 10;
                    dashCooldownRemaining = dashCooldown;
                }

                if (Input.WasMeleeButtonPressed() && meleeCooldownRemaining <= 0)
                {
                    float aimAngle = aim.ToAngle();
                    Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0, 0, aimAngle);

                    Vector2 offset = Vector2.Transform(new Vector2(25, -8), aimQuat);

                    Vector2 vel = MathUtil.FromPolar(aimAngle, 11f);

                    if(dashDuration > 0)
                    {
                        EntityManager.Add(new Melee(Position + offset, vel, true));
                    }
                    else
                    {
                        EntityManager.Add(new Melee(Position + offset, vel, false));
                    }

                    

                    meleeCooldownRemaining = meleeCooldown;
                }

                if (Input.WasHookButtonPressed() && hookCooldownRemaining <= 0)
                {
                    float aimAngle = aim.ToAngle();

                    Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0, 0, aimAngle);

                    Vector2 offset = Vector2.Transform(new Vector2(25, -8), aimQuat);

                    Vector2 vel = MathUtil.FromPolar(aimAngle, 12f);

                    EntityManager.Add(new Hook(Position + offset, vel));

                    hookCooldownRemaining = hookCooldown;
                }

                if (cooldownRemaining > 0)
                {
                    cooldownRemaining--;
                }
                if (dashCooldownRemaining > 0)
                {
                    dashCooldownRemaining--;
                }
                if (meleeCooldownRemaining > 0)
                {
                    meleeCooldownRemaining--;
                }

                if(hookCooldownRemaining > 0)
                {
                    hookCooldownRemaining--;
                }

                if (dashDuration > 0)
                {
                    dashDuration--;
                }
                else
                {
                    speed = 4;
                }
                
                if (speedRemaining > 0)
                {
                    speed += 3f;
                    if (speed > 16)
                    {
                        speed = 16;
                    }

                }
                else
                {
                    speedRemaining--;
                }

                if (powerupTimeRemaining > 0)
                {
                    powerupTimeRemaining--;
                }
                else
                {
                    powerup = PowerupType.Null;
                }

                if(invincibilityRemaining > 0)
                {
                    invincibilityRemaining--;
                }
                else
                {
                    Invincibility = false;
                }

                if (forceFieldRemaining > 0)
                {
                    forceFieldRemaining--;
                }
                else
                {
                ForceFieldActive = false;
                }
            }

            public override void Draw(SpriteBatch spriteBatch)
            {
                if (!IsDead)
                {
                    base.Draw(spriteBatch);
                }
            }

            public void Kill()
            {
                PlayerStatus.RemoveLife();
                Invincibility = true;
                invincibilityRemaining = invincibilityDuration;
                framesUntilRespawn = PlayerStatus.IsGameOver ? 300 : 120;
                
                if(PlayerStatus.Lives == 0)
                {
                    // Trigger Game Over state instead of resetting immediately
                GameRoot.Instance.gameState = GameStates.GameOver;
                GameRoot.Instance.ShowGameOverScreen();
                // Update high score if needed
                if (PlayerStatus.Score > PlayerStatus.HighScore)
                {
                    PlayerStatus.HighScore = PlayerStatus.Score;
                    // Save new high score to file if applicable
                    PlayerStatus.SaveHighScore(PlayerStatus.HighScore);
                }
                }
                EnemySpawner.Reset();
            }



           
        }

       class Bullet : Entity
        {
            public Bullet(Vector2 position, Vector2 velocity)
            {
                image = Art.Bullet;
                Position = position;
                Velocity = velocity;
                Orientation = Velocity.ToAngle();
                Radius = 8;
            }

            public override void Update()
            {
                if (Velocity.LengthSquared() > 0)
                    Orientation = Velocity.ToAngle();
                Position += Velocity;
                // delete bullets that go off-screen 
                if (!GameRoot.Viewport.Bounds.Contains(Position.ToPoint()))
                    IsExpired = true;
            }
        }

        class Missile : Entity
        {
            public Missile(Vector2 position, Vector2 velocity)
            {
                image = Art.MissileBullet;
                Position = position;
                Velocity = velocity;
                Orientation = Velocity.ToAngle();
                Radius = 10;
            }

            public override void Update()
            {
                if (Velocity.LengthSquared() > 0)
                    Orientation = Velocity.ToAngle();
                Position += Velocity;
                // delete bullets that go off-screen 
                if (!GameRoot.Viewport.Bounds.Contains(Position.ToPoint()))
                    IsExpired = true;
            }
        }

        static class Input
        {
            private static KeyboardState keyboardState, lastKeyboardState;
            private static MouseState mouseState, lastMouseState;
            private static GamePadState gamepadState, lastGamepadState;

            private static bool isAimingWithMouse = false;

            public static Vector2 MousePosition { get { return new Vector2(mouseState.X, mouseState.Y); } }

            public static void Update()
            {
                lastKeyboardState = keyboardState;
                lastMouseState = mouseState;
                lastGamepadState = gamepadState;

                keyboardState = Keyboard.GetState();
                mouseState = Mouse.GetState();
                gamepadState = GamePad.GetState(PlayerIndex.One);

                if (new[] { Keys.Left, Keys.Right, Keys.Up, Keys.Down }.Any(x => keyboardState.IsKeyDown(x)) || gamepadState.ThumbSticks.Right != Vector2.Zero)
                    isAimingWithMouse = false;
                else if (MousePosition != new Vector2(lastMouseState.X, lastMouseState.Y))
                    isAimingWithMouse = true;
            }

            public static bool WasKeyPressed(Keys key)
            {
                return lastKeyboardState.IsKeyUp(key) && keyboardState.IsKeyDown(key);
            }

            

            public static bool WasButtonPressed(Buttons button)
            {
                return lastGamepadState.IsButtonUp(button) && gamepadState.IsButtonDown(button);
            }

            public static Vector2 GetMovementDirection()
            {
                Vector2 direction = gamepadState.ThumbSticks.Left;
                direction.Y *= -1;

                if (keyboardState.IsKeyDown(Keys.A))
                {
                    
                    direction.X -= 1;
                }
                if (keyboardState.IsKeyDown(Keys.D))
                {
                    direction.X += 1;
                }
                if (keyboardState.IsKeyDown(Keys.W))
                {
                    direction.Y -= 1;
                }
                if (keyboardState.IsKeyDown(Keys.S))
                {
                    direction.Y += 1;
                }

                if (direction.LengthSquared() > 1)
                {
                    direction.Normalize();
                }

                return direction;
            }

            public static Vector2 GetAimDirection()
            {
                if (isAimingWithMouse)
                {
                    return GetMouseAimDirection();
                }

                Vector2 direction = gamepadState.ThumbSticks.Right;
                direction.Y *= -1;

                if (keyboardState.IsKeyDown(Keys.Left))
                    direction.X -= 1;
                if (keyboardState.IsKeyDown(Keys.Right))
                    direction.X += 1;
                if (keyboardState.IsKeyDown(Keys.Up))
                    direction.Y -= 1;
                if (keyboardState.IsKeyDown(Keys.Down))
                    direction.Y += 1;

                if(direction == Vector2.Zero)
                {
                    return Vector2.Zero;
                }
                else
                {
                    return Vector2.Normalize(direction);
                }
            }

            private static Vector2 GetMouseAimDirection()
            {
                Vector2 direction = MousePosition - Playership.Instance.Position;

                if (direction == Vector2.Zero)
                {
                    return Vector2.Zero;
                }
                else
                {
                    return Vector2.Normalize(direction);
                }
            }

            public static bool WasBombButtonPressed()
            {
                return WasButtonPressed(Buttons.LeftTrigger) || WasButtonPressed(Buttons.RightTrigger) || WasKeyPressed(Keys.Space); 
            }

            public static bool WasDashButtonPressed()
            {
                return WasKeyPressed(Keys.LeftShift);
            }
            
            public static bool WasMeleeButtonPressed()
            {
                return lastMouseState.RightButton == ButtonState.Released && mouseState.RightButton == ButtonState.Pressed;
            }

            public static bool WasHookButtonPressed()
            {
                return WasKeyPressed(Keys.E);
            }

            public static bool WasGrappleButtonPressed()
            {
                return WasKeyPressed(Keys.C);
            }
        }

        class Enemy : Entity
        {
            private int timeUntilStart = 60;

            private static int explosionTime = 15;
            private int timeUntilExplosion;
            public bool IsActive { get { return timeUntilStart <= 0; } }

            public bool IsHooked { get; set; }
            public bool IsGrapple { get; set; }

            public Hook HookEntity { get; set; }

            public static Random rand = new Random();

            static int grappleCooldown = 60 * 2;
            int grappleCooldownRemaining = 0;

            public Vector2 superhitVel {  get; private set; }

            public bool IsSuperHit {  get; private set; }

            public bool IsDown { get; set; }

            public int PointValue { get; private set; }

            private List<IEnumerator<int>> behaviours = new List<IEnumerator<int>>();

            IEnumerable<int> FollowPlayer(float acceleration = 1f)
            {
                while (true)
                {
                    Velocity += (Playership.Instance.Position - Position).ScaleTo(acceleration);
                    if (Velocity != Vector2.Zero)
                    {
                        Orientation = Velocity.ToAngle();
                    }
                    yield return 0;
                }
            }

            IEnumerable<int> MoveRandomly()
            {
                float direction = rand.NextFloat(0, MathHelper.TwoPi);

                while (true)
                {
                    direction += rand.NextFloat(-0.1f, 0.1f);
                    direction = MathHelper.WrapAngle(direction);

                    for (int i = 0; i < 6; i++)
                    {
                        Velocity += MathUtil.FromPolar(direction, 0.4f);
                        Orientation -= 0.05f;

                        Rectangle bounds = new Rectangle(0, 220, (int)ScreenSize.X, 13 * 40);

                        
                       bounds.Inflate(-image.Width, -image.Height);

                        if (!bounds.Contains(Position.ToPoint()))
                        {
                            direction = (GameRoot.ScreenSize / 2 - Position).ToAngle() + rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);

                        }
                        yield return 0;
                    }
                }

            }

            public Enemy(Texture2D image, Vector2 position)
            {
                this.image = image;
                Position = position;
                Radius = image.Width / 2f;
                color = Color.Transparent;
                IsHooked = false;
                IsGrapple = false;
            }


            public override void Update()
            {
                if (!IsHooked && !IsDown && !Playership.Instance.Invincibility)
                {
                    if (timeUntilStart <= 0)
                    {
                        ApplyBehaviours();
                    }
                    else
                    {
                        timeUntilStart--;
                        color = Color.White * (1 - timeUntilStart / 60f);
                    }

                    Position += Velocity;
                    Position = Vector2.Clamp(Position, Size / 2, GameRoot.ScreenSize - Size / 2);

                    Velocity *= 0.8f;
                }
                else if (IsGrapple)
                    {
                        float acceleration = 1f;
                        Velocity += (Playership.Instance.Position - Position).ScaleTo(acceleration);
                        Position += Velocity;

                        Velocity *= 0.9f;
                    }
                else if (IsSuperHit)
                {
                    Position += superhitVel;
                    if (timeUntilExplosion <= 0)
                    {
                        EntityManager.Add(new Explosion(Position, 1f));
                        WasShot();
                    }
                    else
                    {
                        timeUntilExplosion--;
                    }
                }

                if (grappleCooldownRemaining > 0)
                {
                    grappleCooldownRemaining--;
                }
                else
                {
                    IsDown = false;
                }
            }


            public void WasShot()
            {
                IsExpired = true;
                PlayerStatus.AddPoints(PointValue);
                PlayerStatus.IncreaseMultiplier();
                PlayerStatus.IncreaseChain();
                Wave.EnemyKilled();
                
            }

            public void WasGrappled(Hook hook)
            {
                IsGrapple = true;
                HookEntity = hook;
            }

            public void WasUngrappled()
            {
                IsGrapple = false;
                HookEntity = null;
                grappleCooldownRemaining = grappleCooldown;
                IsDown = true;
            }

            public void SuperHit(float angle)
            {
                
                superhitVel = MathUtil.FromPolar(angle, 11f);
                IsSuperHit = true;
                IsGrapple = false;
                IsHooked = false;
                IsDown = true;
                grappleCooldownRemaining = grappleCooldown;
                timeUntilExplosion = explosionTime;
                if(HookEntity != null)
                {
                    HookEntity.UnhookEnemy();
                }
                

            }

            private void AddBehaviour(IEnumerable<int> behaviour)
            {
                behaviours.Add(behaviour.GetEnumerator());
            }

            private void ApplyBehaviours()
            {
                for (int i = 0; i < behaviours.Count; i++)
                {
                    if (!behaviours[i].MoveNext())
                    {
                        behaviours.RemoveAt(i--);
                    }
                }
            }

            public static Enemy CreateSeeker(Vector2 position)
            {
                var enemy = new Enemy(Art.Seeker, position);
                enemy.AddBehaviour(enemy.FollowPlayer());
                enemy.PointValue = 2;

                return enemy;
            }

            public static Enemy CreateWanderer(Vector2 position)
            {
                var enemy = new Enemy(Art.Wanderer, position);
                enemy.AddBehaviour(enemy.MoveRandomly());
                enemy.PointValue = 1;
                return enemy;
            }

            public void HandleCollision(Enemy other)
            {
                if (IsSuperHit)
                {
                    timeUntilExplosion = 0;
                }
                else
                {
                    var d = Position - other.Position;
                    Velocity += 10 * d / (d.LengthSquared() + 1);
                }
                
            }
        }

        static class EnemySpawner
        {
            static Random rand = new Random();
            static float inverseSpawnChance = 150;
            static int enemiesSpawned = 0;
            static int bossesSpawned = 0;

            public static void Update()
            {
                if (!Playership.Instance.IsDead && EntityManager.Count < 400 && EntityManager.enemies.Count < 30)
                {
                    if (rand.Next((int)inverseSpawnChance) == 0 && enemiesSpawned < Wave.totalEnemies && !Wave.isBoss){
                        EntityManager.Add(Enemy.CreateSeeker(GetSpawnPosition()));
                        enemiesSpawned++;
                    }
                    else if (Wave.isBoss && rand.Next((int)inverseSpawnChance ) == 0 && !Boss.Instance.isDead)
                    {
                        EntityManager.Add(Enemy.CreateSeeker(GetSpawnPosition()));
                    }

                    if (rand.Next((int)inverseSpawnChance) == 0 && enemiesSpawned < Wave.totalEnemies)
                    {
                        EntityManager.Add(Enemy.CreateWanderer(GetSpawnPosition()));
                        enemiesSpawned++;
                    }
                    else if (Wave.isBoss && rand.Next((int)inverseSpawnChance) == 0 && !Boss.Instance.isDead)
                    {
                        EntityManager.Add(Enemy.CreateWanderer(GetSpawnPosition()));
                    }
                }

                if (Wave.isBoss && bossesSpawned < 1)
                {
                    EntityManager.Add(new Boss(100 * (Wave.wave / 5)));
                    bossesSpawned++;
                }

                if (inverseSpawnChance > 30)
                {
                    inverseSpawnChance -= 0.01f * Wave.wave / 2;
                }
            }

            private static Vector2 GetSpawnPosition()
            {
                Vector2 pos;
                do
                {
                    pos = new Vector2(rand.Next((int)GameRoot.ScreenSize.X), rand.Next((int)GameRoot.ScreenSize.Y - 60));
                    if (pos.Y < 260)
                    {
                        pos.Y += 260;
                    }
                }
                while (Vector2.DistanceSquared(pos, Playership.Instance.Position) < 200 * 200);

                return pos;
            }

            public static void Reset()
            {
                inverseSpawnChance = 150 - Wave.wave * 1.5f;
            }

            public static void WaveReset()
            {
                enemiesSpawned = 0;
                Reset();
                bossesSpawned = 0;
            }
        }

        static class PowerupSpawner
        {
            static Random rand = new Random();
            static float inverseSpawnChance = 7000;

            public static void Update()
            {
                if (!Playership.Instance.IsDead && EntityManager.powerups.Count < 3)
                {
                    if (rand.Next((int)inverseSpawnChance) == 0)
                    {
                        EntityManager.Add(new Powerup(PowerupType.Missile, GetSpawnPosition()));
                    }

                    if (rand.Next((int)inverseSpawnChance) == 0)
                    {
                        EntityManager.Add(new Powerup(PowerupType.Nuke, GetSpawnPosition()));
                    }

                    if (rand.Next((int)inverseSpawnChance) == 0)
                    {
                        EntityManager.Add(new Powerup(PowerupType.Diamond, GetSpawnPosition()));
                    }

                    if (rand.Next((int)inverseSpawnChance) == 0)
                    {
                        EntityManager.Add(new Powerup(PowerupType.Speed, GetSpawnPosition()));
                    }

                    if (rand.Next((int)inverseSpawnChance) == 0)
                    {
                        EntityManager.Add(new Powerup(PowerupType.Rapid, GetSpawnPosition()));
                    }

                    if (rand.Next((int)inverseSpawnChance) == 0)
                    {
                        EntityManager.Add(new Powerup(PowerupType.ExtraLife, GetSpawnPosition()));
                    }

                    if (rand.Next((int)inverseSpawnChance) == 0)
                    {
                        EntityManager.Add(new Powerup(PowerupType.Invincibility, GetSpawnPosition()));
                    }

                    if (rand.Next((int)inverseSpawnChance) == 0)
                    {
                        EntityManager.Add(new Powerup(PowerupType.ForceField, GetSpawnPosition()));
                    }

                }

            }

            private static Vector2 GetSpawnPosition()
            {
                Vector2 pos;
                do
                {
                    pos = new Vector2(rand.Next((int)GameRoot.ScreenSize.X), rand.Next((int)GameRoot.ScreenSize.Y - 100));
                    if (pos.Y < 300)
                    {
                        pos.Y += 300;
                    }
                }
                while (Vector2.DistanceSquared(pos, Playership.Instance.Position) < 250 * 250);

                return pos;
            }

            
        }

        static class PlayerStatus
        {
            // amount of time it takes, in seconds, for a multiplier to expire
            private const float multiplierExpiryTime = 0.8f;
            private const int maxMultiplier = 20;

            private const float chainExpiryTime = 5f;
            

            public static int Lives { get; private set; }
            public static int Score { get; private set; }
            public static int Multiplier {  get; private set; }
            public static int Chain {  get; private set; }

            public static int HighScore { get; set; }

            private static float multiplierTimeLeft;
            private static int scoreForExtraLife;

            private static float chainTimeLeft;

            public static bool Speed1 {  get; private set; }
            public static bool FiringSpeed1 {  get; private set; }
            public static bool ExplosiveBullets {  get; private set; }

            private const string highScoreFilename = "highscore.txt";

            public static bool IsGameOver { get { return Lives == 0; } }



            // Static constructor

            static PlayerStatus()
            {
                HighScore = LoadHighScore();
                Reset();
            }

            public static void Reset()
            {
                if (Score > HighScore)
                {
                    SaveHighScore(HighScore = Score);
                }

                Score = 0;
                Multiplier = 1;
                Lives = 4;
                scoreForExtraLife = 2000;
                multiplierTimeLeft = 0;
                Chain = 0;
                chainTimeLeft = 0;
            }

            public static void Update()
            {
                if (Multiplier > 1)
                {
                    if ((multiplierTimeLeft -= (float)GameRoot.GameTime.ElapsedGameTime.TotalSeconds) <= 0)
                    {
                        multiplierTimeLeft = multiplierExpiryTime;
                        ResetMultiplier();
                    }
                }



                if (Chain > 0)
                {
                    if ((chainTimeLeft -= (float)GameRoot.GameTime.ElapsedGameTime.TotalSeconds) <= 0)
                    {
                        chainTimeLeft = chainExpiryTime;
                        ResetChain();
                    }
                    if(Chain >= 10)
                    {
                        Speed1 = true;
                    }
                    else
                    {
                        Speed1 = false;
                    }

                    if (Chain >= 25)
                    {
                        FiringSpeed1 = true;
                    }
                    else { FiringSpeed1 = false; }

                    if (Chain >= 50)
                    {
                        ExplosiveBullets = true;
                    }
                    else
                    {
                        ExplosiveBullets = false;
                    }
                }
            }

            public static void AddPoints(int basePoints)
            {
                if (Playership.Instance.IsDead)
                    return;
                Score += basePoints * Multiplier;
                while (Score >= scoreForExtraLife)
                {
                    scoreForExtraLife += 2000;
                    Lives++;
                }
            }
            public static void IncreaseMultiplier()
            {
                if (Playership.Instance.IsDead)
                    return;
                multiplierTimeLeft = multiplierExpiryTime;
                if (Multiplier < maxMultiplier)
                    Multiplier++;
            }

            public static void IncreaseChain()
            {
                if (Playership.Instance.IsDead)
                    return;
                chainTimeLeft = chainExpiryTime;
                Chain++;
            }
            public static void ResetMultiplier()
            {
                Multiplier = 1;
            }

            public static void ResetChain()
            {
                Chain = 0;
                Speed1 = false;
                FiringSpeed1 = false;
                ExplosiveBullets = false;
            }
            public static void RemoveLife()
            {
                Lives--;
                ResetChain();
            }

            public static void AddLife()
            {
                Lives++;
            }

            private static int LoadHighScore()
            {
                // return the saved high score if possible and return 0 otherwise
                int score;
                return File.Exists(highScoreFilename) && int.TryParse(File.ReadAllText(highScoreFilename), out score) ? score: 0;
            }

            public static void SaveHighScore(int score)
            {
                File.WriteAllText(highScoreFilename, score.ToString());
            }
        }

        static class Sound
        {
            public static Song Music { get; private set; }

            

            private static readonly Random rand = new Random();

            //I commented these out because I dont have any sound effects yet
            //private static SoundEffect[] explosions;
            // return a random explosion sound

            /*
            public static SoundEffect Explosion { get { return explosions[rand.Next(explosions.Length)];} }

            private static SoundEffect[] shots;
            public static SoundEffect Shot { get { return shots[rand.Next(shots.Length)];} }

            private static SoundEffect[] spawns;
            public static SoundEffect Spawn { get { return spawns[rand.Next(spawns.Length)]; } }
            */

            public static void Load(ContentManager content)
            {
                Music = content.Load<Song>("ultrakill");

                // These linq expressions are just a fancy way loading all sounds of each category into an array
                // I dont have sound effects so uh
                /*
                explosions = Enumerable.Range(1, 8).Select(x => content.Load<SoundEffect>("Sound/explosion-0" + x)).ToArray();
                shots = Enumerable.Range(1, 4).Select(x => content.Load<SoundEffect>("Sound/shoot-0" + x)).ToArray();
                spawns = Enumerable.Range(1, 8).Select(x => content.Load<SoundEffect>("Sound/spawn-0" + x)).ToArray();
                */
            }
        }

        class Melee : Entity
        {
            public int forward = 0;
            public int backward = 0;
            static int meleeLength = 30;
            ContentManager content;
            AnimatedTexture spriteTexture;
            int meleeOutTime = 0;
            Vector2 offset;
            public float aimAngle { get; private set; }
            public bool Super {  get; private set; }
            public Melee(Vector2 position, Vector2 velocity, bool super)
            {
                
                Velocity = Vector2.Zero;
                Position = position;
                Orientation = velocity.ToAngle();
                Vector2 offsetAnimation = new Vector2(0, Art.MeleeAnimated.Height / 2);


                if (super)
                {
                    Radius = 40;
                    spriteTexture = new AnimatedTexture(Vector2.Zero + offsetAnimation, Orientation, 1.25f, .5f);
                }
                else
                {
                    Radius = 25;
                    spriteTexture = new AnimatedTexture(Vector2.Zero + offsetAnimation, Orientation, .75f, .5f);
                }
                Super = super;
                image = Art.Melee;

                spriteTexture.Load(content, Art.MeleeAnimated, 7, 14);

                var aim = Input.GetAimDirection();
                aimAngle = aim.ToAngle();

                Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0, 0, aimAngle);

                offset = Vector2.Transform(new Vector2(25, -8), aimQuat);
            }

            public override void Update()
            {
                EntityManager.HandleMelee(this);
               
                
                Position = Playership.Instance.Position + offset;

                float elapsed = (float)GameTime.ElapsedGameTime.TotalSeconds;
                spriteTexture.UpdateFrame(elapsed);

                


                if(meleeOutTime > meleeLength)
                {
                    IsExpired = true;
                }
                else
                {
                    meleeOutTime++;
                }
            }

            public override void Draw(SpriteBatch spriteBatch)
            {
                Vector2 characterPos = new Vector2(Viewport.Width / 2, Viewport.Height / 2); 
                spriteTexture.DrawFrame(spriteBatch, characterPos, Playership.Instance.Position);
            }
        }

        class Hook : Entity
        {
            private int movement = 0;
            private int hookTime = 0;
            public bool hooked {  get; private set; }
            public bool actionTaken { get; private set; }
            public bool reeling {  get; private set; }
            private Enemy enemy;

            public Hook(Vector2 position, Vector2 velocity)
            {
                image = Art.Hook;
                Position = position;
                Velocity = velocity;
                Orientation = Velocity.ToAngle();
                Radius = 14;
            }

            public override void Update()
            {
                if (Playership.Instance.IsDead)
                {
                    IsExpired = true;
                    return;
                }
                EntityManager.HandleHook(this);
                
                if (!hooked)
                {
                    if(movement <= 25)
                    {
                        Position += Velocity;
                        movement++;
                        reeling = false;
                    }
                    else
                    {
                        reeling = true;
                        float acceleration = 1.5f;
                        Velocity += (Playership.Instance.Position - Position).ScaleTo(acceleration);
                        Position += Velocity;

                        Velocity *= 0.9f;

                        float radius = Playership.Instance.Radius + Radius;
                        if (Vector2.DistanceSquared(Playership.Instance.Position, Position) < radius * radius)
                        {
                            IsExpired = true;
                        }
                    }
                    
                    
                }
                else
                {
                    if (Input.WasGrappleButtonPressed())
                    {
                        enemy.WasGrappled(this);
                        actionTaken = true;
                    }
                    if(enemy != null)
                    {
                        Position = enemy.Position;
                        if (!actionTaken)
                        {
                            hookTime++;
                        }

                        if (enemy.IsExpired)
                        {
                            IsExpired = true;
                        }

                        if (hookTime >= 60 * 5)
                        {
                            UnhookEnemy();
                        }
                    }
                    

                    
                    
                }
            }

            public void HookEnemy(Enemy hookedEnemy)
            {
                Position = hookedEnemy.Position;
                enemy = hookedEnemy;
                hookedEnemy.IsHooked = true;
                hooked = true;
            }

            public void UnhookEnemy()
            {
                enemy.IsHooked = false;
                enemy = null;
                IsExpired = true;
            }

            
        }

        class Explosion : Entity
        {
            ContentManager content;
            AnimatedTexture spriteTexture;
            
            int durationRemaining = 25;

            public bool isVisual {  get; set; }

            public Explosion(Vector2 position, float scale)
            {

                isVisual = false; // If the explosion already hit a boss, isVisual lets the animation keep going without hitting the boss infinitely
                Position = position;
                Orientation = 0;
                Radius = 35;
                Vector2 offsetAnimation = new Vector2(Art.Explosion.Width / 12, Art.Explosion.Height / 2);
                spriteTexture = new AnimatedTexture(Vector2.Zero + offsetAnimation, Orientation, scale, 0f);

                spriteTexture.Load(content, Art.Explosion, 6, 12);
            }

            public override void Update()
            {
                float elapsed = (float)GameTime.ElapsedGameTime.TotalSeconds;
                spriteTexture.UpdateFrame(elapsed);

                if(durationRemaining <= 0)
                {
                    IsExpired = true;
                }
                else
                {
                    durationRemaining--;
                }
            }

            public override void Draw(SpriteBatch spriteBatch)
            {
                Vector2 characterPos = new Vector2(Viewport.Width / 2, Viewport.Height / 2);
                spriteTexture.DrawFrame(spriteBatch, characterPos, Position);
            }
        }
       
        class Powerup : Entity
        {
            public PowerupType type { get; private set; }
            private int lifeTime = 60 * 15;

            public Powerup(PowerupType typeSet, Vector2 position)
            {
                type = typeSet;
                Position = position;
                Radius = 25;

                switch (typeSet)
                {
                    case PowerupType.Diamond:
                        image = Art.Diamond;
                        break;

                    case PowerupType.Nuke:
                        image = Art.Nuke; break;

                    case PowerupType.Missile:
                        image = Art.Missile; break;

                    case PowerupType.Speed:
                        image = Art.Speed; break;

                    case PowerupType.Rapid:
                        image = Art.RapidFire; break;

                    case PowerupType.ExtraLife:
                    image = Art.ExtraLife; break;

                    case PowerupType.Invincibility:
                    image = Art.Invincibility; break;

                    case PowerupType.ForceField:
                    image = Art.ForceField; break;
                }
            }

            public override void Update()
            {
                if (lifeTime > 0)
                {
                    lifeTime--;
                }
                else
                {
                    IsExpired = true;
                }
            }
        }

        class Boss : Entity
        {
            public int HP { get; private set; }

            public static Random rand = new Random();

            int guaranteedFire = 0;

            int randomFireChance = 100;

            bool tookDamage = false;

            int damageTimer = 0;

            private int timeUntilStart = 60;
            public bool IsActive { get { return timeUntilStart <= 0; } }

            private List<IEnumerator<int>> behaviours = new List<IEnumerator<int>>();

            private static Boss instance;
            public static Boss Instance
            {
                get
                {
                    if (instance == null)
                        instance = new Boss(0);
                    return instance;
                }
            }

            public bool isDead {  get; private set; }

            int deadTimer = 0;

            public Boss(int hp)
            {
                HP = hp;
                image = Art.Boss;
                Position = new Vector2(ScreenSize.X / 2, ScreenSize.Y / 2);
                isDead = false;
                
                Radius = image.Width / 2f;

                AddBehaviour(MoveRandomly());
            }

            public override void Update()
            {
                if (timeUntilStart <= 0)
                {
                    ApplyBehaviours();


                    Position += Velocity;
                    Position = Vector2.Clamp(Position, Size / 2, GameRoot.ScreenSize - Size / 2);

                    Velocity *= 0.8f;

                    if (HP <= 0)
                    {
                        isDead = true;
                    }

                    if (!isDead)
                    {
                        if (rand.Next(randomFireChance) == 0)
                        {
                            Fire();
                        }

                        if (guaranteedFire < 300)
                        {
                            guaranteedFire++;
                        }
                        else
                        {
                            Fire();
                            guaranteedFire = 0;
                        }
                    }
                    else
                    {
                        deadTimer++;

                        if (deadTimer % 20 == 0)
                        {
                            Die();
                        }

                        if (deadTimer >= 300)
                        {
                            Wave.BossKilled();
                            IsExpired = true;
                        }
                    }
                }
                else
                {
                    timeUntilStart--;
                    color = Color.White * (1 - timeUntilStart / 60f);
                }

                
                

            }

            private void AddBehaviour(IEnumerable<int> behaviour)
            {
                behaviours.Add(behaviour.GetEnumerator());
            }

            IEnumerable<int> MoveRandomly()
            {
                float direction = rand.NextFloat(0, MathHelper.TwoPi);

                while (true)
                {
                    direction += rand.NextFloat(-0.1f, 0.1f);
                    direction = MathHelper.WrapAngle(direction);

                    for (int i = 0; i < 6; i++)
                    {
                        Velocity += MathUtil.FromPolar(direction, 0.4f);
                       

                        Rectangle bounds = new Rectangle(0, 220, (int)ScreenSize.X, 13 * 40);


                        bounds.Inflate(-image.Width, -image.Height);

                        if (!bounds.Contains(Position.ToPoint()))
                        {
                            direction = (GameRoot.ScreenSize / 2 - Position).ToAngle() + rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);

                        }
                        yield return 0;
                    }
                }

            }

            private void ApplyBehaviours()
            {
                for (int i = 0; i < behaviours.Count; i++)
                {
                    if (!behaviours[i].MoveNext())
                    {
                        behaviours.RemoveAt(i--);
                    }
                }
            }

            private void Fire()
            {
                
                Vector2 vel1 = MathUtil.FromPolar((float)Math.PI / 4, 7f);
                Vector2 vel2 = MathUtil.FromPolar(3 * (float)Math.PI / 4, 7f);
                Vector2 vel3 = MathUtil.FromPolar(5 * (float)Math.PI / 4, 7f);
                Vector2 vel4 = MathUtil.FromPolar(7 * (float)Math.PI / 4, 7f);

                EntityManager.Add(new BossBullet(Position, vel1));
                EntityManager.Add(new BossBullet(Position, vel2));
                EntityManager.Add(new BossBullet(Position, vel3));
                EntityManager.Add(new BossBullet(Position, vel4));
            }

            public void TakeDamage(int damage)
            {
                if (HP > 0)
                {
                    HP -= damage;
                }
                tookDamage = true;

                Debug.WriteLine("Boss HP: " + HP);
                
            }

            public override void Draw(SpriteBatch spriteBatch)
            {

                
                if (tookDamage)
                {
                    for(int i = 0; i < 3; i++)
                    {
                        spriteBatch.Draw(image, Position, null, Color.Red, Orientation, Size / 2f, 2f, 0, .1f);
                    }
                    damageTimer++;

                    if(damageTimer >= 5)
                    {
                        tookDamage = false;
                        damageTimer = 0;
                    }
                    
                }
                else
                {
                    spriteBatch.Draw(image, Position, null, color, Orientation, Size / 2f, 2f, 0, .1f);
                }
            }

            private void Die()
            {
                int randX = rand.Next(40) - 20;
                int randY = rand.Next(40) - 20;
                Vector2 randPos = new Vector2(randX, randY);
                EntityManager.Add(new Explosion(Position + randPos, 2f));
            }

        }

        class BossBullet : Entity
        {
            public BossBullet(Vector2 position, Vector2 velocity)
            {
                image = Art.BossBullet;
                Position = position;
                Velocity = velocity;
                Orientation = Velocity.ToAngle();
                Radius = 8;
            }

            public override void Update()
            {
                if (Velocity.LengthSquared() > 0)
                    Orientation = Velocity.ToAngle();
                Position += Velocity;
                // delete bullets that go off-screen 
                if (!GameRoot.Viewport.Bounds.Contains(Position.ToPoint()))
                    IsExpired = true;
            }
        }

        public class AnimatedTexture
        {
            private int frameCount;

            private Texture2D myTexture;

            private float timePerFrame;

            private int frame;

            private float totalElapsed;

            private bool isPaused;

            public float Rotation, Scale, Depth;

            public Vector2 Origin;

            public AnimatedTexture(Vector2 origin, float rotation, float scale, float depth)
            {
                this.Origin = origin;
                this.Rotation = rotation;
                this.Scale = scale;
                this.Depth = depth;
            }

            public void Load(ContentManager content, Texture2D image, int frameCount, int framesPerSec)
            {
                this.frameCount = frameCount;
                myTexture = image;
                timePerFrame = (float)1 / framesPerSec;
                frame = 0;
                totalElapsed = 0;
                isPaused = false;
            }

            public void UpdateFrame(float elapsed)
            {
                if (isPaused)
                {
                    return;
                }

                totalElapsed += elapsed;
                if(totalElapsed > timePerFrame)
                {
                    frame++;

                    frame %= frameCount;
                    totalElapsed -= timePerFrame;
                }
            }

            public void DrawFrame(SpriteBatch batch, Vector2 screenPos, Vector2 position)
            {
                DrawFrame(batch, frame, screenPos, position);
            }

            public void DrawFrame(SpriteBatch batch, int frame, Vector2 screenPos, Vector2 position)
            {
                int FrameWidth = myTexture.Width / frameCount;

                int row = frame / (myTexture.Width / FrameWidth);
                int column = frame % (myTexture.Width / FrameWidth);

                Rectangle sourcerect = new Rectangle(FrameWidth * column, row * myTexture.Height, FrameWidth, myTexture.Height);
                batch.Draw(myTexture, position, sourcerect, Color.White, Rotation, Origin, Scale, SpriteEffects.None, Depth);
            }

            public bool IsPaused
            {
                get { return isPaused; }
            }

            public void Reset()
            {
                frame = 0; totalElapsed = 0;
            }

            public void Stop()
            {
                Pause();
                Reset();
            }

            public void Play()
            {
                isPaused = false;
            }

            public void Pause()
            {
                isPaused = true;
            }
        }

        public static class Wave
        {
            public static int wave = 1;
            public static int numEnemies = 15;
            public static int totalEnemies = 15;

            public static int numBosses = 1;
            public static int totalBosses = 1;

            public static bool isBoss { get; private set; } 
        

            public static void IncreaseWave()
            {
                wave++;

                if(wave % 5 == 0)
                {
                    isBoss = true;
                    
                }
                else
                {
                    isBoss = false;
                }
                numEnemies = 15 + (5 * wave);
                totalEnemies = numEnemies;
                EntityManager.DestroyAllEnemies();

            }

            public static void EnemyKilled()
            {
                if (!isBoss)
                {
                    numEnemies--;
                }
                
            }

            public static void BossKilled()
            {
                numEnemies = 0;
                EntityManager.DestroyAllEnemies();
            }

        public static void ResetWaves()
        {
            wave = 1;
            isBoss = false;
            numEnemies = 15;
            totalEnemies = 15;
            numBosses = 1;
            totalBosses = 1;
            EntityManager.DestroyAllEnemies();
        }

        }

        private List<Button> gameComponents;

        //Background
        private Dictionary<Vector2, int> bg;
        private Dictionary<Vector2, int> fg;
        private Dictionary<Vector2, int> collisions;
        private Texture2D textureAtlas;

        float scrollOffset = 0f;
        float scrollSpeed = 200f;
        float scrollLimit = 1200f;
        bool scrolling = true;

        int tileWidth = 40;
        int tileHeight = 40;
        private int TILESIZE = 40;

        int tilesPerRow;
        int mapWidthInTiles;
        int mapWidthInPixels;

        private Sprite player;

        private List<Rectangle> intersections;

        public List<Rectangle> getIntersectingTilesHorizontal(Rectangle target)
        {
            List<Rectangle> intersections = new();

            int widthInTiles = (target.Width - (target.Width % TILESIZE)) / TILESIZE;
            int heightInTiles = (target.Height - (target.Height % TILESIZE)) / TILESIZE;

            for (int x = 0; x <= widthInTiles; x++)
            {
                for (int y = 0; y <= heightInTiles; y++)
                {
                    intersections.Add(new Rectangle(

                        (target.X + x*TILESIZE) / TILESIZE,
                        (target.Y + y*(TILESIZE - 1)) / TILESIZE,
                        TILESIZE,
                        TILESIZE

                        ));
                }
            }
            return intersections;
        }

        public List<Rectangle> getIntersectingTilesVertical(Rectangle target)
        {
            List<Rectangle> intersections = new();

            int widthInTiles = (target.Width - (target.Width % TILESIZE)) / TILESIZE;
            int heightInTiles = (target.Height - (target.Height % TILESIZE)) / TILESIZE;

            for (int x = 0; x <= widthInTiles; x++)
            {
                for (int y = 0; y <= heightInTiles; y++)
                {
                    intersections.Add(new Rectangle(

                        (target.X + x * (TILESIZE - 1)) / TILESIZE,
                        (target.Y + y * (TILESIZE)) / TILESIZE,
                        TILESIZE,
                        TILESIZE

                        ));
                }
            }
            return intersections;
        }

        private Dictionary<Vector2, int> LoadMap(string filepath)
        {
            Dictionary<Vector2, int> result = new();

            StreamReader reader = new(filepath);

            int y = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] items = line.Split(',');

                for (int x = 0; x < items.Length; x++)
                {
                    if (int.TryParse(items[x], out int value))
                    {
                        if (value > -1)
                        {
                            result[new Vector2(x, y)] = value;
                        }
                    }
                }

                y++;

            }

            return result;
        }

        public GameRoot()
        {
            _graphics = new GraphicsDeviceManager(this);
            //_graphics.IsFullScreen = true;
            _graphics.PreferredBackBufferWidth = 1200;
            _graphics.PreferredBackBufferHeight = 800;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Instance = this;

            gameState = GameStates.Menu;

            bg = LoadMap("../../../Content/Data/chainlinkFINAL_bg.csv");
            collisions = LoadMap("../../../Content/Data/chainlinkFINAL_collisions.csv");

            //do the same loops for collisions as bg
            scrolling = false;
            

        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
            EntityManager.Add(Playership.Instance);
            
           MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(Sound.Music);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Art.Load(Content);

            Sound.Load(Content);
            
            var startButton = new Button(Art.Button, Art.Font)
            {
                Position = new Vector2(ScreenSize.X / 2 - Art.Button.Width / 2, ScreenSize.Y / 2 - Art.Button.Height),
                Text = "Start",
            };

            var quitButton = new Button(Art.Button, Art.Font)
            {
                Position = new Vector2(ScreenSize.X / 2 - Art.Button.Width / 2, ScreenSize.Y / 2 + 60 - Art.Button.Height),
                Text = "Quit",
            };


            startButton.Click += StartButton_Click;
            quitButton.Click += QuitButton_Click;

            gameComponents = new List<Button>()
            {
                startButton,
                quitButton,
            };
            // TODO: use this.Content to load your game content here

            textureAtlas = Content.Load<Texture2D>("Data/chainLinkTileSet2");
            tilesPerRow = textureAtlas.Width / tileWidth;

            mapWidthInTiles = bg.Keys.Max(v => (int)v.X) + 1;
            mapWidthInPixels = mapWidthInTiles * tileWidth;

            player = new Sprite(
                Content.Load<Texture2D>("Player"),
                new Rectangle(300, 300, TILESIZE, TILESIZE),
                new Rectangle(0, 0, 40, 40)
                );
        }

        private void QuitButton_Click(object sender, EventArgs e)
        {
            Exit();
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            gameState = GameStates.Game;
        }

        /// Randomly places obstacle blocks at the start of each wave.
        /// Obstacles use the border tile texture and avoid player spawn and map edges.
        private void PlaceWaveObstacles()
        {
            // Determine a tile index to use for obstacles (use an existing border tile)
            int obstacleTileIndex = 0;
            if (collisions.Count > 0)
            {
                // Use the first collision tile's index as the obstacle (assumes border tiles are used for collisions)
                obstacleTileIndex = collisions.Values.First();
            }

            // Decide how many obstacles to place (increases with wave number)
            int obstaclesToPlace = 3 + (Wave.wave / 2);  // e.g., Wave1:3 obstacles, Wave2:4, Wave4:5, etc.
            obstaclesToPlace = Math.Min(obstaclesToPlace, 15); // cap to prevent excessive obstacles

            Random rand = new Random();
            for (int i = 0; i < obstaclesToPlace; i++)
            {
                // Try to find a valid position for an obstacle
                bool placed = false;
                for (int attempt = 0; attempt < 100 && !placed; attempt++)
                {
                    // Random tile coordinates within the play area (avoiding outermost border tiles)
                    int tileX = rand.Next(1, mapWidthInTiles - 1);
                    int tileY = rand.Next(7, (int)ScreenSize.Y / TILESIZE - 2); 
                    // Note: Y starts at 7 to avoid the top UI/banner area (260px/40px ≈ 6.5, so start at 7)

                    var tilePos = new Vector2(tileX, tileY);

                    // Skip if this position is at a collision (wall/boundary) or too close to player spawn
                    if (collisions.ContainsKey(tilePos))
                        continue;
                    Vector2 worldPos = new Vector2(tileX * TILESIZE, tileY * TILESIZE);
                    if (Vector2.DistanceSquared(worldPos, Playership.Instance.Position) < 200 * 200)
                        continue;

                    // Place obstacle: add to background and collisions using the border tile
                    bg[tilePos] = obstacleTileIndex;
                    collisions[tilePos] = obstacleTileIndex;
                    placed = true;
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
            GameTime = gameTime;
            Input.Update();

            //Waves
            if(Wave.numEnemies == 0)
            {
                scrolling = true;
                scrollOffset = 0;
                Wave.IncreaseWave();
                EnemySpawner.WaveReset();
                // Place new obstacles at the start of each wave
                PlaceWaveObstacles();

                
            }

            

            if (gameState == GameStates.Menu || gameState == GameStates.Paused || gameState == GameStates.GameOver)
            {
                MediaPlayer.Pause();
                foreach (var Button in gameComponents)
                {
                    Button.Update(gameTime);
                }
            }
            else if (!scrolling)
            {
                MediaPlayer.Resume();
                EntityManager.Update();
                EnemySpawner.Update();
                PowerupSpawner.Update();

                
                PlayerStatus.Update();

                

                base.Update(gameTime);
            }
            else
            {
                EntityManager.Update();
                MediaPlayer.Resume();

                float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
                    scrollOffset += scrollSpeed * delta;

                    if (scrollOffset >= scrollLimit)
                    {
                        scrollOffset = scrollLimit;
                        scrolling = false;

                    }
                    EntityManager.DestroyPowerups();
                
            }


            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Input.WasKeyPressed(Keys.Escape))
                if(gameState == GameStates.Game)
                {
                    gameState = GameStates.Paused;
                }
                else
                {
                    gameState = GameStates.Game;
                }


            // TODO: Add your update logic here

            

            intersections = getIntersectingTilesHorizontal(player.rect);

            foreach(var rect in intersections)
            {
                if(collisions.TryGetValue(new Vector2(rect.X, rect.Y), out int _val))
                {
                    Rectangle collision = new Rectangle(
                        rect.X * TILESIZE,
                        rect.Y * TILESIZE,
                        TILESIZE,
                        TILESIZE
                        );

                    if(player.velocity.X > 0.0f)
                    {
                        player.rect.X = collision.Left - player.rect.Width;
                    }
                    else if (player.velocity.X < 0.0f)
                    {
                        player.rect.X = collision.Right;
                    }
                }
            }

            intersections = getIntersectingTilesVertical(player.rect);

            foreach (var rect in intersections)
            {
                if (collisions.TryGetValue(new Vector2(rect.X, rect.Y), out int _val))
                {
                    Rectangle collision = new Rectangle(
                        rect.X * TILESIZE,
                        rect.Y * TILESIZE,
                        TILESIZE,
                        TILESIZE
                        );

                    if (player.velocity.Y > 0.0f)
                    {
                        player.rect.Y = collision.Top - player.rect.Height;
                    }
                    else if (player.velocity.Y < 0.0f)
                    {
                        player.rect.Y = collision.Bottom;
                    }
                }
            }

        }

        public void ShowGameOverScreen()
        {
            // Create Restart button
            var restartButton = new Button(Art.Button, Art.Font)
            {
                Position = new Vector2(ScreenSize.X / 2 - Art.Button.Width / 2, ScreenSize.Y / 2 - Art.Button.Height),
                Text = "Restart",
            };
            restartButton.Click += RestartButton_Click;

            // Create Quit button (reuse same handler as main menu quit)
            var quitButton = new Button(Art.Button, Art.Font)
            {
                Position = new Vector2(ScreenSize.X / 2 - Art.Button.Width / 2, ScreenSize.Y / 2 + 60 - Art.Button.Height),
                Text = "Quit",
            };
            quitButton.Click += QuitButton_Click;

            // Replace current gameComponents with Game Over screen buttons
            gameComponents = new List<Button>() { restartButton, quitButton };
        }

        private void RestartButton_Click(object sender, EventArgs e)
        {
            // Reset player and game status to initial values
            PlayerStatus.Reset();         // resets score, multiplier, lives, and updates HighScore if needed
            Wave.ResetWaves();            // reset wave count and enemy counts to start from wave 1
            EntityManager.DestroyAllEnemies();  // remove any remaining enemies
            EntityManager.DestroyPowerups();    // clear any remaining power-ups

            // Reset player position and state
            Playership.Instance.Position = ScreenSize / 2;
            Playership.Instance.powerup = PowerupType.Null;
            Playership.Instance.powerupTimeRemaining = 0;
            Playership.Instance.speedRemaining = 0;
            // Cancel any invincibility or force field effects
            Playership.Instance.Invincibility = false;
            // (If ForceFieldActive is implemented, turn it off here as well)
            // Playership.Instance.ForceFieldActive = false;

        
            // Reset scrolling and offset for a fresh start
            scrollOffset = 0;
            scrolling = false;

            // Remove the Restart button from UI to avoid it appearing during gameplay
            gameComponents.RemoveAll(btn => btn.Text == "Restart");

            // Switch back to gameplay state
            gameState = GameStates.Game;
        }



        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            
            int num_tiles_per_row = 4;
            int pixel_tilesize = 40;

            
            foreach(var item in bg)
            {
                Vector2 mapPos = item.Key;
                int tileIndex = item.Value;

                Vector2 drawPos = new Vector2(
                    mapPos.X * tileWidth - scrollOffset,
                    mapPos.Y * tileHeight
                    );

                int x = tileIndex % num_tiles_per_row;
                int y = tileIndex / num_tiles_per_row;

                Rectangle src = new(
                    x * pixel_tilesize,
                    y * pixel_tilesize,
                    pixel_tilesize,
                    pixel_tilesize
                    );

                if(drawPos.X + tileWidth > 0 && drawPos.X < ScreenSize.X)
                {
                    spriteBatch.Draw(textureAtlas, drawPos, src, Color.White);
                }
                

                Vector2 drawPos2 = new Vector2(
                    mapPos.X * tileWidth + mapWidthInPixels - scrollOffset,
                    mapPos.Y * tileHeight
                    );

                if (drawPos2.X + tileWidth > 0 && drawPos2.X < ScreenSize.X)
                {
                    spriteBatch.Draw(textureAtlas, drawPos2, src, Color.White);
                }
                    

            }

            


            

            spriteBatch.End();

            if(gameState == GameStates.Menu) {
                spriteBatch.Begin();
                // Draw game title in the top banner
                string title = "The Sharp Sea";
                Vector2 titleSize = Art.WaveFont.MeasureString(title);
                Vector2 titlePosition = new Vector2(ScreenSize.X / 2 - titleSize.X / 2, 50);
                spriteBatch.DrawString(Art.WaveFont, title, titlePosition, Color.Black);
                //Buttons
                foreach (var Button in gameComponents)
                {
                    Button.Draw(gameTime, spriteBatch);
                }
                spriteBatch.End();

            }

            else if (gameState == GameStates.GameOver)
{
            spriteBatch.Begin();
                // Display Game Over text
                string overText = $"GAME OVER\nWave Reached: {Wave.wave}\nHigh Score: {PlayerStatus.HighScore}";
                Vector2 textSize = Art.Font.MeasureString(overText);
                Vector2 textPosition = new Vector2(ScreenSize.X / 2 - textSize.X / 2, ScreenSize.Y / 2 - textSize.Y / 2 - 40);
                spriteBatch.DrawString(Art.Font, overText, textPosition, Color.White);
                // Draw Game Over buttons (Restart, Quit)
                foreach (var Button in gameComponents)
    {
            Button.Draw(gameTime, spriteBatch);
    }
            spriteBatch.End();
}

            else 
            {

                if (gameState == GameStates.Paused)
                {

                    spriteBatch.Begin();
                    foreach (var Button in gameComponents)
                    {
                        Button.Draw(gameTime, spriteBatch);
                    }
                    spriteBatch.End();
                }
                spriteBatch.Begin(SpriteSortMode.Texture);

                if(!scrolling)
                {
                    string text = "WAVE " + Wave.wave;
                    Vector2 textSize = Art.Font.MeasureString(text);
                    Vector2 textPos;
                    textPos.X = (int)ScreenSize.X / 2 - textSize.X - 140;
                    textPos.Y = 50;
                    spriteBatch.DrawString(Art.WaveFont, text, textPos, Color.Black);


                }


                spriteBatch.DrawString(Art.Font, "Lives: " + PlayerStatus.Lives, new Vector2(5), Color.White);
                DrawRightAlignedString("Score: " + PlayerStatus.Score, 5);
                DrawRightAlignedString("Multiplier: " + PlayerStatus.Multiplier, 35);
                DrawLeftAlignedString("Enemies: " + Wave.numEnemies, 25);

                const int chainLinkY = 100;

                DrawLeftAlignedString("CHAIN: " + PlayerStatus.Chain, chainLinkY);

                if (PlayerStatus.Speed1)
                {
                    DrawLeftAlignedString("+SPEED", chainLinkY + 35);
                }
                if (PlayerStatus.FiringSpeed1)
                {
                    DrawLeftAlignedString("+FIRING SPEED", chainLinkY + 70);
                }
                if (PlayerStatus.ExplosiveBullets)
                {
                    DrawLeftAlignedString("+EXPLOSIVE BULLETS", chainLinkY + 105);
                }

                if (PlayerStatus.IsGameOver)
                {
                    string text = "Game Over\n" +
                        "Your Score: " + PlayerStatus.Score + "\n" +
                        "High Score: " + PlayerStatus.HighScore;

                    Vector2 textSize = Art.Font.MeasureString(text);
                    spriteBatch.DrawString(Art.Font, text, ScreenSize / 2 - textSize / 2, Color.White);
                }

                spriteBatch.Draw(Art.Pointer, Input.MousePosition, Color.White);
                spriteBatch.End();

                spriteBatch.Begin(SpriteSortMode.BackToFront);
                EntityManager.Draw(spriteBatch);
                
                spriteBatch.End();


                base.Draw(gameTime);
            }
            

            
        }

        private void DrawRightAlignedString(string text, float y)
        {
            var textWidth = Art.Font.MeasureString(text).X;
            spriteBatch.DrawString(Art.Font, text, new Vector2(ScreenSize.X - textWidth - 5, y), Color.White);
        }

        private void DrawLeftAlignedString(string text, float y) {

            
            spriteBatch.DrawString(Art.Font, text, new Vector2( 5, y), Color.White);

        }
    }
}
