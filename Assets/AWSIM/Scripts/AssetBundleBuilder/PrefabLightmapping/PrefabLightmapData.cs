using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// Script from: https://github.com/Ayfel/PrefabLightmapping

[ExecuteAlways]
public class PrefabLightmapData : MonoBehaviour
{
    [Tooltip(
        "Reassigns shaders when applying the baked lightmaps. Might conflict with some shaders like transparent HDRP.")]
    public bool releaseShaders = true;

    [System.Serializable]
    private struct RendererInfo
    {
        public Renderer renderer;
        public int lightmapIndex;
        public Vector4 lightmapOffsetScale;
    }

    [System.Serializable]
    private struct LightInfo
    {
        public Light light;
        public int lightmapBaketype;
        public int mixedLightingMode;
    }

    [SerializeField] RendererInfo[] m_RendererInfo;
    [SerializeField] Texture2D[] m_Lightmaps;
    [SerializeField] Texture2D[] m_LightmapsDir;
    [SerializeField] Texture2D[] m_ShadowMasks;
    [SerializeField] LightInfo[] m_LightInfo;


    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (m_RendererInfo == null || m_RendererInfo.Length == 0)
            return;

        var lightmaps = LightmapSettings.lightmaps;
        int[] offsetsIndexes = new int[m_Lightmaps.Length];
        int countTotal = lightmaps.Length;
        List<LightmapData> combinedLightmaps = new List<LightmapData>();

        for (int i = 0; i < m_Lightmaps.Length; i++)
        {
            bool exists = false;
            for (int j = 0; j < lightmaps.Length; j++)
            {
                if (m_Lightmaps[i] == lightmaps[j].lightmapColor)
                {
                    exists = true;
                    offsetsIndexes[i] = j;
                }
            }

            if (!exists)
            {
                offsetsIndexes[i] = countTotal;
                var newLightmapData = new LightmapData
                {
                    lightmapColor = m_Lightmaps[i],
                    lightmapDir = m_LightmapsDir.Length == m_Lightmaps.Length ? m_LightmapsDir[i] : default(Texture2D),
                    shadowMask = m_ShadowMasks.Length == m_Lightmaps.Length ? m_ShadowMasks[i] : default(Texture2D),
                };

                combinedLightmaps.Add(newLightmapData);

                countTotal += 1;
            }
        }

        var combinedLightmaps2 = new LightmapData[countTotal];

        lightmaps.CopyTo(combinedLightmaps2, 0);
        combinedLightmaps.ToArray().CopyTo(combinedLightmaps2, lightmaps.Length);

        bool directional = true;

        foreach (Texture2D t in m_LightmapsDir)
        {
            if (t == null)
            {
                directional = false;
                break;
            }
        }

        LightmapSettings.lightmapsMode = (m_LightmapsDir.Length == m_Lightmaps.Length && directional)
            ? LightmapsMode.CombinedDirectional
            : LightmapsMode.NonDirectional;
        ApplyRendererInfo(m_RendererInfo, offsetsIndexes, m_LightInfo);
        LightmapSettings.lightmaps = combinedLightmaps2;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // called second
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Init();
    }

    // called when the game is terminated
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void ApplyRendererInfo(RendererInfo[] infos, int[] lightmapOffsetIndex, LightInfo[] lightsInfo)
    {
        foreach (var info in infos)
        {
            info.renderer.lightmapIndex = lightmapOffsetIndex[info.lightmapIndex];
            info.renderer.lightmapScaleOffset = info.lightmapOffsetScale;

            if (releaseShaders)
            {
                // You have to release shaders.
                Material[] mat = info.renderer.sharedMaterials;
                foreach (var t in mat)
                {
                    if (t != null && Shader.Find(t.shader.name) != null)
                    {
                        t.shader = Shader.Find(t.shader.name);
                    }
                }
            }
        }

        for (int i = 0; i < lightsInfo.Length; i++)
        {
            LightBakingOutput bakingOutput = new LightBakingOutput
            {
                isBaked = true,
                lightmapBakeType = (LightmapBakeType)lightsInfo[i].lightmapBaketype,
                mixedLightingMode = (MixedLightingMode)lightsInfo[i].mixedLightingMode
            };

            lightsInfo[i].light.bakingOutput = bakingOutput;
        }
    }

