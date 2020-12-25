using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamteck.Forever
{
    [AddComponentMenu("Dreamteck/Forever/Resource Management")]
    public class ResourceManagement : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Tooltip("List of objects to protect from unloading. Adding prefabs to the list will protect their related meshes, materials, textures and audioclips.")]
        public Object[] persistentObjects = new Object[0];

        public static bool available
        {
            get
            {
                return _available;
            }
        }

        [HideInInspector]
        [SerializeField]
        private Object[] persistentResources = new Object[0];

        private static ResourceManagement _instance = null;

        private static List<UnloadableResource> _unloadableResources = new List<UnloadableResource>();
        private static bool _available = false;

        void Awake()
        {
            if(_instance != null)
            {
                Destroy(this);
                return;
            }
            _instance = this;
            _available = true;
        }

        private void OnDestroy()
        {
            _available = false;
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            List<Object> unique = new List<Object>();
            for (int i = 0; i < persistentObjects.Length; i++)
            {
                if (persistentObjects[i] is Material || persistentObjects[i] is Mesh || persistentObjects[i] is Texture || persistentObjects[i] is AudioClip)
                {
                    AddIfUnique(persistentObjects[i], unique);
                }
                else if (persistentObjects[i] is GameObject)
                {
                    ExtractUniqueResources(persistentObjects[i] as GameObject, unique);
                }
                if (persistentObjects[i] is Material)
                {
                    Texture[] tex = GetTexturesFromMaterial(persistentObjects[i] as Material);
                    for (int j = 0; j < tex.Length; j++) AddIfUnique(tex[j], unique);
                }
            }
            persistentResources = unique.ToArray();
#endif
        }

        public void OnAfterDeserialize()
        {
        }


        public static void AddIfUnique(Object obj, List<Object> list)
        {
            if (obj == null) return;
            if (!list.Contains(obj)) list.Add(obj);
        }


        public static void RegisterUnloadableResource(Object obj, int segmentIndex)
        {
            if (!_available) return;
            if (obj == null) return;
            for (int i = 0; i < _unloadableResources.Count; i++)
            {
                if (_unloadableResources[i].resource == obj)
                {
                    _unloadableResources[i].segmentIndex = segmentIndex;
                    return;
                }
            }
            _unloadableResources.Add(new UnloadableResource(obj, segmentIndex));
        }

        public static void UnRegisterUnloadableResource(Object obj)
        {
            if (!_available) return;
            if (obj == null) return;
            for (int i = 0; i < _unloadableResources.Count; i++)
            {
                if (_unloadableResources[i].resource == obj)
                {
                    _unloadableResources.RemoveAt(i);
                    return;
                }
            }
        }

        public static void UnRegisterUnloadableResources(int segmentIndex)
        {
            if (!_available) return;
            for (int i = 0; i < _unloadableResources.Count; i++)
            {
                if (_unloadableResources[i].segmentIndex == segmentIndex)
                {
                    _unloadableResources.RemoveAt(i);
                    i--;
                    continue;
                }
            }
        }

        public static void UnloadResources(int segmentIndex)
        {
            if (_available && !LevelGenerator.instance.debugMode)
            {
                for (int i = _unloadableResources.Count - 1; i >= 0; i--)
                {
                    if (_unloadableResources[i].segmentIndex <= segmentIndex)
                    {
                        if (IsPersistent(_unloadableResources[i].resource)) continue;
                        Resources.UnloadAsset(_unloadableResources[i].resource);
                        _unloadableResources.RemoveAt(i);
                    }
                }
            }
        }

        public static void UnloadResources()
        {
            if (_available && !LevelGenerator.instance.debugMode)
            {
                for (int i = 0; i < _unloadableResources.Count; i++)
                {
                    if (IsPersistent(_unloadableResources[i].resource)) continue;
                    Resources.UnloadAsset(_unloadableResources[i].resource);
                }
                _unloadableResources.Clear();
            }
        }

        static bool IsPersistent(Object resource)
        {
            for (int i = 0; i < _instance.persistentResources.Length; i++)
            {
                if (_instance.persistentResources[i] == resource)
                {
                    return true;
                }
            }
            return false;
        }

#if UNITY_EDITOR
        public static void ExtractUniqueResources(GameObject go, List<Object> resources) //Call this in the editor when serializing instead of during the game
        {
            MeshFilter filter = go.GetComponent<MeshFilter>();
            Renderer rend = go.GetComponent<Renderer>();
            AudioSource audio = go.GetComponent<AudioSource>();
            MeshCollider collider = go.GetComponent<MeshCollider>();
            if (filter != null) AddIfUnique(filter.sharedMesh, resources);
            if (rend != null)
            {
                for (int j = 0; j < rend.sharedMaterials.Length; j++)
                {
                    if (rend.sharedMaterials[j] != null)
                    {
                        AddIfUnique(rend.sharedMaterials[j], resources);
                        Texture[] tex = GetTexturesFromMaterial(rend.sharedMaterials[j]);
                        for (int k = 0; k < tex.Length; k++) AddIfUnique(tex[k], resources);
                    }
                }
                if (rend is ParticleSystemRenderer)
                {
                    ParticleSystemRenderer psrend = (ParticleSystemRenderer)rend;
                    Mesh[] psMeshes = new Mesh[4];
                    int meshCount = psrend.GetMeshes(psMeshes);
                    for (int j = 0; j < meshCount; j++) AddIfUnique(psMeshes[j], resources);
                }
            }
            if (audio != null && audio.clip != null && !resources.Contains(audio.clip)) resources.Add(audio.clip);
            if (collider != null && collider.sharedMesh != null && !resources.Contains(collider.sharedMesh)) resources.Add(collider.sharedMesh);
        }

        public static Texture[] GetTexturesFromMaterial(Material material)
        {
            Shader shader = material.shader;
            Texture[] textures = new Texture[UnityEditor.ShaderUtil.GetPropertyCount(shader)];
            for (int i = 0; i < textures.Length; i++)
            {
                if (UnityEditor.ShaderUtil.GetPropertyType(shader, i) == UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    textures[i] = material.GetTexture(UnityEditor.ShaderUtil.GetPropertyName(shader, i));
                }
            }
            return textures;
        }
#endif

        internal class UnloadableResource
        {
            internal Object resource = null;
            internal int segmentIndex = 0;

            internal UnloadableResource(Object obj, int indeX)
            {
                resource = obj;
                segmentIndex = indeX;
            }
        }
    }
}
