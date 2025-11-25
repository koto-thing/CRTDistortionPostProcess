using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Minge2025Summer.Scripts.RenderingScript.CRTEffectScript
{
    public class CRTDistortionPass : ScriptableRenderPass
    {
        public class PassData0
        {
            internal TextureHandle source;
            internal Material material;
            internal float distortionStrength;
            internal float choromaticAberrationStrength;
        }

        public class PassData1
        {
            internal TextureHandle source;
            internal Material material;
            internal float scanlineDensity;
            internal float scanlineStrength;
            internal float phosphorStrength;
            internal Vector4 screenResolution;
            internal float bloomStrength;
            internal float noiseStrength;
            internal float flickerStrength;
            internal float vsyncGlitchStrength;
            internal float vsyncGlitchSpeed;
            internal float vsyncGlitchBarHeight;
        }
        
        private CRTDistortionVolume m_Volume;
        private readonly Material m_Material;
        private readonly ProfilingSampler m_ProfilingSampler;

        public CRTDistortionPass(Material material)
        {
            m_Material = material;
            m_ProfilingSampler = new ProfilingSampler(nameof(CRTDistortionPass));
        }

        public void Setup(CRTDistortionVolume volume)
        {
            m_Volume = volume;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null || m_Volume == null)
                return;

            // カメラのカラーテクスチャを取得
            var urpResources = frameData.Get<UniversalResourceData>();
            var cameraColor = urpResources.activeColorTexture;

            // 画面解像度を設定
            var camDesc = renderGraph.GetTextureDesc(cameraColor);
            m_Material.SetVector("_ScreenResolution", new Vector4(camDesc.width, camDesc.height, 0.0f, 0.0f));

            // 一時テクスチャを作成
            var tempDesc = camDesc;
            tempDesc.name = "CRTDistortionTempTexture";
            var tempTexture = renderGraph.CreateTexture(tempDesc);
            
            // CRT歪みエフェクトパス
            using (var builder = renderGraph.AddRasterRenderPass<PassData0>("CRTDistortion", out var passData0, m_ProfilingSampler))
            {
                // パスデータの設定
                passData0.material = m_Material;
                passData0.source = cameraColor;
                passData0.distortionStrength = m_Volume.distortionStrength.value;
                passData0.choromaticAberrationStrength = m_Volume.chromaticAberrationStrength.value;
                
                // マテリアルプロパティの設定
                m_Material.SetFloat("_DistortionStrength", passData0.distortionStrength);
                m_Material.SetFloat("_ChromaticAberrationStrength", passData0.choromaticAberrationStrength);
                
                // リソースの使用を宣言
                builder.UseTexture(passData0.source, AccessFlags.Read);
                builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);
                
                // レンダリング関数の設定
                builder.SetRenderFunc((PassData0 data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
            
            using (var builder = renderGraph.AddRasterRenderPass<PassData1>("CRTScanlinesAndPhosphor", out var passData1, m_ProfilingSampler))
            {
                // パスデータの設定
                passData1.material = m_Material;
                passData1.source = tempTexture;
                passData1.scanlineDensity = m_Volume.scanlineDensity.value;
                passData1.scanlineStrength = m_Volume.scanlineStrength.value;
                passData1.phosphorStrength = m_Volume.phosphorStrength.value;
                passData1.screenResolution = new Vector4(camDesc.width, camDesc.height, 0.0f, 0.0f);
                passData1.bloomStrength = m_Volume.bloomStrength.value;
                passData1.noiseStrength = m_Volume.noiseStrength.value;
                passData1.flickerStrength = m_Volume.flickerStrength.value;
                passData1.vsyncGlitchStrength = m_Volume.vsyncGlitchStrength.value;
                passData1.vsyncGlitchSpeed = m_Volume.vsyncGlitchSpeed.value;
                passData1.vsyncGlitchBarHeight = m_Volume.vsyncGlitchBarHeight.value;
                
                // マテリアルプロパティの設定
                m_Material.SetFloat("_ScanlineDensity", passData1.scanlineDensity);
                m_Material.SetFloat("_ScanlineStrength", passData1.scanlineStrength);
                m_Material.SetFloat("_PhosphorStrength", passData1.phosphorStrength);
                m_Material.SetVector("_ScreenResolution", passData1.screenResolution);
                m_Material.SetFloat("_BloomStrength", passData1.bloomStrength);
                m_Material.SetFloat("_NoiseStrength", passData1.noiseStrength);
                m_Material.SetFloat("_FlickerStrength", passData1.flickerStrength);
                m_Material.SetFloat("_VsyncGlitchStrength", passData1.vsyncGlitchStrength);
                m_Material.SetFloat("_VsyncGlitchSpeed", passData1.vsyncGlitchSpeed);
                m_Material.SetFloat("_VsyncGlitchBarHeight", passData1.vsyncGlitchBarHeight);
                
                // リソースの使用を宣言
                builder.UseTexture(passData1.source, AccessFlags.Read);
                builder.SetRenderAttachment(cameraColor, 0, AccessFlags.Write);
                
                // レンダリング関数の設定
                builder.SetRenderFunc((PassData1 data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 1);
                });
            }
        }
    }
}