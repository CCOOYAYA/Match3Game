using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetMarkTweener : MonoBehaviour
{
    [SerializeField] ParticleSystem particle;

    void Start()
    {
        transform.DOScale(Vector3.one,0.2f);
        particle.Play();
    }
}
