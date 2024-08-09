using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SimpleBuildButton : MonoBehaviour
{
    private Tween zoominTween;

    private Color hidecolor = new Color(1f, 1f, 1f, 0f);

    // Start is called before the first frame update
    void Start()
    {
        zoominTween = transform.DOScale(Vector3.one * 1.08f, 0.15f).SetLoops(2, LoopType.Yoyo).SetAutoKill(false).Pause();
    }

    // Update is called once per frame
    void Update()
    {

    }

    protected void ZoomIn()
    {
        transform.localScale = Vector3.one;
        zoominTween.Restart();
    }

    public void LoadScene(string name)
    {
        SceneManager.LoadSceneAsync(name);
    }
}
