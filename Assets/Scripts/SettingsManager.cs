using UnityEngine;
using System;

class SettingsManager : MonoBehaviour
{
	public static bool DisplayFps
	{
		get { return _displayFps; }
		set
		{
			_displayFps = value;
			DisplayFpsChanged?.Invoke(value);
			PlayerPrefs.SetInt(_displayFpsKey, value ? 1 : 0);
		}
	}
	public static event Action<bool> DisplayFpsChanged;
	[SerializeField] private static bool _displayFps = false;
	private const string _displayFpsKey = "DisplayFps";
	// -----------
	public static bool PowerSaver
	{
		get { return _powerSaver; }
		set
		{
			_powerSaver = value;
			PowerSaverChanged?.Invoke(value);
			PlayerPrefs.SetInt(_powerSaverKey, value ? 1 : 0);
		}
	}
	public static event Action<bool> PowerSaverChanged;
	[SerializeField] private static bool _powerSaver = false;
	private const string _powerSaverKey = "PowerSaver";

	private void Awake()
	{
		_displayFps = PlayerPrefs.GetInt(_displayFpsKey, 0) == 1;
		_powerSaver = PlayerPrefs.GetInt(_powerSaverKey, 0) == 1;
	}
}
