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
    public Vector2 lableHiddenPosition = new Vector2(0, 125);
    public Vector2 labelShownPosition = new Vector2(0, -350);

    [Header("Launch settings")]
    public float overlayScreenFadeDuration;
    public float overlayScreenGameLaunchDelayDuration;

    [Header("In-game UI")]
    public TMPro.TextMeshProUGUI moneyLabel;
    public Color defaultMoneyLabelColor;
    public Color highlightedMoneyLabelColor;

    [Header("Hand tip animation")]
    public GameObject handAnimationUIGroup;
    public RectTransform hand;
    public RectTransform handFirstPosition;
    public RectTransform handSecondPosition;
    [Range(0, 5)] public float handHalfCycleAnimationDuration;

    [Header("Retry screen")]
    public RectTransform levelFailedText;
    public Button retryButton;
    public GameObject levelFailedUIGroup;

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

    [Header("Settings UI settings")]
    public RectTransform _settingsButton;
    public RectTransform _fpsToggle;
    public RectTransform _powerSavingToggle;
    public RectTransform _resetButton;

    [Header("FPS display")]
    [SerializeField] [Range(0.001f, 2f)] private float _refreshInterval = 0.5f;
    [SerializeField] private TMPro.TextMeshProUGUI fpsLabel = null;
    [SerializeField] private FPSCounter fPSCounter = null;

    [Header("Progress reset warning")]
    [SerializeField] private Graphic _clickableUnderlay;
    [SerializeField] private RectTransform _warningObject;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _confirmButton;

    private Sequence handAnimation;
    private Sequence levelFailedAnimation;
    private Sequence levelRetryAnimationStart;
    private Sequence levelReadyAnimation;
    private Sequence levelCompletedAnimation;
    private Sequence nextLevelStartAnimation;
    private Sequence fpsRefresher;
    private Sequence _settingsExpandAnimation;
    private Sequence _settingsCollapseAnimation;
    private Sequence _progressResetWarningExpandAnimation;
    private Sequence _progressResetWarningCollapseAnimation;
    private Sequence _moneyLabelHighlightAnimation;
    private Sequence _moneyLabelAnimationIn;
    private Sequence _moneyLabelAnimationOut;

    private ComponentPool<RectTransform> moneyPickupObjectsPool;
    private int _currentlyDisplayedMoney;
    private bool _settingsExpanded = false;

    public void SetMoneyText(int amount)
	{
        _currentlyDisplayedMoney = amount;
        moneyLabel.text = _currentlyDisplayedMoney.ToString();
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
                SetMoneyText(_currentlyDisplayedMoney + amount);
                _moneyLabelHighlightAnimation.Restart();
            }).SetAutoKill(true).Play();
    }

    public void AnimateMoneyText(int amount)
    {
        _moneyLabelAnimationIn.Restart();

        DOTween.To(() => { return _currentlyDisplayedMoney; }, x => _currentlyDisplayedMoney = x, amount, 2)
            .SetEase(Ease.OutCubic)
            .SetAutoKill()
            .OnUpdate(() => moneyLabel.text = _currentlyDisplayedMoney.ToString())
            .OnComplete(() => _moneyLabelAnimationOut.Restart())
            .Play();
    }

    public void LevelStartAnimation()
	{
        handAnimationUIGroup.SetActive(false);
        handAnimation.Pause();
    }

    public void LevelFailedAnimation()
	{
        levelFailedAnimation.Restart();
	}

    public void LevelRetryAnimation(TweenCallback levelLoader)
	{
        levelRetryAnimationStart.OnComplete(levelLoader).Restart();
	}

    public void LevelReadyAnimation()
	{
        levelReadyAnimation.Restart();
    }

    public void LevelCompletedAnimation(int multiplier, int collectedMoney, TweenCallback action)
	{
        multiplierLabel.text = $"x{multiplier}";
        collectedMoneyLabel.text = collectedMoney.ToString();
        levelCompletedAnimation.OnComplete(action).Restart();
	}

    public void NextLevelAnimation(TweenCallback levelLoader)
    {
        nextLevelStartAnimation.OnComplete(levelLoader).Restart();
    }

    public void SetGraphicsAlpha(Graphic graphic, float alpha)
	{
        Color oldColor = graphic.color;
        graphic.color = new Color(oldColor.r, oldColor.g, oldColor.b, alpha);
	}

    private void SetFpsCounterActive(bool value)
	{
        if (value)
            fpsRefresher.Play();
        else
            fpsRefresher.Pause();

        fpsLabel.gameObject.SetActive(value);
    }

    public void OnSettingsPressed()
	{
        if (_settingsExpanded)
        {
            _settingsExpandAnimation.Pause();
            _settingsCollapseAnimation.Restart();
        }
        else
        {
            _settingsCollapseAnimation.Pause();
            _settingsExpandAnimation.Restart();
        }

        _settingsExpanded = !_settingsExpanded;
	}

    private void SettingButtonInit(RectTransform target)
	{
        target.localScale = Vector3.zero;
        target.gameObject.SetActive(false);
        target.anchoredPosition = _settingsButton.anchoredPosition;
	}
    
    public void ShowResetProgressWarning()
	{
        _progressResetWarningExpandAnimation.Restart();
	}

    public void CloseProgressResetWarning()
	{
        _progressResetWarningCollapseAnimation.Restart();
	}

    public void ResetProgressAnimation(TweenCallback resetter)
	{
        overlayScreen.gameObject.SetActive(true);
        _confirmButton.interactable = false;
        _cancelButton.interactable = false;
        _settingsExpanded = false;
        bool isFpsCounterActive = fpsRefresher.IsPlaying();
        DOTween.PauseAll();
        SetFpsCounterActive(isFpsCounterActive);

        DOTween.Sequence()
            .AppendCallback(CloseProgressResetWarning)
            .AppendInterval(1f)
            .Append(overlayScreen.DOFade(1, 0.5f))
            .AppendCallback(() => cameraConfetti.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear))
            .AppendCallback(ResetUI)
            .AppendCallback(resetter)
            .AppendCallback(LevelReadyAnimation)
            .SetAutoKill()
            .Play();
	}

    private void ResetUI()
	{
        underlayScreen.gameObject.SetActive(false);
        _clickableUnderlay.gameObject.SetActive(false);
        levelCompleteUIGroup.SetActive(false);
        levelFailedUIGroup.SetActive(false);

        hand.anchoredPosition = handFirstPosition.anchoredPosition;
        levelFailedText.anchoredPosition = lableHiddenPosition;
        _warningObject.localScale = Vector3.zero;
        retryButton.interactable = false;
        _cancelButton.interactable = false;
        _confirmButton.interactable = false;
        retryButton.transform.localScale = Vector2.zero;
        SettingButtonInit(_fpsToggle);
        SettingButtonInit(_powerSavingToggle);
        SettingButtonInit(_resetButton);
        SetGraphicsAlpha(_clickableUnderlay, 0);
	}

	private void Start()
	{
        Toggle fpsToggle = _fpsToggle.GetComponent<Toggle>();
        Toggle powerSavingToggle = _powerSavingToggle.GetComponent<Toggle>();
        fpsToggle.SetIsOnWithoutNotify(SettingsManager.DisplayFps);
        powerSavingToggle.SetIsOnWithoutNotify(SettingsManager.PowerSaver);

        fpsToggle.onValueChanged.Invoke(fpsToggle.isOn);
        powerSavingToggle.onValueChanged.Invoke(powerSavingToggle.isOn);
    }

	private void Awake()
    {
        // Initialization 
        DOTween.Init(false, true, LogBehaviour.Default);
        DOTween.defaultAutoKill = false;
        DOTween.defaultAutoPlay = AutoPlay.None;

        moneyPickupObjectsPool = new ComponentPool<RectTransform>(moneyPrefab, moneyPickupObjectsPoolCapacity);

        SettingsManager.DisplayFpsChanged += SetFpsCounterActive;

        // Setting all UI to the initial states
        ResetUI();

        // Setting UI for game launch animation
        overlayScreen.gameObject.SetActive(true);
        SetGraphicsAlpha(overlayScreen, 1);

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
                levelFailedUIGroup.SetActive(true);
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
                levelFailedUIGroup.SetActive(false);
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

        // Settings expand animation setup
        _settingsExpandAnimation = DOTween.Sequence()
            .AppendCallback(() => 
            {
                _fpsToggle.gameObject.SetActive(true);
                _powerSavingToggle.gameObject.SetActive(true);
                _resetButton.gameObject.SetActive(true);
            })
            .Insert(0f, _settingsButton.DOLocalRotate(new Vector3(0, 0, 90), 0.7f, RotateMode.Fast).SetEase(Ease.InOutBack))
            .Insert(0f, _fpsToggle.DOScale(1, 0.5f))
            .Insert(0f, _fpsToggle.DOAnchorPosY(_settingsButton.anchoredPosition.y - 110, 0.3f).SetEase(Ease.OutBack))
            .Insert(0.1f, _powerSavingToggle.DOScale(1, 0.5f))
            .Insert(0.1f, _powerSavingToggle.DOAnchorPosY(_settingsButton.anchoredPosition.y - 220, 0.3f).SetEase(Ease.OutBack))
            .Insert(0.2f, _resetButton.DOScale(1, 0.5f))
            .Insert(0.2f, _resetButton.DOAnchorPosY(_settingsButton.anchoredPosition.y - 330, 0.3f).SetEase(Ease.OutBack));
        // Settings collapse animation setup
        _settingsCollapseAnimation = DOTween.Sequence()
            .Insert(0f, _settingsButton.DOLocalRotate(new Vector3(0, 0, 0), 0.7f, RotateMode.Fast).SetEase(Ease.InOutBack))
            .Insert(0.2f, _fpsToggle.DOScale(0, 0.5f))
            .Insert(0.2f, _fpsToggle.DOAnchorPosY(_settingsButton.anchoredPosition.y, 0.3f).SetEase(Ease.InBack))
            .Insert(0.1f, _powerSavingToggle.DOScale(0, 0.5f))
            .Insert(0.1f, _powerSavingToggle.DOAnchorPosY(_settingsButton.anchoredPosition.y, 0.3f).SetEase(Ease.InBack))
            .Insert(0f, _resetButton.DOScale(0, 0.5f))
            .Insert(0f, _resetButton.DOAnchorPosY(_settingsButton.anchoredPosition.y, 0.3f).SetEase(Ease.InBack))
            .AppendCallback(() =>
            {
                _fpsToggle.gameObject.SetActive(false);
                _powerSavingToggle.gameObject.SetActive(false);
                _resetButton.gameObject.SetActive(false);
            });

        // Progress reset warning expand animation setup
        _progressResetWarningExpandAnimation = DOTween.Sequence()
            .AppendCallback(() => { _clickableUnderlay.gameObject.SetActive(true); })
            .Join(_clickableUnderlay.DOFade(0.5f, 0.2f))
            .Join(_warningObject.DOScale(1, 0.2f).SetEase(Ease.OutBack))
            .AppendCallback(() =>
            {
                _cancelButton.interactable = true;
                _confirmButton.interactable = true;
            });

        // Progress reset cancel animation setup
        _progressResetWarningCollapseAnimation = DOTween.Sequence()
            .AppendCallback(() =>
            {
                _cancelButton.interactable = false;
                _confirmButton.interactable = false;
            })
            .Join(_clickableUnderlay.DOFade(0, 0.2f))
            .Join(_warningObject.DOScaleY(0, 0.1f))
            .AppendCallback(() => _clickableUnderlay.gameObject.SetActive(false))
            .Append(_warningObject.DOScaleX(0, 0.01f));

        // Money label highlight animation setup
        _moneyLabelAnimationIn = DOTween.Sequence()
            .Join(moneyLabel.rectTransform.DOScale(1.2f, 0.1f))
            .Join(moneyLabel.DOColor(highlightedMoneyLabelColor, 0.1f));

        // Money label fade out animation setup
        _moneyLabelAnimationOut = DOTween.Sequence()
            .Join(moneyLabel.rectTransform.DOScale(1f, 0.2f))
            .Join(moneyLabel.DOColor(defaultMoneyLabelColor, 0.2f));

        // Money label highlight + fade out animation setup
        _moneyLabelHighlightAnimation = DOTween.Sequence()
            .Append(moneyLabel.rectTransform.DOScale(1.2f, 0.1f))
            .Join(moneyLabel.DOColor(highlightedMoneyLabelColor, 0.1f))
            .Append(moneyLabel.rectTransform.DOScale(1f, 0.2f))
            .Join(moneyLabel.DOColor(defaultMoneyLabelColor, 0.2f));


        // FPS refresher setup
        fpsRefresher = DOTween.Sequence()
            .AppendCallback(() => { fpsLabel.text = $"{fPSCounter.CurrentFps: 0.00} FPS"; })
            .AppendInterval(_refreshInterval)
            .SetLoops(-1);
    }
}