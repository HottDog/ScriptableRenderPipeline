using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [Serializable]
    class VFXGraphAsset : ScriptableObject
    {
        public VFXGraph root { get { return m_Root; } }

        [SerializeField]
        private VFXGraph m_Root;

        public void UpdateSubAssets()
        {
            if (EditorUtility.IsPersistent(this))
            {
                Profiler.BeginSample("UpdateSubAssets");

                try
                {
                    HashSet<Object> persistentObjects = new HashSet<Object>(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)));
                    persistentObjects.Remove(this);

                    //foreach (var o in persistentObjects)
                    //    Debug.Log("PERSISTENT: " + o);

                    HashSet<Object> currentObjects = new HashSet<Object>();

                    m_Root.CollectDependencies(currentObjects);
                    currentObjects.Add(m_Root);

                    //foreach (var o in currentObjects)
                    //    Debug.Log("CURRENT: " + o);

                    // Add sub assets that are not already present
                    foreach (var obj in currentObjects)
                        if (!persistentObjects.Contains(obj))
                            AssetDatabase.AddObjectToAsset(obj, this);

                    // Remove sub assets that are not referenced anymore
                    foreach (var obj in persistentObjects)
                        if (!currentObjects.Contains(obj))
                            ScriptableObject.DestroyImmediate(obj, true);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }

                Profiler.EndSample();
            }
        }

        private void OnModelInvalidate(VFXModel model,VFXModel.InvalidationCause cause)
        {
            if (cause == VFXModel.InvalidationCause.kStructureChanged)
                UpdateSubAssets();

            EditorUtility.SetDirty(this);
        }

        void OnEnable()
        {
            if (m_Root == null)
                m_Root = ScriptableObject.CreateInstance<VFXGraph>();
            m_Root.onInvalidateDelegate += OnModelInvalidate;
        }
    }
}
