using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AWSIM.Loader.SimulationCore
{
    public enum ShadowQuality
    {
        Off = 0,
        Low = 512,
        Medium = 1024,
        High = 2048,
        VeryHigh = 4096
    }

    public enum TextureQuality
    {
        Quarter = 0,
        Half = 1,
        Full = 2
    }

    public enum AntiAliasingMode
    {
        None = 0,
        FXAA = 1,
        SMAA = 2,
        TAA = 3
    }

    public enum AntiAliasingQuality
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum AnisotropicFiltering
    {
        Off = 0,
        x2 = 2,
        x4 = 4,
        x8 = 8
    }

    public enum PostProcessingLevel
    {
        Off = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum ViewDistance
    {
        Low = 256,
        Medium = 512,
        High = 1024,
        VeryHigh = 2048,
        Ultra = 4096
    }

    public class GraphicSettings
    {
        public ShadowQuality ShadowQuality;
        public TextureQuality TextureQuality;
        public AntiAliasingMode AntiAliasingMode;
        public AntiAliasingQuality AntiAliasingQuality;
        public AnisotropicFiltering AnisotropicFiltering;
        public PostProcessingLevel PostProcessingLevel;
        public ViewDistance ViewDistance;

        public bool UseShadows;
        public bool AmbientOcclusion;
        public bool UseAntiAliasing;
        public bool UsePostProcessing;
        public bool WindowedMode;
        public bool Vsync;
        public bool MotionBlur;
        public bool DepthOfField;

        public int FrameRateLimit;

        [Range(0.1f, 2.0f)] public float RenderScale;
        // TODO: Add more settings (mozzz)
        // effects quality      // not used atm
        // particles quality    // not used atm
        // volumetric fog       // doesnt exist in urp, will have to add it
        // weather effects      // not used atm
        // etc.

        public GraphicSettings()
        {
            // Load settings from PlayerPrefs or use defaults
            LoadSettings();
        }

        public void ApplySettings()
        {
            // Apply Shadow Settings
            if (UseShadows)
            {
                QualitySettings.shadows = UnityEngine.ShadowQuality.All; // Enable shadows
                QualitySettings.shadowResolution =
                    (UnityEngine.ShadowResolution)
                    ShadowQuality; // Map your custom ShadowQuality enum to Unity's ShadowResolution
            }
            else
            {
                QualitySettings.shadows = UnityEngine.ShadowQuality.Disable; // Disable shadows
            }

            // Apply VSync and FrameRate
            QualitySettings.vSyncCount = Vsync ? 1 : 0;
            Application.targetFrameRate = FrameRateLimit > 0 ? FrameRateLimit : -1;

            // Apply Anti-Aliasing
            QualitySettings.antiAliasing = UseAntiAliasing ? (int)AntiAliasingQuality * 2 : 0;

            // Apply Texture Quality
            QualitySettings.globalTextureMipmapLimit =
                (int)(2 - TextureQuality); // In Unity, lower number = higher quality

            // Apply Anisotropic Filtering
            QualitySettings.anisotropicFiltering = (UnityEngine.AnisotropicFiltering)AnisotropicFiltering;

            // Apply Render Scale
            UniversalRenderPipeline.asset.renderScale = RenderScale;

            // Apply Windowed Mode
            Screen.fullScreen = !WindowedMode;

            // Apply other effects (Post-Processing, Ambient Occlusion, Motion Blur, etc.)
            // Assuming you have control over PostProcessing through a PostProcessingVolume
            // Add custom code to manage the effects in URP if you use them (e.g., URP features like DepthOfField)

            // Save settings after applying
            SaveSettings();
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetInt("ShadowQuality", (int)ShadowQuality);
            PlayerPrefs.SetInt("TextureQuality", (int)TextureQuality);
            PlayerPrefs.SetInt("AntiAliasingType", (int)AntiAliasingMode);
            PlayerPrefs.SetInt("AntiAliasingQuality", (int)AntiAliasingQuality);
            PlayerPrefs.SetInt("AnisotropicFiltering", (int)AnisotropicFiltering);
            PlayerPrefs.SetInt("PostProcessingLevel", (int)PostProcessingLevel);
            PlayerPrefs.SetInt("ViewDistance", (int)ViewDistance);

            PlayerPrefs.SetInt("UseShadows", UseShadows ? 1 : 0);
            PlayerPrefs.SetInt("AmbientOcclusion", AmbientOcclusion ? 1 : 0);
            PlayerPrefs.SetInt("UseAntiAliasing", UseAntiAliasing ? 1 : 0);
            PlayerPrefs.SetInt("UsePostProcessing", UsePostProcessing ? 1 : 0);
            PlayerPrefs.SetInt("WindowedMode", WindowedMode ? 1 : 0);
            PlayerPrefs.SetInt("Vsync", Vsync ? 1 : 0);
            PlayerPrefs.SetInt("MotionBlur", MotionBlur ? 1 : 0);
            PlayerPrefs.SetInt("DepthOfField", DepthOfField ? 1 : 0);

            PlayerPrefs.SetInt("FrameRateLimit", FrameRateLimit);
            PlayerPrefs.SetFloat("RenderScale", RenderScale);

            PlayerPrefs.Save();
        }

        public void LoadSettings()
        {
            ShadowQuality = (ShadowQuality)PlayerPrefs.GetInt("ShadowQuality", (int)ShadowQuality.High);
            TextureQuality = (TextureQuality)PlayerPrefs.GetInt("TextureQuality", (int)TextureQuality.Full);
            AntiAliasingMode = (AntiAliasingMode)PlayerPrefs.GetInt("AntiAliasingType", (int)AntiAliasingMode.TAA);
            AntiAliasingQuality =
                (AntiAliasingQuality)PlayerPrefs.GetInt("AntiAliasingQuality", (int)AntiAliasingQuality.Medium);
            AnisotropicFiltering =
                (AnisotropicFiltering)PlayerPrefs.GetInt("AnisotropicFiltering", (int)AnisotropicFiltering.x4);
            PostProcessingLevel =
                (PostProcessingLevel)PlayerPrefs.GetInt("PostProcessingLevel", (int)PostProcessingLevel.Medium);
            ViewDistance = (ViewDistance)PlayerPrefs.GetInt("ViewDistance", (int)ViewDistance.High);

            UseShadows = PlayerPrefs.GetInt("UseShadows", 1) == 1;
            AmbientOcclusion = PlayerPrefs.GetInt("AmbientOcclusion", 0) == 1;
            UseAntiAliasing = PlayerPrefs.GetInt("UseAntiAliasing", 1) == 1;
            UsePostProcessing = PlayerPrefs.GetInt("UsePostProcessing", 1) == 1;
            WindowedMode = PlayerPrefs.GetInt("WindowedMode", 0) == 1;
            Vsync = PlayerPrefs.GetInt("Vsync", 1) == 1;
            MotionBlur = PlayerPrefs.GetInt("MotionBlur", 0) == 1;
            DepthOfField = PlayerPrefs.GetInt("DepthOfField", 0) == 1;

            FrameRateLimit = PlayerPrefs.GetInt("FrameRateLimit", 60);
            RenderScale = PlayerPrefs.GetFloat("RenderScale", 1.0f);
        }

        public void ResetToDefaults()
        {
            // Reset all settings to default values
            ShadowQuality = ShadowQuality.Medium;
            TextureQuality = TextureQuality.Full;
            AntiAliasingMode = AntiAliasingMode.TAA;
            AntiAliasingQuality = AntiAliasingQuality.Medium;
            AnisotropicFiltering = AnisotropicFiltering.x4;
            PostProcessingLevel = PostProcessingLevel.Medium;
            ViewDistance = ViewDistance.High;

            UseShadows = true;
            AmbientOcclusion = false;
            UseAntiAliasing = true;
            UsePostProcessing = true;
            WindowedMode = false;
            Vsync = true;
            MotionBlur = false;
            DepthOfField = false;

            FrameRateLimit = 60;
            RenderScale = 1.0f;

            // Save defaults
            SaveSettings();
        }
    }
}
