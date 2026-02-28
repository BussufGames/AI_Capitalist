/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * Central service for UI management, global visual assets, and Tier UI spawning.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2026-02-28 - Cached OfflineProgressManager to prevent OnDestroy errors during Prestige.
 * 2026-02-28 - Restored original SceneLoaded Cross-Scene Architecture.
 * ----------------------------------------------------------------------------
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using AI_Capitalist.Core;
using AI_Capitalist.Gameplay;
using AI_Capitalist.Economy;
using AI_Capitalist.Data;
using BussufGames.Core;

namespace AI_Capitalist.UI
{
	public class UIManager : MonoBehaviour, IService
	{
		#region Global Assets
		[Header("Global Manager State Icons")]
		public Sprite StateManual;
		public Sprite StateHumanWorking;
		public Sprite StateHumanStrike;
		public Sprite StateAIRunning;

		[Header("Global Upgrade Icons")]
		public Sprite GlobalBonusIcon;
		public Sprite TimeUpgradeIcon;
		public Sprite RevenueUpgradeIcon;
		#endregion

		#region Prefabs
		[Header("Prefabs")]
		[SerializeField] private GameObject tierUIPrefab;
		[SerializeField] private GameObject managerPopupPrefab;
		[SerializeField] private GameObject offlinePopupPrefab;
		[SerializeField] private GameObject unlockPanelPrefab;
		#endregion

		#region Fields
		private Transform _uiContainer;
		private ManagerPopupUI _activeManagerPopup;
		private UnlockPanelUI _currentUnlockPanel;

		private TierManager _tierManager;
		private EconomyManager _economyManager;
		private OfflineProgressManager _offlineManager; // Cached reference to avoid OnDestroy errors

		private Dictionary<int, TierVisualData> _visualDataDict = new Dictionary<int, TierVisualData>();
		#endregion

		private void Awake()
		{
			if (CoreManager.Instance != null)
				CoreManager.Instance.RegisterService<UIManager>(this);
		}

		public void Initialize()
		{
			_tierManager = CoreManager.Instance.GetService<TierManager>();
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();
			_offlineManager = CoreManager.Instance.GetService<OfflineProgressManager>(); // Cache it!

			if (_tierManager == null || tierUIPrefab == null || unlockPanelPrefab == null || managerPopupPrefab == null)
			{
				this.LogError("UIManager missing critical references!");
				return;
			}

			LoadVisualData();

			SceneManager.sceneLoaded += OnSceneLoaded;
			_tierManager.OnTierUnlocked += HandleNewTierUnlocked;

			if (_offlineManager != null)
			{
				_offlineManager.OnOfflineProgressCalculated += SpawnOfflinePopup;
			}
		}

		private void OnDestroy()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;

			if (_tierManager != null)
			{
				_tierManager.OnTierUnlocked -= HandleNewTierUnlocked;
			}

			// Clean unsubscribe without asking CoreManager
			if (_offlineManager != null)
			{
				_offlineManager.OnOfflineProgressCalculated -= SpawnOfflinePopup;
			}
		}

		private void LoadVisualData()
		{
			_visualDataDict.Clear();
			TierVisualData[] loadedData = Resources.LoadAll<TierVisualData>("TierVisuals");
			foreach (var data in loadedData)
				_visualDataDict[data.TierID] = data;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (scene.buildIndex == 1) SpawnUI();
		}

		private void SpawnUI()
		{
			GameObject containerObj = GameObject.Find("UI_Container");
			if (containerObj != null) _uiContainer = containerObj.transform;
			else return;

			foreach (var controller in _tierManager.ActiveTiers)
			{
				SpawnSingleTierUI(controller, false);
			}

			SpawnUnlockPanelForNextTier();

			Canvas mainCanvas = Object.FindAnyObjectByType<Canvas>();
			if (mainCanvas != null)
			{
				GameObject popupObj = Instantiate(managerPopupPrefab, mainCanvas.transform);
				popupObj.name = "Manager_Popup_UI";
				popupObj.transform.SetAsLastSibling();
				_activeManagerPopup = popupObj.GetComponent<ManagerPopupUI>();
				_activeManagerPopup.gameObject.SetActive(false);

				if (_offlineManager != null && _offlineManager.HasOfflineProgress)
				{
					SpawnOfflinePopup(_offlineManager.PendingTimeAway, _offlineManager.PendingOfflineEarnings);
				}
			}
		}

		private void SpawnOfflinePopup(System.TimeSpan timeAway, BreakInfinity.BigDouble earnings)
		{
			Canvas mainCanvas = Object.FindAnyObjectByType<Canvas>();
			if (mainCanvas != null && offlinePopupPrefab != null)
			{
				GameObject offlineObj = Instantiate(offlinePopupPrefab, mainCanvas.transform);
				offlineObj.name = "Offline_Popup_UI";
				offlineObj.transform.SetAsLastSibling();
				offlineObj.GetComponent<OfflinePopupUI>()?.OpenPopup(timeAway, earnings);
			}
		}

		private void SpawnSingleTierUI(TierController controller, bool playPopAnimation)
		{
			GameObject uiObj = Instantiate(tierUIPrefab, _uiContainer);
			uiObj.name = $"UI_{controller.gameObject.name}";
			uiObj.transform.SetAsLastSibling();

			TierUIView uiView = uiObj.GetComponent<TierUIView>();
			if (uiView != null)
			{
				_visualDataDict.TryGetValue(controller.StaticData.TierID, out TierVisualData visualData);
				uiView.Initialize(controller, visualData);
			}

			if (playPopAnimation)
			{
				uiObj.transform.localScale = Vector3.zero;
				uiObj.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
			}
		}

		private void SpawnUnlockPanelForNextTier()
		{
			var dataManager = CoreManager.Instance.GetService<DataManager>();
			if (dataManager == null || _economyManager == null) return;

			int nextTierID = dataManager.GameData.HighestUnlockedTier + 1;

			if (_economyManager.TryGetTierConfig(nextTierID, out TierStaticData nextConfig))
			{
				GameObject unlockObj = Instantiate(unlockPanelPrefab, _uiContainer);
				unlockObj.name = $"UnlockPanel_Tier_{nextTierID}";
				unlockObj.transform.SetAsLastSibling();

				_currentUnlockPanel = unlockObj.GetComponent<UnlockPanelUI>();
				_currentUnlockPanel.Initialize(nextConfig);
			}
		}

		private void HandleNewTierUnlocked(TierController newController)
		{
			if (_currentUnlockPanel != null)
			{
				_currentUnlockPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
				{
					Destroy(_currentUnlockPanel.gameObject);
					_currentUnlockPanel = null;

					SpawnSingleTierUI(newController, true);
					SpawnUnlockPanelForNextTier();
				});
			}
		}

		public TierVisualData GetVisualDataForTier(int tierID)
		{
			if (_visualDataDict.TryGetValue(tierID, out var data))
			{
				return data;
			}
			return null;
		}

		public void OpenManagerPopup(TierController controller)
		{
			if (_activeManagerPopup != null)
			{
				_visualDataDict.TryGetValue(controller.StaticData.TierID, out TierVisualData visualData);
				_activeManagerPopup.OpenPopup(controller, visualData);
			}
			else
			{
				Debug.LogWarning("UIManager: Popup is null. Was it destroyed?");
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------