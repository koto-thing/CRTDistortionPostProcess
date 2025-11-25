using System;
using Minge2025Summer.Scripts.RenderingScript.CRTEffectScript;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CRTDistortionFeature : ScriptableRendererFeature
{
    [Serializable]
    public class Settings
    {
        public Shader m_CRTDistortionShader;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }
    
    public Settings settings = new Settings();

    private CRTDistortionPass m_ScriptablePass;
    private Material m_CRTDistortionMaterial;

    public override void Create()
    {
        if (settings.m_CRTDistortionShader != null)
            m_CRTDistortionMaterial = CoreUtils.CreateEngineMaterial(settings.m_CRTDistortionShader);

        m_ScriptablePass = new CRTDistortionPass(m_CRTDistortionMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_CRTDistortionMaterial == null || m_ScriptablePass == null)
        {
            Debug.LogError("CRTDistortionFeature: Missing material. CRTDistortionPass will not execute. Check for missing reference in the assigned renderer.");
            return;
        }

        var stack = VolumeManager.instance.stack;
        var customVolume = stack.GetComponent<CRTDistortionVolume>();

        if (customVolume != null && customVolume.IsActive())
        {
            m_ScriptablePass.Setup(customVolume);
            m_ScriptablePass.renderPassEvent = settings.renderPassEvent;
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_CRTDistortionMaterial);
        m_CRTDistortionMaterial = null;
    }
}