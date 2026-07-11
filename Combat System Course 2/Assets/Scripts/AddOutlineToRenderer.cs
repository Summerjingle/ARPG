using UnityEngine;

/// <summary>
/// 给已有材质的物体加描边 —— 创建子物体 + OutlineOnly shader，不动原始材质。
/// 支持 MeshRenderer 和 SkinnedMeshRenderer。
/// 使用方法：挂到目标物体上，拖入用 OutlineOnly_URP 创建的材质即可。
/// </summary>
[RequireComponent(typeof(Renderer))]
public class AddOutlineToRenderer : MonoBehaviour
{
    [Header("Outline Material")]
    [Tooltip("用 Custom/OutlineOnly_URP 创建的材质")]
    [SerializeField] private Material _outlineMaterial;

    [Header("Auto Setup")]
    [SerializeField] private bool _createOnAwake = true;

    [Header("Interaction")]
    [Tooltip("首次交互后自动销毁描边（需要同 GameObject 上有 IInteractable）")]
    [SerializeField] private bool _removeAfterInteract = false;

    private GameObject _outlineChild;
    private Renderer _parentRenderer;
    private Renderer _outlineRenderer;
    private IInteractable _interactable;

    // ==================== Lifecycle ====================

    private void Awake()
    {
        if (_createOnAwake)
            CreateOutline();
    }

    private void Start()
    {
        if (_removeAfterInteract)
        {
            _interactable = GetComponentInParent<IInteractable>();
            if (_interactable != null)
            {
                _interactable.OnInteracted += HandleInteracted;

                // 读取存档后，如果交互已经发生过（门已开），立即销毁描边并退订
                if (!_interactable.CanInteract)
                {
                    _interactable.OnInteracted -= HandleInteracted;
                    DestroyOutline();
                }
            }
        }
    }

    private void HandleInteracted()
    {
        if (_outlineChild == null) return;
        _interactable.OnInteracted -= HandleInteracted;
        DestroyOutline();
    }

    private void OnDestroy()
    {
        if (_interactable != null)
            _interactable.OnInteracted -= HandleInteracted;
        DestroyOutline();
    }

    // 编辑器中修改属性时自动刷新
    private void OnValidate()
    {
        if (!Application.isPlaying && _outlineChild != null)
        {
            RefreshOutline();
        }
    }

    private void Reset()
    {
        _createOnAwake = true;
    }

    // ==================== Public API ====================

