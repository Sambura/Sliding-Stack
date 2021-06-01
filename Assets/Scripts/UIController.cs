using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIController : MonoBehaviour
{
	[Header("General stuff")]
    public new Camera camera;
    public RawImage overlayScreen;
    public RawImage underlayScreen;
    public GameObject gameStartTrigger;
    public Vector2 lableHiddenPosition = new Vector2(0, 125);
    public Vector2 labelShownPosition = new Vector2(0, -350);

    [Header("Launch settings")]
    public float overlayScreenFadeDuration;
    public float overlayScreenGameLaunchDelayDuration;

    [Header("In-game UI")]
    public TMPro.TextMeshProUGUI moneyLabel;

    [Header("Hand tip animation")]
    public GameObject handAnimationUIGroup;
    public RectTransform hand;
    public RectTransform handFirstPosition;
    public RectTransform handSecondPosition;
    [Range(0, 5)] public float handHalfCycleAnimationDuration;

    [Header("Retry screen")]
    public RectTransform levelFailedText;
    public Button retryButton;

    [Header("Next level screen")]
    public RectTransform levelCompletedText;
    public Button continueButton;
    public TMPro.TextMeshProUGUI collectedMoneyLabel;
    public TMPro.TextMeshProUGUI multiplierLabel;
    public GameObject levelCompleteUIGroup;
    public ParticleSystem cameraConfetti;

    [Header("Money pickup settings")]
    public GameObject moneyPrefab;
    public float pickupDuration;
    public RectTransform pickupTargetPosition;
    public int moneyPickupObjectsPoolCapacity = 50;

    [Header("FPS labels")]
    public TMPro.TextMeshProUGUI averageFpsLabel;
    public TMPro.TextMeshProUGUI fpsLabel;
    public FPSCounter fPSCounter;

    private int currentlyDisplayedMoney;
    private Sequence handAnimation;
    private Sequence levelFailedAnimation;
    private Sequence levelRetryAnimationStart;
    private Sequence levelReadyAnimation;
    private Sequence levelCompletedAnimation;
    private Sequence nextLevelStartAnimation;
    private ComponentPool<RectTransform> moneyPickupObjectsPool;

    public void SetMoneyText(int amount)
	{
        currentlyDisplayedMoney = amount;
        moneyLabel.text = currentlyDisplayedMoney.ToString();
    }

    public void MoneyPickupAnimation(Vector3 collectedObject, int amount)
	{
        Vector3 canvasPosition = camera.WorldToScreenPoint(collectedObject);
        RectTransform ui = moneyPickupObjectsPool.GetFromPool();
        ui.position = canvasPosition;
        ui.gameObject.SetActive(true);
        ui.DOAnchorPos(pickupTargetPosition.anchoredPosition, pickupDuration).SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                ui.gameObject.SetActive(false);
                SetMoneyText(currentlyDisplayedMoney + amount);
            }).SetAutoKill(true).SetRecyclable(true).Play();
    }

    public void GameLaunchAnimation()
	{
        overlayScreen.gameObject.SetActive(true);
        overlayScreen.DOFade(0, overlayScreenFadeDuration)
            .SetDelay(overlayScreenGameLaunchDelayDuration)
            .OnComplete(() => {
                overlayScreen.gameObject.SetActive(false);
                gameStartTrigger.SetActive(true);
            })
            .Play();

        handAnimation.Play();
    }

    public void LevelStartAnimation()
	{
        handAnimationUIGroup.SetActive(false);
        handAnimation.Pause();
        gameStartTrigger.SetActive(false);
    }

    public void LevelFailedAnimation()
	{
        levelFailedAnimation.Restart();
	}

    public void LevelRetryAnimationStart(TweenCallback levelLoader)
	{
        levelRetryAnimationStart.OnComplete(levelLoader).Restart();
	}

    public void LevelReadyAnimation()
	{
        levelReadyAnimation.Restart();
        gameStartTrigger.SetActive(true);
    }

    public void LevelCompletedAnimation(int multiplier, int collectedMoney)
	{
        multiplierLabel.text = $"x{multiplier}";
        collectedMoneyLabel.text = collectedMoney.ToString();
        levelCompletedAnimation.Restart();
	}

    public void NextLevelStartAnimation(TweenCallback levelLoader)
    {
        nextLevelStartAnimation.OnComplete(levelLoader).Restart();
    }

    public void SetGraphicsAlpha(Graphic graphic, float alpha)
	{
        Color oldColor = graphic.color;
        graphic.color = new Color(oldColor.r, oldColor.g, oldColor.b, alpha);
	}

    private void Awake()
    {
        // Initialization 
        DOTween.Init(false, true, LogBehaviour.Default);
        DOTween.defaultAutoKill = false;
        DOTween.defaultAutoPlay = AutoPlay.None;

        moneyPickupObjectsPool = new ComponentPool<RectTransform>(moneyPrefab, moneyPickupObjectsPoolCapacity);

        // Setting all UI to the initial states
        hand.anchoredPosition = handFirstPosition.anchoredPosition;
        levelFailedText.anchoredPosition = lableHiddenPosition;
        levelFailedText.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        underlayScreen.gameObject.SetActive(false);
        retryButton.interactable = false;
        overlayScreen.gameObject.SetActive(false);
        retryButton.transform.localScale = Vector2.zero;

        // Hand animation sequence generation
        handAnimation = DOTween.Sequence()
            .AppendCallback(() => handAnimationUIGroup.SetActive(true))
            .Append(hand.DOAnchorPos(handSecondPosition.anchoredPosition, handHalfCycleAnimationDuration).SetEase(Ease.InOutQuad))
            .Append(hand.DOAnchorPos(handFirstPosition.anchoredPosition, handHalfCycleAnimationDuration).SetEase(Ease.InOutQuad))
            .SetLoops(-1);

        // Level failed sequence generation
        levelFailedAnimation = DOTween.Sequence()
            .AppendCallback(() =>
			{
                levelFailedText.transform.localScale = Vector2.one;
                levelFailedText.gameObject.SetActive(true);
                retryButton.gameObject.SetActive(true);
                SetGraphicsAlpha(underlayScreen, 0);
                underlayScreen.gameObject.SetActive(true);
                retryButton.interactable = true;
            })
            .SetDelay(1)
            .Append(levelFailedText.DOAnchorPos(labelShownPosition, 1).SetEase(Ease.OutQuad))
            .Join(underlayScreen.DOFade(0.4f, 1).SetDelay(0.5f))
            .Join(retryButton.transform.DOScale(1, 0.6f).SetEase(Ease.OutBack).SetDelay(0.5f));

        // Level retry start animation sequence generation
        levelRetryAnimationStart = DOTween.Sequence()
            .AppendCallback(() => {
                retryButton.interactable = false;
                overlayScreen.gameObject.SetActive(true);
            })
            .Join(retryButton.transform.DOScale(0, 0.6f).SetEase(Ease.InBack))
            .Join(levelFailedText.DOScale(0, 0.6f).SetEase(Ease.InBack).SetDelay(0.25f))
            .Join(overlayScreen.DOFade(1, 0.6f).SetDelay(0.5f))
            .AppendCallback(() =>
            {
                levelFailedText.gameObject.SetActive(false);
                retryButton.gameObject.SetActive(false);
                underlayScreen.gameObject.SetActive(false);
            });

        // Level retry end animation sequence generation
        levelReadyAnimation = DOTween.Sequence()
            .AppendCallback(() => handAnimation.Restart())
            .Append(overlayScreen.DOFade(0, 0.5f))
            .AppendCallback(() =>
            {
                overlayScreen.gameObject.SetActive(false);
            });

        // Level completed animation sequence generation
        levelCompletedAnimation = DOTween.Sequence()
            .AppendCallback(() =>
            {
                levelCompletedText.transform.localScale = Vector2.one;
                continueButton.transform.localScale = Vector2.zero;
                collectedMoneyLabel.transform.localScale = Vector2.zero;
                multiplierLabel.transform.localScale = Vector2.zero;
                levelCompletedText.anchoredPosition = lableHiddenPosition;
                levelCompleteUIGroup.SetActive(true);
                continueButton.interactable = true;
                SetGraphicsAlpha(underlayScreen, 0);
                underlayScreen.gameObject.SetActive(true);
            })
            .Insert(1.2f, underlayScreen.DOFade(0.4f, 1))
            .Insert(0.7f, levelCompletedText.DOAnchorPos(labelShownPosition, 0.8f).SetEase(Ease.OutQuad))
            .Insert(0.9f, multiplierLabel.transform.DOScale(1, 0.6f).SetEase(Ease.OutBack))
            .Insert(1.3f, collectedMoneyLabel.transform.DOScale(1, 0.6f).SetEase(Ease.OutBack))
            .Insert(1.7f, continueButton.transform.DOScale(1, 0.6f).SetEase(Ease.OutBack))
            .InsertCallback(2.1f, () => cameraConfetti.Play());

        nextLevelStartAnimation = DOTween.Sequence()
            .AppendCallback(() =>
            {
                overlayScreen.gameObject.SetActive(true);
                continueButton.interactable = false;
            })
            .Insert(0.5f, overlayScreen.DOFade(1, 1.2f))
            .Insert(0, continueButton.transform.DOScale(0, 0.6f).SetEase(Ease.InBack))
            .Insert(0.2f, collectedMoneyLabel.transform.DOScale(0, 0.6f).SetEase(Ease.InBack))
            .Insert(0.4f, multiplierLabel.transform.DOScale(0, 0.6f).SetEase(Ease.InBack))
            .Insert(0.6f, levelCompletedText.transform.DOScale(0, 0.6f).SetEase(Ease.InBack))
            .AppendCallback(() =>
            {
                cameraConfetti.Stop();
                cameraConfetti.Clear();
                underlayScreen.gameObject.SetActive(false);
                levelCompleteUIGroup.SetActive(false);
            });

        // FPS refresher setup

        DOTween.Sequence()
            .AppendCallback(() =>
            {
                averageFpsLabel.text = $"{System.Math.Round(fPSCounter.AverageFps, 2)} FPS (avg)";
                fpsLabel.text = $"{System.Math.Round(fPSCounter.CurrentFps, 2)} FPS";
            })
            .AppendInterval(0.05f)
            .SetLoops(-1)
            .Play();
    }
}