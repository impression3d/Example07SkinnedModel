using Impression;
using Impression.Graphics;
using Impression.Input;
using Impression.Animations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Example07SkinnedModel
{
    public class Example07SkinnedModelGame : Impression.Game
    {
		GraphicsDeviceManager graphics;

		Camera camera;

		Model model;
        SkinnedAnimation skinnedAnimation;

		List<AnimationClip> clips;
		int currentClipIndex;

		Matrix modelTransform;
		float rotationSpeed = 1;

		MouseState currentMouseState;
		MouseState lastMouseState;

        public Example07SkinnedModelGame()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);

			switch (FrameworkContext.Platform)
			{
				case PlatformType.Windows:
				case PlatformType.Mac:
				case PlatformType.Linux:
					{
						graphics.PreferredBackBufferWidth = 1280;
						graphics.PreferredBackBufferHeight = 720;

						this.IsMouseVisible = true;
					}
					break;
                case PlatformType.WindowsStore:
                case PlatformType.WindowsMobile:
					{
						graphics.PreferredBackBufferWidth = 1280;
						graphics.PreferredBackBufferHeight = 720;

						graphics.IsFullScreen = true;

						// Frame rate is 30 fps by default for mobile device
						TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 30L);
					}
					break;
				case PlatformType.Android:
				case PlatformType.iOS:
					{
						graphics.PreferredBackBufferWidth = 1280;
						graphics.PreferredBackBufferHeight = 720;

						graphics.IsFullScreen = true;

						// Frame rate is 30 fps by default for mobile device
						TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 30L);
					}
					break;
			}

            this.View.Title = "Example07SkinnedModel";
        }

        protected override void Initialize()
        { 
            base.Initialize();

            camera = new Camera(graphics.GraphicsDevice.Viewport);
			camera.Transform = Matrix.CreateTranslation(0, .5f, 3);
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            model = this.Content.Load<Model>("Models/Karakter/Karakter");

            skinnedAnimation = new SkinnedAnimation(model);
            skinnedAnimation.IsBindPosed = false;
            skinnedAnimation.Animation.LoopType = LoopType.Loop;

			clips = new List<AnimationClip>();

            // Load clips
            clips.Add(this.Content.Load<AnimationClip>("Models/Karakter/idle@Karakter"));
            clips.Add(this.Content.Load<AnimationClip>("Models/Karakter/climbing-up@Karakter"));
            clips.Add(this.Content.Load<AnimationClip>("Models/Karakter/climbing-left@Karakter"));
            clips.Add(this.Content.Load<AnimationClip>("Models/Karakter/climbing-right@Karakter"));
            clips.Add(this.Content.Load<AnimationClip>("Models/Karakter/walk@Karakter"));
            clips.Add(this.Content.Load<AnimationClip>("Models/Karakter/run@Karakter"));

            // Add clips to animation
            clips.ForEach((clip) => skinnedAnimation.Animation.AddClip(clip, clip.Name));
        }

        protected override void UnloadContent()
        {
            // do something

            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
			switch (FrameworkContext.Platform)
			{
				case PlatformType.Windows:
				case PlatformType.Mac:
				case PlatformType.Linux:
					{
						if (Keyboard.GetState().IsKeyDown(Keys.Escape))
							this.Exit();
					}
					break;
				default:
					{
						// do nothings
					}
					break;
			}

			lastMouseState = currentMouseState;
			currentMouseState = Mouse.GetState();

			// Allow clips to sequenced when left button was just pressed
			if (lastMouseState.LeftButton == ButtonState.Released &&
				currentMouseState.LeftButton == ButtonState.Pressed)
			{
				currentClipIndex++;

				// When current clip index is out of range, reset index to zero
				if (!(currentClipIndex < clips.Count))
				{
					currentClipIndex = 0;
				}

				// Finally play selected clip
                skinnedAnimation.Animation.Play(clips[currentClipIndex].Name);
			}

            skinnedAnimation.Update(gameTime);

            modelTransform = Matrix.CreateScale(Vector3.One) * 
                             Matrix.CreateRotationY((float)gameTime.TotalGameTime.TotalSeconds * rotationSpeed) *
							 Matrix.CreateTranslation(Vector3.Zero);
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
			graphics.GraphicsDevice.Clear(Color.BurlyWood);

			// Update the bone transforms if skinned material effect issupported
			foreach (ModelMesh mesh in model.Meshes)
			{
                foreach (var material in mesh.Materials)
				{
					SkinnedEffect effect = material.Effect as SkinnedEffect;

                    if (effect == null)
                        continue;

                    effect.SetBoneTransforms(skinnedAnimation.SkinTransforms);
				}
			}

            // Draw model
            model.Draw(modelTransform, camera.View, camera.Projection);
            
            base.Draw(gameTime);
        }
    }
}