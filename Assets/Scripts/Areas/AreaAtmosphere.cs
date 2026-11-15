using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Per-area lighting for URP: sun angle and colour, procedural sky, three-colour ambient,
// distance haze and a post-processing profile (tonemapping, grading, bloom, vignette).
// AreaDef still supplies the sun colour/intensity and fog density; the theme supplies the mood.
public static class AreaAtmosphere
{
    struct Preset
    {
        public float sunPitch, sunYaw, sunScale;
        public Color skyTint, skyGround;
        public float skyThickness, skyExposure;
        public Color ambientSky, ambientEquator, ambientGround;
        public Color fogColor;
        public float hazeDensity;
        public float exposure, contrast, saturation, temperature, tint;
        public Color filter;
        public float bloom, bloomThreshold, vignette;
    }

    static Preset For(AreaTheme theme)
    {
        switch (theme)
        {
            case AreaTheme.Forest:
                return new Preset
                {
                    sunPitch = 32f, sunYaw = -50f, sunScale = 1.0f,
                    skyTint = new Color(0.45f, 0.62f, 0.55f), skyGround = new Color(0.25f, 0.3f, 0.22f), skyThickness = 1.1f, skyExposure = 1.1f,
                    ambientSky = new Color(0.42f, 0.55f, 0.5f), ambientEquator = new Color(0.33f, 0.42f, 0.3f), ambientGround = new Color(0.16f, 0.2f, 0.11f),
                    fogColor = new Color(0.55f, 0.68f, 0.6f), hazeDensity = 0.006f,
                    exposure = 0.15f, contrast = 14f, saturation = 6f, temperature = 4f, tint = -4f, filter = new Color(1f, 1f, 0.94f),
                    bloom = 0.7f, bloomThreshold = 1.0f, vignette = 0.28f
                };
            case AreaTheme.Desert:
                return new Preset
                {
                    sunPitch = 55f, sunYaw = -60f, sunScale = 1.15f,
                    skyTint = new Color(0.62f, 0.6f, 0.55f), skyGround = new Color(0.55f, 0.45f, 0.32f), skyThickness = 1.4f, skyExposure = 1.25f,
                    ambientSky = new Color(0.68f, 0.66f, 0.62f), ambientEquator = new Color(0.66f, 0.56f, 0.42f), ambientGround = new Color(0.5f, 0.38f, 0.24f),
                    fogColor = new Color(0.92f, 0.82f, 0.64f), hazeDensity = 0.002f,
                    exposure = 0.1f, contrast = 18f, saturation = 8f, temperature = 14f, tint = 0f, filter = new Color(1f, 0.97f, 0.92f),
                    bloom = 0.5f, bloomThreshold = 1.1f, vignette = 0.22f
                };
            case AreaTheme.Snow:
                return new Preset
                {
                    sunPitch = 24f, sunYaw = -35f, sunScale = 1.0f,
                    skyTint = new Color(0.6f, 0.7f, 0.85f), skyGround = new Color(0.7f, 0.74f, 0.8f), skyThickness = 0.9f, skyExposure = 1.2f,
                    ambientSky = new Color(0.62f, 0.7f, 0.85f), ambientEquator = new Color(0.58f, 0.63f, 0.72f), ambientGround = new Color(0.62f, 0.66f, 0.72f),
                    fogColor = new Color(0.8f, 0.86f, 0.95f), hazeDensity = 0.008f,
                    exposure = 0.0f, contrast = 10f, saturation = -6f, temperature = -12f, tint = 2f, filter = new Color(0.96f, 0.98f, 1f),
                    bloom = 0.6f, bloomThreshold = 1.05f, vignette = 0.22f
                };
            case AreaTheme.Volcano:
                return new Preset
                {
                    sunPitch = 18f, sunYaw = -55f, sunScale = 0.9f,
                    skyTint = new Color(0.55f, 0.22f, 0.16f), skyGround = new Color(0.14f, 0.07f, 0.05f), skyThickness = 2.6f, skyExposure = 0.65f,
                    ambientSky = new Color(0.36f, 0.2f, 0.17f), ambientEquator = new Color(0.32f, 0.17f, 0.12f), ambientGround = new Color(0.4f, 0.15f, 0.06f),
                    fogColor = new Color(0.32f, 0.14f, 0.1f), hazeDensity = 0.012f,
                    exposure = 0.1f, contrast = 22f, saturation = 10f, temperature = 10f, tint = 4f, filter = new Color(1f, 0.93f, 0.88f),
                    bloom = 1.2f, bloomThreshold = 0.9f, vignette = 0.32f
                };
            default: // Town
                return new Preset
                {
                    sunPitch = 38f, sunYaw = -35f, sunScale = 1.1f,
                    skyTint = new Color(0.5f, 0.6f, 0.75f), skyGround = new Color(0.37f, 0.35f, 0.34f), skyThickness = 1.0f, skyExposure = 1.2f,
                    ambientSky = new Color(0.52f, 0.6f, 0.72f), ambientEquator = new Color(0.48f, 0.47f, 0.42f), ambientGround = new Color(0.28f, 0.25f, 0.2f),
                    fogColor = new Color(0.68f, 0.76f, 0.86f), hazeDensity = 0.0015f,
                    exposure = 0.1f, contrast = 12f, saturation = 10f, temperature = 6f, tint = 0f, filter = Color.white,
                    bloom = 0.6f, bloomThreshold = 1.1f, vignette = 0.2f
                };
        }
    }

