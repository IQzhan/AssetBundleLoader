using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace E
{
    public class AssetBundleTool
    {
        /// <summary>
        /// Recover shaders if they looks weird
        /// </summary>
        public static void RecoverShader()
        {
            Renderer[] renderers = Object.FindObjectsOfType<Renderer>(true);
            RecoverShader(renderers);
        }

        /// <summary>
        /// Recover shaders if they looks weird
        /// </summary>
        /// <param name="obj"></param>
        public static void RecoverShader(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            RecoverShader(renderers);
        }

        /// <summary>
        /// Recover shaders if they looks weird
        /// </summary>
        /// <param name="renderers"></param>
        public static void RecoverShader(Renderer[] renderers)
        {
            Dictionary<int, bool> recoveredIDs = new Dictionary<int, bool>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                foreach (Material material in materials)
                {
                    int key = material.GetInstanceID();
                    if (!recoveredIDs.ContainsKey(key))
                    {
                        RecoverShader(material);
                        recoveredIDs.Add(key, true);
                    }
                }
            }
            recoveredIDs.Clear();
        }

        /// <summary>
        /// Recover shaders if they looks weird
        /// </summary>
        /// <param name="material"></param>
        public static void RecoverShader(Material material)
        {
            material.shader = Shader.Find(material.shader.name);
        }
    }
}