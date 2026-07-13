using UnityEngine;

/// <summary>
/// Controls petrification progress with optional radial center→edge spread.
/// Attach to the same GameObject that has the SkinnedMeshRenderer(s).
///
/// Usage:
///   pc.PetrifyOverTime(3f);         // 0→1 center→outward over 3s
///   pc.DePetrifyOverTime(2f);       // 1→0 outward→center over 2s
///   pc.SetProgress(0.5f);           // snap to 50%
/// </summary>
public class PetrificationController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Range(0f, 1f)] private float _progress = 0f;
    [SerializeField] private float _defaultDuration = 2f;

    [Header("Radial Spread")]
    [SerializeField] private bool _useRadial = true;
    [SerializeField] private float _radius = 1.5f;
    [SerializeField, Range(0.01f, 0.5f)] private float _edgeSoftness = 0.05f;

    [Header("Movement Slowdown")]
    [SerializeField] private bool _affectMovement = true;
    [SerializeField] private bool _affectAnimator = true;

    [Header("Debug")]
    [SerializeField] private bool _testPetrify;
    [SerializeField] private bool _testDePetrify;

    // Internal
    private MaterialPropertyBlock _propBlock;
    private SkinnedMeshRenderer[] _renderers;
    private float _targetProgress;
    private float _velocity;
    private Vector3 _centerWorld;

    // Optional components
    private UnityEngine.AI.NavMeshAgent _agent;
    private Animator _animator;
    private float _originalAgentSpeed;
    private float _originalAnimatorSpeed;

    // Shader property IDs (cached)
    private static readonly int PropProgress    = Shader.PropertyToID("_PetrificationProgress");
    private static readonly int PropCenter      = Shader.PropertyToID("_PetrificationCenter");
    private static readonly int PropRadius      = Shader.PropertyToID("_PetrificationRadius");
    private static readonly int PropEdgeSoftness = Shader.PropertyToID("_PetrificationEdgeSoftness");

    // ── Unity Lifecycle ────────────────────────────────────────

    private void Awake()
    {
        _renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        _propBlock = new MaterialPropertyBlock();
        ComputeBounds();
    }

    private void Start()
    {
        TryGetComponent(out _agent);
        TryGetComponent(out _animator);

        if (_agent != null)
            _originalAgentSpeed = _agent.speed;
        if (_animator != null)
            _originalAnimatorSpeed = _animator.speed;

        // Apply initial state
        ApplyProgress();
    }

    private void Update()
    {
        // Recompute center each frame (character may move)
        if (_useRadial)
            ComputeBounds();

#if UNITY_EDITOR
        if (_testPetrify)  { _testPetrify  = false; PetrifyOverTime(_defaultDuration); }
        if (_testDePetrify){ _testDePetrify = false; DePetrifyOverTime(_defaultDuration); }
#endif

        if (!Mathf.Approximately(_progress, _targetProgress))
        {
            float step = _velocity * Time.deltaTime;
            _progress = (_targetProgress > _progress)
                ? Mathf.Min(_progress + step, _targetProgress)
                : Mathf.Max(_progress - step, _targetProgress);

            ApplyProgress();
        }
    }

    // ── Bounds Computation ─────────────────────────────────────

    private void ComputeBounds()
    {
        Bounds combined = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (var r in _renderers)
        {
            if (r == null) continue;
            if (!hasBounds)
            {
                combined = r.bounds;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(r.bounds);
            }
        }

        if (hasBounds)
        {
            _centerWorld = combined.center;
            _radius = combined.extents.magnitude; // diagonal half-length covers the whole mesh
        }
        else
        {
            _centerWorld = transform.position;
            _radius = 1.5f;
        }
    }

    // ── Public API ─────────────────────────────────────────────

    public void SetProgress(float value)
    {
        _progress = Mathf.Clamp01(value);
        _targetProgress = _progress;
        _velocity = 0f;
        ApplyProgress();
    }

    public void PetrifyOverTime(float duration)
    {
        _targetProgress = 1f;
        _velocity = Mathf.Abs(1f - _progress) / Mathf.Max(duration, 0.001f);
    }

    public void DePetrifyOverTime(float duration)
    {
        _targetProgress = 0f;
        _velocity = Mathf.Abs(_progress) / Mathf.Max(duration, 0.001f);
    }

    public float Progress          => _progress;
    public bool  IsFullyPetrified  => _progress >= 0.99f;
    public bool  IsFullyNormal     => _progress <= 0.01f;

    // ── Internal ───────────────────────────────────────────────

    private void ApplyProgress()
    {
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(PropProgress, _progress);

            if (_useRadial)
            {
                _propBlock.SetVector(PropCenter, _centerWorld);
                _propBlock.SetFloat(PropRadius, _radius);
                _propBlock.SetFloat(PropEdgeSoftness, _edgeSoftness);
            }
            else
            {
                _propBlock.SetFloat(PropRadius, 0f); // zero radius = uniform
            }

            r.SetPropertyBlock(_propBlock);
        }

        // Slow down movement / animation
        if (_affectMovement && _agent != null)
            _agent.speed = Mathf.Lerp(_originalAgentSpeed, 0f, _progress);

        if (_affectAnimator && _animator != null)
            _animator.speed = Mathf.Lerp(_originalAnimatorSpeed, 0f, _progress);
    }

    // ── Debug Gizmo ────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!_useRadial) return;

        ComputeBounds();
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(_centerWorld, _radius);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(_centerWorld, _radius * 0.2f);
    }
#endif
}
