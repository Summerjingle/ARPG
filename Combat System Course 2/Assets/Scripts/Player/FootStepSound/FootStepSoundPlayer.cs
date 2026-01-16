using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootStepSoundPlayer : MonoBehaviour
{
    
    public AudioClip[] defaultClips;
    public AudioClip[] woodClips;
    public AudioClip[] rockClips;
    public Animator animator;
    private float _lastFootStep;

    public LayerMask Environment;
    private void OnValidate()
    {
        if (!animator)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        var footstep = animator.GetFloat("FootStep");

        var clips = GetClipsForSurface();


        if (Mathf.Abs(footstep) < 0.0001f) footstep = 0f;

        if (_lastFootStep > 0 && footstep < 0 || _lastFootStep < 0 && footstep > 0)
        {
            var randomClips = clips[Random.Range(0, clips.Length - 1)];
            AudioSource.PlayClipAtPoint(randomClips, transform.position);
        }
        _lastFootStep = footstep;
    }

    private AudioClip[] GetClipsForSurface()
    {
        var ishit = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 0.25f, Environment);
        if (ishit)
        {
            var surface = hit.collider.GetComponent<SurfaceDefinition>();
            if (surface)
            {
                if (surface.SurfaceType == SurfaceType.Wood) return woodClips;
                if (surface.SurfaceType == SurfaceType.Rock) return rockClips;
            }
        }
        return defaultClips;
    }
}