    const string VolumeName = "AreaPostFX";
    static Material skyTemplate;

    public static void Apply(AreaDef area, bool interior, Camera cam)
    {
        Preset p = For(area.theme);

        // Sun
        Light sun = null;
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional)
            {
                sun = light;
                break;
            }
        }
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(p.sunPitch, p.sunYaw, 0f);
            sun.color = area.lightColor;
            sun.intensity = area.lightIntensity * p.sunScale * (interior ? 0.45f : 1f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = interior ? 0.6f : 0.85f;
            RenderSettings.sun = sun;
        }

        // Sky
        Material sky = null;
        if (!interior)
        {
            if (skyTemplate == null)
            {
                skyTemplate = Resources.Load<Material>("Atmosphere/ProceduralSky");
            }
            if (skyTemplate != null)
            {
                sky = new Material(skyTemplate);
                sky.SetColor("_SkyTint", p.skyTint);
                sky.SetColor("_GroundColor", p.skyGround);
                sky.SetFloat("_AtmosphereThickness", p.skyThickness);
                sky.SetFloat("_Exposure", p.skyExposure);
                sky.SetFloat("_SunSize", 0.035f);
            }
        }
        RenderSettings.skybox = sky;

        // Ambient: sky above, horizon around, bounce from the ground below.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        if (interior)
        {
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.36f, 0.3f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.28f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.15f, 0.12f);
        }
        else
        {
            RenderSettings.ambientSkyColor = p.ambientSky;
            RenderSettings.ambientEquatorColor = p.ambientEquator;
            RenderSettings.ambientGroundColor = p.ambientGround;
        }
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

        // Haze for depth: the area's own fog if it has one, otherwise a light theme haze.
        RenderSettings.fog = !interior;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = Mathf.Max(area.fogDensity, p.hazeDensity);
        RenderSettings.fogColor = p.fogColor;

        DynamicGI.UpdateEnvironment();

        // Post-processing
        GameObject go = GameObject.Find(VolumeName);
        if (go == null)
        {
            go = new GameObject(VolumeName);
        }
        Volume volume = go.GetComponent<Volume>();
        if (volume == null)
        {
            volume = go.AddComponent<Volume>();
        }
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = BuildProfile(p, interior);

        // Camera
        if (cam != null)
        {
            cam.clearFlags = sky != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = interior ? new Color(0.06f, 0.05f, 0.05f) : area.skyColor;
            cam.allowHDR = true;
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.None; // MSAA from the pipeline asset
            }
        }
    }

    static VolumeProfile BuildProfile(Preset p, bool interior)
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

        Tonemapping tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.ACES);

        ColorAdjustments grading = profile.Add<ColorAdjustments>(true);
        grading.postExposure.Override(p.exposure + (interior ? 0.15f : 0f));
        grading.contrast.Override(p.contrast);
        grading.saturation.Override(p.saturation);
        grading.colorFilter.Override(p.filter);
        grading.hueShift.Override(0f);

        WhiteBalance balance = profile.Add<WhiteBalance>(true);
        balance.temperature.Override(interior ? 12f : p.temperature);
        balance.tint.Override(p.tint);

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(interior ? 0.9f : p.bloom);
        bloom.threshold.Override(interior ? 1.0f : p.bloomThreshold);
        bloom.scatter.Override(0.65f);
        bloom.highQualityFiltering.Override(false);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(interior ? 0.35f : p.vignette);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(Color.black);

        return profile;
    }
}
