using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameProject.Models;

namespace GameProject
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Burger burger;
        List<TeddyBear> bears = new List<TeddyBear>();
        static List<Projectile> projectiles = new List<Projectile>();
        List<Explosion> explosions = new List<Explosion>();

        // projectile and explosion sprites. Saved so they don't have to
        // be loaded every time projectiles or explosions are created
        static Texture2D frenchFriesSprite;
        static Texture2D teddyBearProjectileSprite;
        static Texture2D explosionSpriteStrip;

        // scoring support
        int scores = 0;
        ScoreManager scoreManager;

        // health support
        string healthString = GameConstants.HealthPrefix +
            GameConstants.BurgerInitialHealth;
        bool burgerDead = false;

        // text display support
        SpriteFont font;
        Vector2 scorePosition = new Vector2(GameConstants.WindowWidth / 12, GameConstants.WindowHeight / 12);

        // sound effects
        SoundEffect burgerDamage;
        SoundEffect burgerDeath;
        SoundEffect burgerShot;
        SoundEffect explosion;
        SoundEffect teddyBounce;
        SoundEffect teddyShot;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // set resolution
            graphics.PreferredBackBufferWidth = GameConstants.WindowWidth;
            graphics.PreferredBackBufferHeight = GameConstants.WindowHeight;
            IsMouseVisible = true;


        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            RandomNumberGenerator.Initialize();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
       ////////////////////////////////////////////////     ((     LOAD CONTENT HERE       ))        ////////////////////////////////////////////////
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.5
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // load audio content
            burgerDamage = Content.Load<SoundEffect>("sounds/BurgerDamage");
            burgerDeath = Content.Load<SoundEffect>("sounds/BurgerDeath");
            burgerShot = Content.Load<SoundEffect>("sounds/BurgerShot");
            explosion = Content.Load<SoundEffect>("sounds/Explosion");
            teddyBounce = Content.Load<SoundEffect>("sounds/TeddyBounce");
            teddyShot = Content.Load<SoundEffect>("sounds/TeddyShot");


            // load sprite font
            font = Content.Load<SpriteFont>("Arial");
            scoreManager = ScoreManager.Load();

            // load projectile and explosion sprites
            teddyBearProjectileSprite = Content.Load<Texture2D>(@"graphics\teddybearprojectile");
            frenchFriesSprite = Content.Load<Texture2D>(@"graphics\frenchfries");
            explosionSpriteStrip = Content.Load<Texture2D>(@"graphics\explosion");

            // add initial game objects
            burger = new Burger(Content, @"graphics\burger", (GameConstants.WindowWidth / 2), (GameConstants.WindowHeight * 7 / 8), burgerShot);

            for (int i = 0; i < GameConstants.MaxBears; i++)
            {
                SpawnBear();
            }

            // set initial health and score strings
            healthString = GameConstants.HealthPrefix + burger.Health;

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {

        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // get current mouse state and update burger
            //MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();
            burger.Update(gameTime, keyboard);

            // update other game objects
            foreach (TeddyBear bear in bears)
            {
                bear.Update(gameTime);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Update(gameTime);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Update(gameTime);
            }

            // check and resolve collisions between teddy bears
            for (int i = 0; i < bears.Count; i++)

                for (int j = i + 1; j < bears.Count; j++)
                {
                    if (bears[i].Active && bears[j].Active)
                    {
                        CollisionResolutionInfo collisionInfo = CollisionUtils.CheckCollision(gameTime.ElapsedGameTime.Milliseconds, GameConstants.WindowWidth,
                            GameConstants.WindowHeight, bears[i].Velocity, bears[i].DrawRectangle, bears[j].Velocity, bears[j].DrawRectangle);
                        if (!(collisionInfo == null))
                        {
                            if (collisionInfo.FirstOutOfBounds)
                            {
                                bears[i].Active = false;
                            }
                            else
                            {
                                bears[i].Velocity = collisionInfo.FirstVelocity;
                                bears[i].DrawRectangle = collisionInfo.FirstDrawRectangle;
                            }
                            if (collisionInfo.SecondOutOfBounds)
                            {
                                bears[j].Active = false;
                            }
                            else
                            {
                                bears[j].Velocity = collisionInfo.SecondVelocity;
                                bears[j].DrawRectangle = collisionInfo.SecondDrawRectangle;
                            }
                            teddyBounce.Play();
                        }
                    }
                }

            // check and resolve collisions between burger and teddy bears
            foreach (TeddyBear Bear in bears)
            {
                if (Bear.CollisionRectangle.Intersects(burger.CollisionRectangle))
                {
                    burger.Health -= GameConstants.BearDamage;
                    healthString = GameConstants.HealthPrefix + burger.Health;
                    CheckBurgerKill();
                    Bear.Active = false;
                    explosions.Add(new Explosion(explosionSpriteStrip, Bear.Location.X, Bear.Location.Y, explosion));
                    burgerDamage.Play();


                }
            }

            // check and resolve collisions between burger and projectiles


            foreach (Projectile p in projectiles)
            {
                if (p.Type == ProjectileType.TeddyBear && p.CollisionRectangle.Intersects(burger.CollisionRectangle))
                {
                    burger.Health -= GameConstants.TeddyBearProjectileDamage;
                    healthString = GameConstants.HealthPrefix + burger.Health;
                    CheckBurgerKill();
                    p.Active = false;
                    burgerDamage.Play();
                }
            }


            // check and resolve collisions between teddy bears and projectiles
            foreach (TeddyBear teddy in bears)
            {
                foreach (Projectile projectile in projectiles)
                {
                    if (teddy.CollisionRectangle.Intersects(projectile.CollisionRectangle) && projectile.Type == ProjectileType.FrenchFries)
                    {
                        teddy.Active = false;
                        projectile.Active = false;
                        explosions.Add(new Explosion(explosionSpriteStrip, teddy.Location.X, teddy.Location.Y, explosion));
                        scores += GameConstants.BearPoints;
                    }
                }
            }




            // clean out inactive teddy bears and add new ones as necessary
            for (int i = bears.Count - 1; i >= 0; i--)
            {
                if (!bears[i].Active)
                {
                    bears.RemoveAt(i);
                }
            }


            // clean out inactive projectiles
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                if (!projectiles[i].Active)
                {
                    projectiles.RemoveAt(i);
                }
            }

            // clean out finished explosions
            for (int i = explosions.Count - 1; i >= 0; i--)
            {
                if (explosions[i].Finished)
                {
                    explosions.RemoveAt(i);
                }
            }

            for (int i = bears.Count; i < GameConstants.MaxBears; i++)
            {
                SpawnBear();
            }

            base.Update(gameTime);

        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            // draw game objects
            burger.Draw(spriteBatch);

            foreach (TeddyBear bear in bears)
            {
                bear.Draw(spriteBatch);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Draw(spriteBatch);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Draw(spriteBatch);
            }


            // draw score and health
            spriteBatch.DrawString(font, "Score: " + scores, new Vector2(10, 10), Color.White);
            spriteBatch.DrawString(font, "Highscores:\n" + string.Join("\n", scoreManager.Highscores.Select(c => c.PlayerName + ": " + c.Value).ToArray()), new Vector2(680, 10), Color.Red);
            spriteBatch.DrawString(font, healthString, GameConstants.HealthLocation, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Public methods

        /// <summary>
        /// Gets the projectile sprite for the given projectile type
        /// </summary>
        /// <param name="type">the projectile type</param>
        /// <returns>the projectile sprite for the type</returns>

        //////////////////////////////////////////////          ((((    GET PROJECTILE SPRITE   HERE    ))))            /////////////////////////////////////////////////
        public static Texture2D GetProjectileSprite(ProjectileType type)
        {
            // replace with code to return correct projectile sprite based on projectile type
            if (type == ProjectileType.FrenchFries)
            {
                return frenchFriesSprite;
            }
            else
            {
                return teddyBearProjectileSprite;
            }

        }

        /// <summary>
        /// Adds the given projectile to the game
        /// </summary>
        /// <param name="projectile">the projectile to add</param>
        public static void AddProjectile(Projectile projectile)
        {
            projectiles.Add(projectile);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Spawns a new teddy bear at a random location
        /// </summary>
        private void SpawnBear()
        {
            // generate random location
            int X = GetRandomLocation(GameConstants.SpawnBorderSize, GameConstants.WindowWidth - GameConstants.SpawnBorderSize);
            int Y = GetRandomLocation(GameConstants.SpawnBorderSize, GameConstants.WindowHeight - GameConstants.SpawnBorderSize);

            // generate random velocity
            float angle = RandomNumberGenerator.NextFloat((float)Math.PI * 2);
            float velocity = GameConstants.MinBearSpeed + RandomNumberGenerator.NextFloat(GameConstants.BearSpeedRange);



            //Angle in which the bear's velocity travels
            angle = RandomNumberGenerator.NextFloat((float)Math.PI);

            //new vector 2 object
            Vector2 bearVelocity;
            bearVelocity.X = (float)Math.Cos(angle) * velocity;
            bearVelocity.Y = (float)Math.Sin(angle) * velocity;
            // create new bear
            TeddyBear
                newBear = new TeddyBear(Content, @"graphics\teddybear", X, Y, bearVelocity, teddyBounce, teddyShot);

            // make sure we don't spawn into a collision
            List<Rectangle> collisionRectangles = GetCollisionRectangles();
            while (!CollisionUtils.IsCollisionFree(newBear.CollisionRectangle, collisionRectangles))
            {
                newBear.X = GetRandomLocation(GameConstants.SpawnBorderSize, GameConstants.WindowWidth - 2 * GameConstants.SpawnBorderSize);
                newBear.Y = GetRandomLocation(GameConstants.SpawnBorderSize, GameConstants.WindowHeight - 2 * GameConstants.SpawnBorderSize);
            }

            // add new bear to list
            bears.Add(newBear);
        }

        /// <summary>
        /// Gets a random location using the given min and range
        /// </summary>
        /// <param name="min">the minimum</param>
        /// <param name="range">the range</param>
        /// <returns>the random location</returns>
        private int GetRandomLocation(int min, int range)
        {
            return min + RandomNumberGenerator.Next(range);
        }

        /// <summary>
        /// Gets a list of collision rectangles for all the objects in the game world
        /// </summary>
        /// <returns>the list of collision rectangles</returns>
        private List<Rectangle> GetCollisionRectangles()
        {
            List<Rectangle> collisionRectangles = new List<Rectangle>();
            collisionRectangles.Add(burger.CollisionRectangle);
            foreach (TeddyBear bear in bears)
            {
                collisionRectangles.Add(bear.CollisionRectangle);
            }
            foreach (Projectile projectile in projectiles)
            {
                collisionRectangles.Add(projectile.CollisionRectangle);
            }
            foreach (Explosion explosion in explosions)
            {
                collisionRectangles.Add(explosion.CollisionRectangle);
            }
            return collisionRectangles;
        }

        /// <summary>
        /// Checks to see if the burger has just been killed
        /// </summary>
        private void CheckBurgerKill()
        {
            if (burger.Health == 0 && !burgerDead)
            {
                burgerDeath.Play();
                burgerDead = true;
                scoreManager.Add(new Models.Score()
                    {
                        PlayerName = "Player Joe 1",
                        Value = scores,
                    }
            );
                    ScoreManager.Save(scoreManager);
                }

        }
        /// <summary>
        /// Gets the score string for the Given Score
        /// </summary>

        #endregion
    }
}
