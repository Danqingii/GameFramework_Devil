using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HybridCLR
{
    public class HybridCLRHelper
    {
        public static string ToRelativeAssetPath(string s)
        {
            return s.Substring(s.IndexOf("Assets/"));
        }
        
        /// <summary>
        /// 将HotFix.dll和HotUpdatePrefab.prefab打入common包.
        /// 将HotUpdateScene.unity打入scene包.
        /// </summary>
        /// <param name="hotfixDir"></param>
        /// <param name="aotDir"></param>
        /// <param name="target"></param>
        private static void BuildAssembly(string hotfixDir,string aotDir, BuildTarget target)
        {
            Directory.CreateDirectory(hotfixDir);
            Directory.CreateDirectory(aotDir);
            CompileDllHelper.CompileDll(target);

            List<string> notSceneAssets = new List<string>();

            //热更新dll
            string hotfixDllSrcDir = BuildConfig.GetHotFixDllsOutputDirByTarget(target);
            foreach (var dll in BuildConfig.AllHotUpdateDllNames)
            {
                string dllPath = $"{hotfixDllSrcDir}/{dll}";
                string dllBytesPath = $"{hotfixDir}/{dll}.bytes";
                File.Copy(dllPath, dllBytesPath, true);
                notSceneAssets.Add(dllBytesPath);
            }

            //AOT dll
            string aotDllDir = BuildConfig.GetAssembliesPostIl2CppStripDir(target);
            foreach (var dll in BuildConfig.AOTMetaDlls)
            {
                string dllPath = $"{aotDllDir}/{dll}";
                if (!File.Exists(dllPath))
                {
                    Debug.LogError($"ab中添加AOT补充元数据dll:{dllPath} 时发生错误,文件不存在。裁剪后的AOT dll在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包。");
                    continue;
                }
                string dllBytesPath = $"{aotDir}/{dll}.bytes";
                File.Copy(dllPath, dllBytesPath, true);
                notSceneAssets.Add(dllBytesPath);
            }

            for (int i = 0; i < notSceneAssets.Count; i++)
            {
                Debug.Log($"suc: {notSceneAssets[i]}");
            }
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        [MenuItem("HybridCLR/BuildDll/ActiveBuildTarget")]
        public static void BuildAssemblyTarget()
        {
            BuildAssembly(BuildConfig.HotfixCacheDir,BuildConfig.MetadataForAOTAssemblyDir,EditorUserBuildSettings.activeBuildTarget);
        }
    }
}