#if UNITY_EDITOR
    [MenuItem("AWSIMLabs/Bake Prefab Lightmaps")]
    private static void GenerateLightmapInfo()
    {
        if (Lightmapping.giWorkflowMode != Lightmapping.GIWorkflowMode.OnDemand)
        {
            Debug.LogError("ExtractLightmapData requires that you have baked you lightmaps and Auto mode is disabled.");
            return;
        }

        Lightmapping.Bake();

        PrefabLightmapData[] prefabs = FindObjectsOfType<PrefabLightmapData>();

        foreach (var instance in prefabs)
        {
            var gameObject = instance.gameObject;
            var rendererInfos = new List<RendererInfo>();
            var lightmaps = new List<Texture2D>();
            var lightmapsDir = new List<Texture2D>();
            var shadowMasks = new List<Texture2D>();
            var lightsInfos = new List<LightInfo>();

            GenerateLightmapInfo(gameObject, rendererInfos, lightmaps, lightmapsDir, shadowMasks, lightsInfos);

            instance.m_RendererInfo = rendererInfos.ToArray();
            instance.m_Lightmaps = lightmaps.ToArray();
            instance.m_LightmapsDir = lightmapsDir.ToArray();
            instance.m_LightInfo = lightsInfos.ToArray();
            instance.m_ShadowMasks = shadowMasks.ToArray();
#if UNITY_2018_3_OR_NEWER
            var targetPrefab =
                PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance.gameObject) as GameObject;
            if (targetPrefab != null)
            {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(instance.gameObject); // 根结点
                //如果当前预制体是是某个嵌套预制体的一部分（IsPartOfPrefabInstance）
                if (root != null)
                {
                    GameObject rootPrefab = PrefabUtility.GetCorrespondingObjectFromSource(instance.gameObject);
                    string rootPath = AssetDatabase.GetAssetPath(rootPrefab);
                    //打开根部预制体
                    PrefabUtility.UnpackPrefabInstanceAndReturnNewOutermostRoots(root, PrefabUnpackMode.OutermostRoot);
                    try
                    {
                        //Apply各个子预制体的改变
                        PrefabUtility.ApplyPrefabInstance(instance.gameObject, InteractionMode.AutomatedAction);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        //重新更新根预制体
                        PrefabUtility.SaveAsPrefabAssetAndConnect(root, rootPath, InteractionMode.AutomatedAction);
                    }
                }
                else
                {
                    PrefabUtility.ApplyPrefabInstance(instance.gameObject, InteractionMode.AutomatedAction);
                }
            }
#else
            var targetPrefab = UnityEditor.PrefabUtility.GetPrefabParent(gameObject) as GameObject;
            if (targetPrefab != null)
            {
                //UnityEditor.Prefab
                UnityEditor.PrefabUtility.ReplacePrefab(gameObject, targetPrefab);
            }
#endif
        }
    }

    private static void GenerateLightmapInfo(GameObject root, List<RendererInfo> rendererInfos,
        List<Texture2D> lightmaps,
        List<Texture2D> lightmapsDir, List<Texture2D> shadowMasks, List<LightInfo> lightsInfo)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.lightmapIndex != -1)
            {
                RendererInfo info = new RendererInfo
                {
                    renderer = renderer
                };

                if (renderer.lightmapScaleOffset != Vector4.zero)
                {
                    //1ibrium's pointed out this issue : https://docs.unity3d.com/ScriptReference/Renderer-lightmapIndex.html
                    if (renderer.lightmapIndex is < 0 or 0xFFFE) continue;
                    info.lightmapOffsetScale = renderer.lightmapScaleOffset;

                    Texture2D lightmap = LightmapSettings.lightmaps[renderer.lightmapIndex].lightmapColor;
                    Texture2D lightmapDir = LightmapSettings.lightmaps[renderer.lightmapIndex].lightmapDir;
                    Texture2D shadowMask = LightmapSettings.lightmaps[renderer.lightmapIndex].shadowMask;

                    info.lightmapIndex = lightmaps.IndexOf(lightmap);
                    if (info.lightmapIndex == -1)
                    {
                        info.lightmapIndex = lightmaps.Count;
                        lightmaps.Add(lightmap);
                        lightmapsDir.Add(lightmapDir);
                        shadowMasks.Add(shadowMask);
                    }

                    rendererInfos.Add(info);
                }
            }
        }

        var lights = root.GetComponentsInChildren<Light>(true);

        foreach (Light l in lights)
        {
            LightInfo lightInfo = new LightInfo
            {
                light = l,
                lightmapBaketype = (int)l.lightmapBakeType
            };
#if UNITY_2020_1_OR_NEWER
            lightInfo.mixedLightingMode = (int)UnityEditor.Lightmapping.lightingSettings.mixedBakeMode;
#elif UNITY_2018_1_OR_NEWER
            lightInfo.mixedLightingMode = (int)UnityEditor.LightmapEditorSettings.mixedBakeMode;
#else
            lightInfo.mixedLightingMode = (int)l.bakingOutput.lightmapBakeType;
#endif
            lightsInfo.Add(lightInfo);
        }
    }
#endif
}