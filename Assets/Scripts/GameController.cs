using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public int initialCubesCount = 1;
    public Player player;
    public GameObject[] levels;
    public int currentLevel;
    public UIController uIController;
    public int targerFrameRate = 50;
    public int powerSavingFrameRate = 40;

    private LevelController _levelController;
    private int collectedMoney = 0;
    private int money;

	private void Awake()
	{
		SettingsManager.PowerSaverChanged += OnPowerSavingChanged;
    }

	private void Start()
	{
        Physics.autoSimulation = false;
        player.enabled = false;
        _levelController = new LevelController();
        
        player.Death += GameOver;
        player.Completion += LevelCompleted;
        player.MoneyPickedUp += OnMoneyCollected;

        LoadProgress();
        LoadLevel();
	}

    private void LoadProgress()
	{
        money = PlayerPrefs.GetInt("MoneyCount", 0);
        initialCubesCount = PlayerPrefs.GetInt("InitialCubesCount", 1);
#if !UNITY_EDITOR
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
#endif
    }

    private void SaveProgress()
	{
        PlayerPrefs.SetInt("MoneyCount", money);
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("InitialCubesCount", initialCubesCount);

        PlayerPrefs.Save();
	}

    public void GameStart()
	{
        InputManager.PointerDown -= GameStart;
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
        collectedMoney *= multiplier;
        money += collectedMoney;
        uIController.LevelCompletedAnimation(multiplier, collectedMoney, () => { if (multiplier > 1) uIController.AnimateMoneyText(money); });
        collectedMoney = 0;
        currentLevel = (currentLevel + 1) % levels.Length;
        SaveProgress();
    }

    private void LoadLevel()
    {
        // Instantiate new level
        if (_levelController.LevelInstance != null) 
            Destroy(_levelController.LevelInstance); // For initial level creation
        _levelController.LevelInstance = Instantiate(levels[currentLevel]);

        // Inititalize player
        player.InitPlayer(_levelController, initialCubesCount);

        collectedMoney = 0; // Reset collected money

        // Set up UI and enable animation
        uIController.SetMoneyText(money);
        uIController.LevelReadyAnimation();
        
        // Subscribe to PointerDown event that triggers game start
        InputManager.PointerDown += GameStart;
    }

    public void ResetProgress()
	{
        player.enabled = false;
        money = 0;
        currentLevel = 0;
        initialCubesCount = 1;

        SaveProgress();
        uIController.ResetProgressAnimation(() =>
        {
            InputManager.PointerDown -= GameStart;
            LoadLevel();
        });
	}

    public void Retry() => uIController.LevelRetryAnimation(LoadLevel);
    public void Continue() => uIController.NextLevelAnimation(LoadLevel);
    private void OnPowerSavingChanged(bool value) => Application.targetFrameRate = value ? powerSavingFrameRate : targerFrameRate;
    private void OnMoneyCollected(Vector3 position)
    {
        uIController.MoneyPickupAnimation(position, 1);
        collectedMoney++;
    }

	private void OnDestroy()
	{
        player.Death -= GameOver;
        player.Completion -= LevelCompleted;
        player.MoneyPickedUp -= OnMoneyCollected;
    }
}
