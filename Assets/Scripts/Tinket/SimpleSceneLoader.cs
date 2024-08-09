using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneLoader : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private async void WaitAnimationOver()
    {
        UserDataManager.BuildSceneLoadComplete = false;
        UserDataManager.HomeSceneSwitchFlag = false;
        var op = SceneManager.LoadSceneAsync("HomeScene", LoadSceneMode.Additive);
        await UniTask.WaitUntil(() => 1f <= animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        await UniTask.WaitUntil(() => UserDataManager.BuildSceneLoadComplete);
        await SceneManager.UnloadSceneAsync("LoadScene");
        UserDataManager.HomeSceneSwitchFlag = true;
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    void Start()
    {   
        animator.Play("logo_celebrate");
        WaitAnimationOver();
    }

    
}
