using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeSceneManager : MonoBehaviour
{
    [SerializeField] UserDataManager userDataManager;
    [SerializeField] HomeSceneUIManager homeSceneUIManager;
    [SerializeField] BuildManager buildManager;
    [SerializeField] EventControlCenter eventControllCenter;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        userDataManager.InitCheck();
    }

    // Start is called before the first frame update
    async void Start()
    {
        UserDataManager.LoadNextLevel();
        buildManager.LoadScene(UserDataManager.CurrentSceneID);
        await UniTask.WaitUntil(() => (UserDataManager.HomeSceneSwitchFlag && UserDataManager.BuildSceneLoadComplete));
        homeSceneUIManager.InitMe();
        eventControllCenter.EventInit();
        ButtonBase.ResetLockLevel();
        await homeSceneUIManager.HomeSceneUIStartCheck();
        await eventControllCenter.EventStartCheck();
        homeSceneUIManager.OtherStartCheck();
    }
}
