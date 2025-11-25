using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Minge2025Summer.Scripts.RenderingScript.CRTEffectScript
{
    [Serializable, VolumeComponentMenu("CRTDistortionVolume")]
    public class CRTDistortionVolume : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enableDistortion = new BoolParameter(true);
        public ClampedFloatParameter distortionStrength = new ClampedFloatParameter(0.1f, 0.0f, 0.5f);

        public BoolParameter enableScanlinesAndPhosphor = new BoolParameter(true);
        public ClampedFloatParameter scanlineDensity = new ClampedFloatParameter(400.0f, 0.0f, 1000.0f);
        public ClampedFloatParameter scanlineStrength = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public ClampedFloatParameter phosphorStrength = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);

        public BoolParameter enableChromaticAberration = new BoolParameter(true);
        public ClampedFloatParameter chromaticAberrationStrength = new ClampedFloatParameter(10.0f, 0.0f, 50.0f);
        public BoolParameter enableBloom = new BoolParameter(true);
        public ClampedFloatParameter bloomStrength = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);
        
        public BoolParameter enableNoiseAndFlicker = new  BoolParameter(true);
        public ClampedFloatParameter noiseStrength = new ClampedFloatParameter(0.05f, 0.0f, 0.2f);
        public ClampedFloatParameter flickerStrength = new ClampedFloatParameter(0.08f, 0.0f, 0.5f);

        public BoolParameter enableVsyncGlitch = new BoolParameter(true);
        public ClampedFloatParameter vsyncGlitchStrength = new ClampedFloatParameter(20.0f, 0.0f, 100.0f);
        public ClampedFloatParameter vsyncGlitchSpeed = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);
        public ClampedFloatParameter vsyncGlitchBarHeight = new ClampedFloatParameter(0.1f, 0.01f, 0.5f);

        public bool IsActive()
        {
            bool pass0Active = enableDistortion.value || enableChromaticAberration.value;
            bool pass1Active = enableScanlinesAndPhosphor.value || enableBloom.value || enableVsyncGlitch.value;
            
            return pass0Active || pass1Active;
        }
        
        public bool IsTileCompatible() => false;
    }
}