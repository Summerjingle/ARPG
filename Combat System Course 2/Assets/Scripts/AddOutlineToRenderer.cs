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

                // 加载存档后，如果交互已经发生过（门已开），立即销毁描边并退订
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

    // 在编辑器中修改属性时自动刷新
    private void OnValidate()
    {
        // 延迟调用避免在 OnValidate 中直接创建/销毁对象导致的警告
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

        // 子物体
        _outlineChild = new GameObject("Outline");
        _outlineChild.transform.SetParent(transform);
        _outlineChild.transform.localPosition = Vector3.zero;
        _outlineChild.transform.localRotation = Quaternion.identity;
        _outlineChild.transform.localScale = Vector3.one;
        _outlineChild.hideFlags = HideFlags.NotEditable; // 防止用户误操作

        // 根据父物体类型复制 Mesh
        if (_parentRenderer is SkinnedMeshRenderer parentSkinned)
        {
            var skinned = _outlineChild.AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = parentSkinned.sharedMesh;
            skinned.bones = parentSkinned.bones;
            skinned.rootBone = parentSkinned.rootBone;
            skinned.quality = parentSkinned.quality;
            skinned.updateWhenOffscreen = parentSkinned.updateWhenOffscreen;
            // 匹配父物体 SubMesh 数量，每个槽位都用 outline 材质
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
            // 需要 MeshFilter
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

            var meshRenderer = _outlineChild.AddComponent<MeshRenderer>();
            // 匹配父物体 SubMesh 数量，每个槽位都用 outline 材质
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

        // 重新赋值 Material（匹配 SubMesh 数量，全部用 outline 材质）
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
}
