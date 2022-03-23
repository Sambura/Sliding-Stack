using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleSpriteChanger : MonoBehaviour
{
    [SerializeField] private Toggle _targetToggle;
    [SerializeField] private Image _targetImage;
    [SerializeField] private Sprite _spriteOn;
    [SerializeField] private Sprite _spriteOff;

    private void Start()
    {
        _targetToggle.onValueChanged.AddListener((x) => UpdateSprite(x));

        UpdateSprite(_targetToggle.isOn);
    }

    private void UpdateSprite(bool isOn)
	{
        if (isOn)
            _targetImage.sprite = _spriteOn;
        else 
            _targetImage.sprite = _spriteOff;
    }
}
