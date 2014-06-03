using System;
using _4DMonoEngine.Core.Common.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Assets
{
    public class AssetManager : GameComponent
    {
        public Model AimedBlockModel { get; private set; }
        public Model SampleModel { get; private set; }
        public Model SkyDomeModel { get; private set; }

        public Effect BlockEffect { get; private set; }
        public BasicEffect AimedBlockEffect { get; private set; }
        public Effect GaussianBlurEffect { get; private set; }
        public Effect SkyDomeEffect { get; private set; }
        public Effect PerlinNoiseEffect { get; private set; }

        public Texture2D BlockTextureAtlas { get; private set; }
        public Texture2D CrackTextureAtlas { get; private set; }
        public Texture2D AimedBlockTexture { get; private set; }
        public Texture2D CrossHairNormalTexture { get; private set; }
        public Texture2D CrossHairShovelTexture { get; private set; }
        public Texture2D CloudMapTexture { get; private set; }
        public Texture2D StarMapTexture { get; private set; }
        public Texture2D CloudTexture { get; private set; }

        public SpriteFont Verdana { get; private set; }

        private const string EffectShaderExtension = ".mgfxo"; 

        private static readonly Logger Logger = LogManager.GetOrCreateLogger(); // the logger.

        public AssetManager(Game game)
            : base(game)
        {
            Game.Services.AddService(typeof(AssetManager), this); // export service.   
        }

        public override void Initialize()
        {
            LoadContent();
            base.Initialize();
        }

       public void LoadContent()
        {
            try
            {
                AimedBlockModel = Game.Content.Load<Model>(@"Models/AimedBlock");
                SampleModel = Game.Content.Load<Model>(@"Models/Mii");
                SkyDomeModel = Game.Content.Load<Model>(@"Models/SkyDome");

                BlockEffect = LoadEffectShader(@"Effects/BlockEffect");
                AimedBlockEffect = new BasicEffect(Game.GraphicsDevice);
                GaussianBlurEffect = LoadEffectShader(@"Effects/PostProcessing/Bloom/GaussianBlur");
                SkyDomeEffect = LoadEffectShader(@"Effects/SkyDome");
                PerlinNoiseEffect = LoadEffectShader(@"Effects/PerlinNoise");

                BlockTextureAtlas = Game.Content.Load<Texture2D>(@"Textures/terrain");
                CrackTextureAtlas = Game.Content.Load<Texture2D>(@"Textures/cracks");
                AimedBlockTexture = Game.Content.Load<Texture2D>(@"Textures/AimedBlock");
                CrossHairNormalTexture = Game.Content.Load<Texture2D>(@"Textures/Crosshairs/Normal");
                CrossHairShovelTexture = Game.Content.Load<Texture2D>(@"Textures/Crosshairs/Shovel");
                CloudMapTexture = Game.Content.Load<Texture2D>(@"Textures/cloudmap");
                StarMapTexture = Game.Content.Load<Texture2D>(@"Textures/starmap");
                CloudTexture = Game.Content.Load<Texture2D>(@"Textures/cloud-texture");

                Verdana = Game.Content.Load<SpriteFont>(@"Fonts/Verdana");
            }
            catch(Exception e)
            {
                Logger.Fatal(e, "Error while loading assets!");
                Console.ReadLine();
                Environment.Exit(-1);
            }
        }

        private Effect LoadEffectShader(string path)
        {
            // Note that monogame requires special compiled shaders with mgfxo extension.
            return Game.Content.Load<Effect>(path + EffectShaderExtension);
        }
    }
}