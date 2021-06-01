using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public int money = 0;
    public int initialCubes = 1;
    public Player player;
    public GameObject[] levels;
    public int currentLevel;
    public UIController uIController;

    private GameObject levelInstance;
    private int collectedMoney = 0;

	private void Start()
	{
        uIController.GameLaunchAnimation();
        Physics.autoSimulation = false;

        LoadProgress();
        uIController.SetMoneyText(money);
        player.enabled = false;
        player.Death += GameOver;
        player.Completion += LevelCompleted;
        player.MoneyPickedUp += (x) => { uIController.MoneyPickupAnimation(x, 1); collectedMoney++; };
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

    public void GameStart()
	{
        uIController.LevelStartAnimation();
        player.enabled = true;
	}

    private void GameOver()
	{
        player.enabled = false;
        uIController.LevelFailedAnimation();
    }

    private void LevelCompleted(int multiplier)
	{
        StartCoroutine(LevelCompletedAsync(multiplier));
    }

    private IEnumerator LevelCompletedAsync(int multiplier)
	{
        //yield return new WaitUntil(() => moneyCoroutinesRunning == 0);
        yield return null;
        collectedMoney *= multiplier;
        uIController.LevelCompletedAnimation(multiplier, collectedMoney);

        money += collectedMoney;
        collectedMoney = 0;
        uIController.SetMoneyText(money);

        currentLevel = (currentLevel + 1) % levels.Length;
        SaveProgress();
	}

    public void Retry()
	{
        uIController.LevelRetryAnimationStart(() =>
        {
            levelInstance.SetActive(false);
            Destroy(levelInstance);
            levelInstance = Instantiate(levels[currentLevel]);
            player.InitPlayer(levelInstance.GetComponent<LevelController>(), initialCubes);
            uIController.SetMoneyText(money);
            uIController.LevelReadyAnimation();
        });
    }

    public void NextLevelEnd()
    {
        uIController.NextLevelStartAnimation(() =>
        {
            levelInstance.SetActive(false);
            Destroy(levelInstance);
            levelInstance = Instantiate(levels[currentLevel]);
            player.InitPlayer(levelInstance.GetComponent<LevelController>(), initialCubes);
            collectedMoney = 0;
            uIController.SetMoneyText(money);

            uIController.LevelReadyAnimation();
        });
    }
}