    /// <summary>手动创建描边子物体</summary>
    [ContextMenu("Create Outline")]
    public void CreateOutline()
    {
        if (_outlineMaterial == null)
        {
            Debug.LogWarning($"[AddOutlineToRenderer] {gameObject.name}: Outline Material 未赋值，跳过。", this);
            return;
        }

        _parentRenderer = GetComponent<Renderer>();
        if (_parentRenderer == null)
        {
            Debug.LogError($"[AddOutlineToRenderer] {gameObject.name}: 找不到 Renderer 组件。", this);
            return;
        }

        // 避免重复创建
        DestroyOutline();

        _outlineChild = new GameObject("Outline");
        _outlineChild.layer = gameObject.layer;
        _outlineChild.transform.SetParent(transform);
        _outlineChild.transform.localPosition = Vector3.zero;
        _outlineChild.transform.localRotation = Quaternion.identity;
        _outlineChild.transform.localScale = Vector3.one;
        _outlineChild.hideFlags = HideFlags.NotEditable;

        // 根据父物体类型复制 Mesh
        if (_parentRenderer is SkinnedMeshRenderer parentSkinned)
        {
            var skinned = _outlineChild.AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = parentSkinned.sharedMesh;
            skinned.bones = parentSkinned.bones;
            skinned.rootBone = parentSkinned.rootBone;
            skinned.quality = parentSkinned.quality;
            skinned.updateWhenOffscreen = parentSkinned.updateWhenOffscreen;

            var matCount = parentSkinned.sharedMaterials.Length;
            var outlineMats = new Material[matCount];
            for (int i = 0; i < matCount; i++) outlineMats[i] = _outlineMaterial;
            skinned.materials = outlineMats;

            skinned.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            skinned.receiveShadows = false;
            skinned.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            skinned.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            _outlineRenderer = skinned;
        }
        else if (_parentRenderer is MeshRenderer)
        {
            var parentFilter = GetComponent<MeshFilter>();
            if (parentFilter == null)
            {
                Debug.LogError($"[AddOutlineToRenderer] {gameObject.name}: MeshRenderer 但没有 MeshFilter。", this);
                DestroyImmediate(_outlineChild);
                _outlineChild = null;
                return;
            }

            var filter = _outlineChild.AddComponent<MeshFilter>();
            filter.sharedMesh = parentFilter.sharedMesh;

            // 检测 Static Batching 导致的 Combined Mesh：顶点数异常大，Mesh 名包含 "Combined Mesh"
            if (parentFilter.sharedMesh != null && parentFilter.sharedMesh.name.Contains("Combined Mesh"))
            {
                Debug.LogError($"[AddOutlineToRenderer] {gameObject.name}: 检测到 Static Batching 合并网格 (\"{parentFilter.sharedMesh.name}\", {parentFilter.sharedMesh.vertexCount} 顶点)！请在 Inspector 右上角 Static 下拉中取消勾选 Batching Static，否则 Build 中描边会渲染整片场景。", this);
                DestroyImmediate(_outlineChild);
                _outlineChild = null;
                return;
            }

            var meshRenderer = _outlineChild.AddComponent<MeshRenderer>();
            var matCount = _parentRenderer.sharedMaterials.Length;
            var outlineMats = new Material[matCount];
            for (int i = 0; i < matCount; i++) outlineMats[i] = _outlineMaterial;
            meshRenderer.materials = outlineMats;

            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            _outlineRenderer = meshRenderer;
        }
        else
        {
            Debug.LogError($"[AddOutlineToRenderer] {gameObject.name}: 不支持的 Renderer 类型 ({_parentRenderer.GetType().Name})。", this);
            DestroyImmediate(_outlineChild);
            _outlineChild = null;
            return;
        }
    }

    /// <summary>手动移除描边子物体</summary>
    [ContextMenu("Remove Outline")]
    public void DestroyOutline()
    {
        if (_outlineChild == null) return;

        if (Application.isPlaying)
            Destroy(_outlineChild);
        else
            DestroyImmediate(_outlineChild);

        _outlineChild = null;
        _outlineRenderer = null;
    }

    /// <summary>刷新描边（Mesh 或材质改变后调用）</summary>
    [ContextMenu("Refresh Outline")]
    public void RefreshOutline()
    {
        if (_outlineRenderer == null || _parentRenderer == null) return;

        var matCount = _outlineRenderer.sharedMaterials.Length;
        var outlineMats = new Material[matCount];
        for (int i = 0; i < matCount; i++) outlineMats[i] = _outlineMaterial;
        _outlineRenderer.materials = outlineMats;

        // 同步 Mesh
        if (_parentRenderer is SkinnedMeshRenderer parentSkinned &&
            _outlineRenderer is SkinnedMeshRenderer outlineSkinned)
        {
            outlineSkinned.sharedMesh = parentSkinned.sharedMesh;
            outlineSkinned.bones = parentSkinned.bones;
            outlineSkinned.rootBone = parentSkinned.rootBone;
        }
        else if (_parentRenderer is MeshRenderer)
        {
            var parentFilter = GetComponent<MeshFilter>();
            var outlineFilter = _outlineChild.GetComponent<MeshFilter>();
            if (parentFilter != null && outlineFilter != null)
            {
                outlineFilter.sharedMesh = parentFilter.sharedMesh;
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Find All Inactive Objects")]
    void FindAllInactiveObjects()
    {
        Debug.Log("=== 开始查找所有未激活的物体 ===");

        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);

        int inactiveCount = 0;
        foreach (GameObject obj in allObjects)
        {
            if (!obj.activeInHierarchy)
            {
                inactiveCount++;
                Debug.Log($"未激活: {obj.name}, 路径: {GetPath(obj.transform)}");
            }
        }

        Debug.Log($"=== 查找完成，共找到 {inactiveCount} 个未激活的物体 ===");
    }
#endif

    string GetPath(Transform t)
    {
        if (t.parent == null)
            return t.name;
        return GetPath(t.parent) + "/" + t.name;
    }
}
