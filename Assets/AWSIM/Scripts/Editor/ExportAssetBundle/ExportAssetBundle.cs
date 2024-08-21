using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.Plastic.Newtonsoft.Json;
using Unity.VisualScripting.IonicZip;
// using ICSharpCode.SharpZipLib.Zip;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AWSIM.Scripts.Editor.ExportAssetBundle
{
    public class ExportAssetBundle : EditorWindow
    {
        [SerializeField] private bool _doBuildEgoVehicle;
        [SerializeField] private bool _doBuildEnvironment;

        [SerializeField] private string _playerFolder = string.Empty;

        private const string SceneExtension = "unity";
        private const string ScriptExtension = "cs";
        private const string PrefabExtension = "prefab";
        public static bool BuildSuccessful;
        private static Dictionary<string, BundleData> BuildGroups;

        #region data

        public class BundleData
        {
            public BundleData(BundleConfig.BundleTypes type, string path = null)
            {
                bundleType = type;
                bundlePath = path ?? BundleConfig.PluralOf(type);
            }

            public class Entry
            {
                public string name;
                public string mainAssetFile;
                public bool selected;
                public bool available;
            }

            public BundleConfig.BundleTypes bundleType;
            public string bundlePath;
            public string sourcePath => Path.Combine(BundleConfig.ExternalBase, bundlePath);
            public bool isOpen = false;

            public Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

            public void OnGUI()
            {
                if (entries.Count == 0)
                {
                    EditorGUILayout.HelpBox($"No {bundlePath} are available", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Following {bundlePath} were automatically detected:", MessageType.None);
                }

                #region unity 2020.3.3f1 fix

                // TODO fix for issues with unity 2020.3.3f1 multiple bundles

                //if (entries.Count != 0)
                //{
                //    EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
                //    if (GUILayout.Button("Select All", GUILayout.ExpandWidth(false)))
                //    {
                //        foreach (var entry in entries)
                //        {
                //            entry.Value.selected = true;
                //        }
                //    }
                //    if (GUILayout.Button("Select None", GUILayout.ExpandWidth(false)))
                //    {
                //        foreach (var entry in entries)
                //        {
                //            entry.Value.selected = false;
                //        }
                //    }
                //    EditorGUILayout.EndHorizontal();
                //}

                foreach (var entry in entries.OrderBy(entry => entry.Key))
                {
                    if (entry.Value.available)
                    {
                        if (GUILayout.Toggle(entry.Value.selected, entry.Key))
                        {
                            // BuildPlayer = false;
                            foreach (var group in BuildGroups.Values)
                            {
                                foreach (var e in group.entries.Values)
                                {
                                    e.selected = false;
                                }
                            }

                            entry.Value.selected = true;
                            // CurrentSelectedEntryName = entry.Value.name;
                        }
                        else
                        {
                            entry.Value.selected = false;
                        }
                    }
                    else
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        GUILayout.Toggle(false, $"{entry.Key} (missing items/{entry.Value.mainAssetFile}");
                        EditorGUI.EndDisabledGroup();
                    }
                }

                #endregion
            }

            public void Refresh()
            {
                var updated = new HashSet<string>();
                foreach (var entry in Directory.EnumerateDirectories(sourcePath))
                {
                    var name = Path.GetFileName(entry);

                    if (name.StartsWith("."))
                    {
                        continue;
                    }

                    if (!entries.ContainsKey(name))
                    {
                        var extension = bundleType == BundleConfig.BundleTypes.Environment;
                        var fullPath = Path.Combine(sourcePath, name, $"{name}.{extension}");

                        entries.Add(name, new Entry
                        {
                            name = name,
                            mainAssetFile = fullPath,
                            available = File.Exists(fullPath),
                            selected = false
                        });
                    }

                    updated.Add(name);
                }

                entries = entries.Where(entry => updated.Contains(entry.Key)).ToDictionary(p => p.Key, p => p.Value);
            }

            public bool EnableByName(string name)
            {
                if (!entries.ContainsKey(name))
                {
                    var knownKeys = string.Join(",", entries.Keys);
                    Debug.LogWarning(
                        $"[BUILD] could not enable entry {name} as it was not found. Known entries of {bundlePath} are {knownKeys}");

                    return false;
                }

                Debug.Log($"[BUILD] Enable entry '{name}' of {bundlePath}");

                entries[name].selected = true;
                return true;
            }

            private void PreparePrefabManifest(Entry prefabEntry, string outputFolder,
                List<(string, string)> buildArtifacts, Manifest manifest)
            {
                const string vehiclePreviewScenePath = "Assets/PreviewEnvironmentAssets/PreviewEnvironmentScene.unity";

                string assetGuid = Guid.NewGuid().ToString();
                manifest.assetName = prefabEntry.name;
                manifest.assetGuid = assetGuid;
                manifest.assetFormat = BundleConfig.Versions[bundleType];
                manifest.description = "";
                manifest.fmuName = "";

                // Scene scene = EditorSceneManager.OpenScene(vehiclePreviewScenePath, OpenSceneMode.Additive);
                // var scenePath = scene.path;

                try
                {
                    if (bundleType == BundleConfig.BundleTypes.Vehicle)
                    {
                        var vehiclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabEntry.mainAssetFile);
                        var rigidbody = vehiclePrefab.GetComponent<Rigidbody>();
                        var articulationBody = vehiclePrefab.GetComponent<ArticulationBody>();
                        if (rigidbody == null && articulationBody == null)
                        {
                            throw new Exception(
                                $"Build failed: Rigidbody or ArticulationBody on {prefabEntry.mainAssetFile} not found. Please add a Rigidbody component and rebuild.");
                        }

                        // var controller = vehiclePrefab.GetComponent<IAgentController>();
                        // if (controller == null)
                        // {
                        //     throw new Exception(
                        //         $"Build failed: IAgentController implementation on {prefabEntry.mainAssetFile} not found. Please add a component implementing IAgentController and rebuild.");
                        // }
                        //
                        // var info = vehiclePrefab.GetComponent<VehicleInfo>();
                        // var fmu = vehiclePrefab.GetComponent<VehicleFMU>();
                        // var baseLink = vehiclePrefab.GetComponentInChildren<BaseLink>();
                        //
                        // if (info == null)
                        // {
                        //     Debug.LogWarning(
                        //         $"Build warning: Vehicle info on {prefabEntry.mainAssetFile} not found. Please add a VehicleInfo component to include meta data in the manifest.");
                        // }
                        //
                        // manifest.description = info != null ? info.Description : "";
                        // manifest.assetType = "vehicle";
                        // manifest.fmuName = fmu == null ? "" : fmu.FMUData.Name;
                        //
                        // manifest.baseLink = baseLink != null
                        //     ? new double[]
                        //     {
                        //         baseLink.transform.position.x, baseLink.transform.position.y,
                        //         baseLink.transform.position.z
                        //     }
                        //     : // rotation
                        //     new double[] { 0, 0, 0 };

                        Dictionary<string, object> files = new Dictionary<string, object>();
                        manifest.attachments = files;

                        // using (var exporter = new GltfExporter(prefabEntry.mainAssetFile, manifest.assetGuid,
                        //            manifest.assetName))
                        // {
                        //     var fileName = exporter.Export(outputFolder);
                        //     var glbOut = Path.Combine(outputFolder, fileName);
                        //     buildArtifacts.Add((glbOut, Path.Combine("gltf", fileName)));
                        //     files.Add("gltf", ZipPath("gltf", fileName));
                        // }

                        // var textures = new BundlePreviewRenderer.PreviewTextures();
                        // BundlePreviewRenderer.RenderVehiclePreview(prefabEntry.mainAssetFile, textures);
                        // var bytesLarge = textures.large.EncodeToPNG();
                        // textures.Release();

                        string tmpdir = Path.Combine(outputFolder, $"{manifest.assetName}_pictures");
                        Directory.CreateDirectory(tmpdir);
                        // File.WriteAllBytes(Path.Combine(tmpdir, "preview-1.png"), bytesLarge);

                        // var images = new string[]
                        // {
                        //     ZipPath("images", "preview-1.png"),
                        // };
                        // manifest.attachments.Add("images", images);
                        //
                        // buildArtifacts.Add((Path.Combine(tmpdir, "preview-1.png"), images[0]));
                        buildArtifacts.Add((tmpdir, null));
                    }
                }
                finally
                {
                    // var openedScene = SceneManager.GetSceneByPath(scenePath);
                    // EditorSceneManager.CloseScene(openedScene, true);
                }
            }

            private void PrepareSceneManifest(Entry sceneEntry, string outputFolder,
                List<(string, string)> buildArtifacts, Manifest manifest)
            {
                Scene scene = EditorSceneManager.OpenScene(sceneEntry.mainAssetFile, OpenSceneMode.Additive);
                var scenePath = scene.path;
                // NodeTreeLoader[] loaders = GameObject.FindObjectsOfType<NodeTreeLoader>();
                // string dataPath = GameObject.FindObjectOfType<NodeTreeLoader>()?.GetFullDataPath();
                // List<Tuple<string, string>> loaderPaths = new List<Tuple<string, string>>();

                // foreach (NodeTreeLoader loader in loaders)
                // {
                //     loaderPaths.Add(new Tuple<string, string>(
                //         Utilities.Utility.StringToGUID(loader.GetDataPath()).ToString(), loader.GetFullDataPath()));
                // }

                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        // MapOrigin origin = root.GetComponentInChildren<MapOrigin>();
                        // if (origin != null)
                        // {
                        //     manifest.assetName = sceneEntry.name;
                        //     manifest.assetType = "map";
                        //     manifest.assetGuid = Guid.NewGuid().ToString();
                        //     manifest.mapOrigin = new double[] { origin.OriginEasting, origin.OriginNorthing };
                        //     manifest.assetFormat = BundleConfig.Versions[BundleConfig.BundleTypes.Environment];
                        //     manifest.description = origin.Description;
                        //     manifest.fmuName = "";
                        //     manifest.attachments = new Dictionary<string, object>();
                        //
                        //     string name = manifest.assetName;
                        //
                        // var previewOrigin = origin.transform;
                        // var spawns = FindObjectsOfType<SpawnInfo>().OrderBy(spawn => spawn.name).ToList();
                        // var mapPreview = FindObjectOfType<MapPreview>();
                        // var forcePreview = false;
                        // if (mapPreview != null)
                        // {
                        //     previewOrigin = mapPreview.transform;
                        //     forcePreview = true;
                        // }
                        // else if (spawns.Count > 0)
                        // {
                        //     previewOrigin = spawns[0].transform;
                        // }
                        // else
                        // {
                        //     Debug.LogError("No spawns or map preview found, preview will be rendered from origin.");
                        // }
                        //
                        // var textures = new BundlePreviewRenderer.PreviewTextures();
                        // BundlePreviewRenderer.RenderScenePreview(previewOrigin, textures, forcePreview);
                        // var bytesLarge = textures.large.EncodeToPNG();
                        // textures.Release();
                        //
                        // tmpdir = Path.Combine(outputFolder, $"{name}_pictures");
                        // Directory.CreateDirectory(tmpdir);
                        // File.WriteAllBytes(Path.Combine(tmpdir, "preview-1.png"), bytesLarge);
                        //
                        // var images = new string[]
                        // {
                        //     ZipPath("images", "preview-1.png"),
                        // };
                        // manifest.attachments.Add("images", images);
                        // buildArtifacts.Add((Path.Combine(tmpdir, "preview-1.png"), images[0]));
                        // buildArtifacts.Add((tmpdir, null));
                        //
                        // foreach (Tuple<string, string> t in loaderPaths)
                        // {
                        //     if (!manifest.attachments.ContainsKey($"pointcloud_{t.Item1}"))
                        //     {
                        //         manifest.attachments.Add($"pointcloud_{t.Item1}", t.Item2);
                        //     }
                        // }

                        //     return;
                        // }
                    }

                    throw new Exception(
                        $"Build failed: MapOrigin on {sceneEntry.name} not found. Please add a MapOrigin component.");
                }
                finally
                {
                    var openedScene = SceneManager.GetSceneByPath(scenePath);
                    EditorSceneManager.CloseScene(openedScene, true);
                }
            }

            public void RunBuild(string outputFolder)
            {
                const string loaderScenePath = "Assets/Scenes/LoaderScene.unity";
                string Thing = BundleConfig.SingularOf(bundleType);
                string Things = BundleConfig.PluralOf(bundleType);
                string thing = Thing.ToLower();

                outputFolder = Path.Combine(outputFolder, bundlePath);
                Directory.CreateDirectory(outputFolder);
                var openScenePaths = new List<string>();

                var selected = entries.Values.Where(e => e.selected && e.available).ToList();
                if (selected.Count == 0) return;

                if (bundleType == BundleConfig.BundleTypes.Environment)
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        Debug.LogWarning("Cancelling the build.");
                        return;
                    }
                }

                var activeScenePath = SceneManager.GetActiveScene().path;
                for (int i = 0; i < SceneManager.loadedSceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    openScenePaths.Add(scene.path);
                }

                EditorSceneManager.OpenScene(loaderScenePath, OpenSceneMode.Single);

                try
                {
                    foreach (var entry in selected)
                    {
                        BuildSuccessful = false;
                        Manifest manifest = new Manifest();
                        var buildArtifacts = new List<(string source, string archiveName)>();
                        var persistentBuildArtifacts = new List<(string source, string archiveName)>();
                        bool mainAssetIsScript = entry.mainAssetFile.EndsWith("." + ScriptExtension);
                        bool hasPrefabs = AssetDatabase
                            .FindAssets("t:GameObject", new[] { Path.Combine(sourcePath, entry.name) }).Any();
                        if (bundleType == BundleConfig.BundleTypes.Environment)
                        {
                            PrepareSceneManifest(entry, outputFolder, buildArtifacts, manifest);
                            manifest.assetType = "map";
                        }
                        else
                        {
                            PreparePrefabManifest(entry, outputFolder, buildArtifacts, manifest);
                            manifest.assetType = thing;
                        }

                        var asmDefPath = Path.Combine(BundleConfig.ExternalBase, Things, $"Simulator.{Things}.asmdef");
                        AsmdefBody asmDef = null;
                        if (File.Exists(asmDefPath))
                        {
                            asmDef = JsonUtility.FromJson<AsmdefBody>(File.ReadAllText(asmDefPath));
                        }

                        try
                        {
                            Debug.Log($"Building asset: {entry.mainAssetFile} -> " +
                                      Path.Combine(outputFolder, $"{thing}_{entry.name}"));

                            if (!File.Exists(Path.Combine(Application.dataPath, "..", entry.mainAssetFile)))
                            {
                                Debug.LogError($"Building of {entry.name} failed: {entry.mainAssetFile} not found");
                                break;
                            }

                            if (asmDef != null)
                            {
                                AsmdefBody asmdefContents = new AsmdefBody();
                                asmdefContents.name = entry.name;
                                asmdefContents.references = asmDef.references;
                                var asmDefOut = Path.Combine(sourcePath, entry.name, $"{entry.name}.asmdef");
                                File.WriteAllText(asmDefOut, JsonUtility.ToJson(asmdefContents));
                                buildArtifacts.Add((asmDefOut, null));
                            }

                            AssetDatabase.Refresh();
                            if (hasPrefabs)
                            {
                                var texturesNames = new List<string>();
                                var assetsNames = new List<string>();
                                switch (bundleType)
                                {
                                    case BundleConfig.BundleTypes.Vehicle:
                                    case BundleConfig.BundleTypes.Environment:
                                        ////Include the main asset only
                                        texturesNames.AddRange(AssetDatabase.GetDependencies(entry.mainAssetFile)
                                            .Where(a => a.EndsWith(".png") || a.EndsWith(".jpg") || a.EndsWith(".tga"))
                                            .ToArray());
                                        assetsNames.Add(entry.mainAssetFile);
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                var textureBuild = new AssetBundleBuild()
                                {
                                    assetBundleName = $"{manifest.assetGuid}_{thing}_textures",
                                    assetNames = texturesNames.Distinct().ToArray()
                                };
                                bool buildTextureBundle = textureBuild.assetNames.Length > 0;

                                var windowsBuild = new AssetBundleBuild()
                                {
                                    assetBundleName = $"{manifest.assetGuid}_{thing}_main_windows",
                                    assetNames = assetsNames.ToArray()
                                };

                                var linuxBuild = new AssetBundleBuild()
                                {
                                    assetBundleName = $"{manifest.assetGuid}_{thing}_main_linux",
                                    assetNames = assetsNames.ToArray()
                                };

                                var builds = new[]
                                {
                                    (build: linuxBuild, platform: UnityEditor.BuildTarget.StandaloneLinux64),
                                    (build: windowsBuild, platform: UnityEditor.BuildTarget.StandaloneWindows64)
                                };

                                foreach (var buildConf in builds)
                                {
                                    var taskItems = new List<AssetBundleBuild>() { buildConf.build };

                                    if (buildTextureBundle)
                                    {
                                        taskItems.Add(textureBuild);
                                    }

                                    BuildPipeline.BuildAssetBundles(
                                        outputFolder,
                                        taskItems.ToArray(),
                                        BuildAssetBundleOptions.ChunkBasedCompression |
                                        BuildAssetBundleOptions.StrictMode,
                                        buildConf.platform);

                                    buildArtifacts.Add((Path.Combine(outputFolder, buildConf.build.assetBundleName),
                                        buildConf.build.assetBundleName));
                                    buildArtifacts.Add((
                                        Path.Combine(outputFolder, buildConf.build.assetBundleName + ".manifest"),
                                        null));
                                    if (buildTextureBundle)
                                    {
                                        buildArtifacts.Add((Path.Combine(outputFolder, textureBuild.assetBundleName),
                                            textureBuild.assetBundleName));
                                        buildArtifacts.Add((
                                            Path.Combine(outputFolder, textureBuild.assetBundleName + ".manifest"),
                                            null));
                                    }
                                }
                            }

                            DirectoryInfo prefabDir = new DirectoryInfo(Path.Combine(sourcePath, entry.name));
                            var scripts = prefabDir.GetFiles("*.cs", SearchOption.AllDirectories)
                                .Select(script => script.FullName).ToArray();

                            string outputAssembly = null;
                            if (scripts.Length > 0)
                            {
                                outputAssembly = Path.Combine(outputFolder, $"{entry.name}.dll");
                                var assemblyBuilder = new AssemblyBuilder(outputAssembly, scripts);
                                assemblyBuilder.compilerOptions.AllowUnsafeCode = true;
                                assemblyBuilder.referencesOptions = ReferencesOptions.UseEngineModules;
                                assemblyBuilder.buildFinished += delegate (string assemblyPath,
                                    CompilerMessage[] compilerMessages)
                                {
                                    var errorCount = compilerMessages.Count(m => m.type == CompilerMessageType.Error);
                                    var warningCount =
                                        compilerMessages.Count(m => m.type == CompilerMessageType.Warning);

                                    Debug.Log($"Assembly build finished for {assemblyPath}");
                                    if (errorCount != 0)
                                    {
                                        Debug.Log($"Found {errorCount} errors");

                                        foreach (CompilerMessage message in compilerMessages)
                                        {
                                            if (message.type == CompilerMessageType.Error)
                                            {
                                                Debug.LogError(message.message);
                                                return;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        buildArtifacts.Add((outputAssembly, $"{entry.name}.dll"));
                                        buildArtifacts.Add((Path.Combine(outputFolder, $"{entry.name}.pdb"), null));
                                    }
                                };

                                // Start build of assembly
                                if (!assemblyBuilder.Build())
                                {
                                    Debug.LogErrorFormat("Failed to start build of assembly {0}!",
                                        assemblyBuilder.assemblyPath);
                                    return;
                                }

                                while (assemblyBuilder.status != AssemblyBuilderStatus.Finished)
                                {
                                    Thread.Sleep(0);
                                }
                            }

                            if (manifest.fmuName != "")
                            {
                                var fmuPathWindows = Path.Combine(sourcePath, manifest.assetName, manifest.fmuName,
                                    "binaries", "win64", $"{manifest.fmuName}.dll");
                                var fmuPathLinux = Path.Combine(sourcePath, manifest.assetName, manifest.fmuName,
                                    "binaries", "linux64", $"{manifest.fmuName}.so");
                                if (File.Exists(fmuPathWindows))
                                {
                                    buildArtifacts.Add((fmuPathWindows, $"{manifest.fmuName}_windows.dll"));
                                }

                                if (File.Exists(fmuPathLinux))
                                {
                                    buildArtifacts.Add((fmuPathLinux, $"{manifest.fmuName}_linux.so"));
                                }
                            }

                            if (manifest.attachments != null)
                            {
                                foreach (string key in manifest.attachments.Keys)
                                {
                                    if (key.Contains("pointcloud"))
                                    {
                                        foreach (FileInfo fi in new DirectoryInfo(manifest.attachments[key].ToString())
                                                     .GetFiles())
                                        {
                                            // if (fi.Extension == TreeUtility.IndexFileExtension ||
                                            //     fi.Extension == TreeUtility.NodeFileExtension ||
                                            //     fi.Extension == TreeUtility.MeshFileExtension)
                                            // {
                                            //     persistentBuildArtifacts.Add((fi.FullName, Path.Combine(key, fi.Name)));
                                            // }
                                        }
                                    }
                                }
                            }

                            var manifestOutput = Path.Combine(outputFolder, "manifest.json");
                            File.WriteAllText(manifestOutput, JsonConvert.SerializeObject(manifest));
                            buildArtifacts.Add((manifestOutput, "manifest.json"));

                            // ZipFile archive = ZipFile.Create(Path.Combine(outputFolder, $"{thing}_{entry.name}"));
                            // archive.BeginUpdate();
                            // foreach (var file in buildArtifacts.Where(e => e.archiveName != null))
                            //     archive.Add(new StaticDiskDataSource(file.source), file.archiveName,
                            //         CompressionMethod.Stored, true);
                            //
                            // foreach (var file in persistentBuildArtifacts.Where(e => e.archiveName != null))
                            //     archive.Add(new StaticDiskDataSource(file.source), file.archiveName,
                            //         CompressionMethod.Stored, true);
                            //
                            // archive.CommitUpdate();
                            // archive.Close();
                            BuildSuccessful = true;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Failed to build archive, exception follows:");
                            Debug.LogException(e);
                            BuildSuccessful = false;
                        }
                        finally
                        {
                            foreach (var file in buildArtifacts)
                            {
                                SilentDelete(file.source);
                                SilentDelete(file.source + ".meta");
                            }
                        }

                        Debug.Log("done");
                        Resources.UnloadUnusedAssets();
                    }

                    // these are an artifact of the asset building pipeline and we don't use them
                    SilentDelete(Path.Combine(outputFolder, Path.GetFileName(outputFolder)));
                    SilentDelete(Path.Combine(outputFolder, Path.GetFileName(outputFolder)) + ".manifest");
                }
                finally
                {
                    // Load back previously opened scenes
                    var mainScenePath = string.IsNullOrEmpty(activeScenePath) ? loaderScenePath : activeScenePath;
                    EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);
                    foreach (var scenePath in openScenePaths)
                    {
                        if (string.Equals(scenePath, activeScenePath) || string.IsNullOrEmpty(scenePath))
                            continue;

                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    }
                }
            }
        }

        #endregion

        #region asmdef

        class AsmdefBody
        {
            public string name;

            public string[] references;

            // TODO: This will enable 'unsafe' code for all.
            // We may find a better way to unable it only when necessary.
            public bool allowUnsafeCode = true;
        }

        #endregion

        [MenuItem("AWSIM/Export Asset Bundles")]
        private static void ShowWindow()
        {
            GetWindow<ExportAssetBundle>("Export Asset Bundles");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Export Settings", EditorStyles.boldLabel);
            _doBuildEgoVehicle = EditorGUILayout.Toggle("Export the ego vehicle", _doBuildEgoVehicle);
            _doBuildEnvironment = EditorGUILayout.Toggle("Export the environment", _doBuildEnvironment);
            GUILayout.Space(10);
            if (GUILayout.Button("Export"))
            {
                Export();
            }
        }

        private void Export()
        {
            void OnComplete()
            {
                if (BuildSuccessful)
                {
                    BuildCompletePopup.Init();
                }
            }

            var assetBundlesLocation = Path.Combine(Application.dataPath, "..", "AssetBundles");
            EditorApplication.delayCall += () => BuildBundles(assetBundlesLocation, OnComplete);
        }

        private void BuildBundles(string outputFolder, Action onComplete = null)
        {
            try
            {
                foreach (var group in BuildGroups.Values)
                {
                    group.RunBuild(outputFolder);
                }
            }
            finally
            {
                onComplete?.Invoke();
            }
        }

        #region SilentDelete

        private static void SilentDelete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path);
            }
        }

        #endregion
    }

    public class BuildCompletePopup : EditorWindow
    {
        public static void Init()
        {
            var window = CreateInstance<BuildCompletePopup>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 150);
            window.ShowPopup();
        }

        // give info about build results
        void OnGUI()
        {
        }
    }


    #region BUNDLECONFIG

    public static class BundleConfig
    {
        public enum BundleTypes
        {
            Vehicle,
            Environment,
        }

        public static Dictionary<BundleTypes, string> Versions = new Dictionary<BundleTypes, string>()
        {
            [BundleTypes.Vehicle] = "awsimlabs.Vehicle",
            [BundleTypes.Environment] = "awsimlabs.Environment",
        };

        public static string SingularOf(BundleTypes type) => Enum.GetName(typeof(BundleTypes), type);
        public static string PluralOf(BundleTypes type) => Enum.GetName(typeof(BundleTypes), type) + "s";
        public static string ExternalBase = Path.Combine("Assets", "External");
    }

    #endregion

    #region MANIFEST

    public class Manifest
    {
        public string assetName;
        public string assetType;
        public string assetGuid;
        public string assetFormat;
        public double[] mapOrigin;
        public double[] baseLink;
        public string description;
        public string fmuName;

        public Dictionary<string, object> attachments;
    }

    #endregion
}
