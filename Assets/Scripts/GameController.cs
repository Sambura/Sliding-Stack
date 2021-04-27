using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public int money = 0;
    public int initialCubes = 1;
    public Animator uiAnimator;
    public Player player;
    public GameObject[] levels;
    public TMPro.TextMeshProUGUI moneyLabel;
    public TMPro.TextMeshProUGUI collectedMoneyLabel;
    public TMPro.TextMeshProUGUI multiplierLabel;
    public GameObject moneyCollectedPrefab;
    public Transform canvas;
    public Transform moneyIcon;
    public int currentLevel;
    public float moneyPickupEffectSmoothing = 0.2f;
    public ParticleSystem cameraConfetti;

    private GameObject levelInstance;
    private int collectedMoney = 0;
    private int moneyCoroutinesRunning = 0;

	private void Start()
	{
        LoadProgress();
        moneyLabel.text = money.ToString();
        player.enabled = false;
        player.Death += GameOver;
        player.Completion += LevelCompleted;
        player.MoneyPickedUp += (x => StartCoroutine(MoneyCollectedAnimation(x)));
        levelInstance = Instantiate(levels[currentLevel]);
        player.InitPlayer(levelInstance.GetComponent<LevelController>(), initialCubes);
	}

    private void LoadProgress()
	{
        money = PlayerPrefs.GetInt("MoneyCount", 0);
        initialCubes = PlayerPrefs.GetInt("InitialCubesCount", 1);
#if !UNITY_EDITOR
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
#endif
    }

    private void SaveProgress()
	{
        PlayerPrefs.SetInt("MoneyCount", money);
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("InitialCubesCount", initialCubes);

        PlayerPrefs.Save();
	}

    private IEnumerator MoneyCollectedAnimation(Vector3 location)
	{
        moneyCoroutinesRunning++;
        Vector3 velocity = Vector3.zero;
        Vector3 canvasPosition = Camera.main.WorldToScreenPoint(location);
        GameObject ui = Instantiate(moneyCollectedPrefab, moneyCollectedPrefab.transform.parent);
        ui.transform.position = canvasPosition;
        ui.SetActive(true);

        while (Vector3.Distance(ui.transform.position, moneyIcon.position) > 2.5f)
        {
            ui.transform.position = Vector3.SmoothDamp(ui.transform.position, 
                                                       moneyIcon.position, 
                                                       ref velocity, 
                                                       moneyPickupEffectSmoothing);
            yield return new WaitForEndOfFrame();
        }

        Destroy(ui);

        moneyLabel.text = (money + ++collectedMoney).ToString();
        moneyCoroutinesRunning--;
    }

    public void GameStart()
	{
        uiAnimator.SetTrigger("GameStart");
        player.enabled = true;
	}

    private void GameOver()
	{
        player.enabled = false;
        uiAnimator.SetTrigger("GameOver");
    }

    private void LevelCompleted(int multiplier)
	{
        StartCoroutine(LevelCompletedAsync(multiplier));
    }

    private IEnumerator LevelCompletedAsync(int multiplier)
	{
        yield return new WaitUntil(() => moneyCoroutinesRunning == 0);
        uiAnimator.SetTrigger("LevelComplete");
        collectedMoney *= multiplier;
        collectedMoneyLabel.text = collectedMoney.ToString();
        multiplierLabel.text = "×" + multiplier.ToString();

        money += collectedMoney;
        collectedMoney = 0;
        moneyLabel.text = money.ToString();

        currentLevel = (currentLevel + 1) % levels.Length;
        SaveProgress();
	}

    public void PlayConfetti()
	{
        cameraConfetti.Play();
	}

    public void RetryStart()
	{
        uiAnimator.SetTrigger("Retry");
    }

    public void RetryEnd()
	{
        levelInstance.SetActive(false);
        Destroy(levelInstance);
        levelInstance = Instantiate(levels[currentLevel]);
        player.InitPlayer(levelInstance.GetComponent<LevelController>(), initialCubes);
        moneyLabel.text = money.ToString();
        cameraConfetti.Stop();

        uiAnimator.SetTrigger("RetryEnd");
    }

    public void NextLevelStart()
	{
        uiAnimator.SetTrigger("NextLevel");
    }

    public void NextLevelEnd()
    {
        levelInstance.SetActive(false);
        Destroy(levelInstance);
        levelInstance = Instantiate(levels[currentLevel]);
        player.InitPlayer(levelInstance.GetComponent<LevelController>(), initialCubes);
        collectedMoney = 0;
        moneyLabel.text = money.ToString();
        cameraConfetti.Clear();

        uiAnimator.SetTrigger("RetryEnd");
    }
}
