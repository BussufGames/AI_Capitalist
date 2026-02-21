/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * Generates the UI Prefabs for each Tier and links them to the Logic Controllers.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * ----------------------------------------------------------------------------
 */

using System.Collections.Generic;
using UnityEngine;
using AI_Capitalist.Core;
using AI_Capitalist.Gameplay;
using BussufGames.Core;

namespace AI_Capitalist.UI
{
	public class UIManager : MonoBehaviour, IService
	{
		[Header("Prefabs & Containers")]
		[Tooltip("The visual UI Prefab with TierUIView script.")]
		[SerializeField] private GameObject tierUIPrefab;
		[Tooltip("The Canvas layout group where UI panels will spawn.")]
		[SerializeField] private Transform uiContainer;

		private TierManager _tierManager;
		private Dictionary<int, TierVisualData> _visualDataDict = new Dictionary<int, TierVisualData>();

		private void Awake()
		{
			if (CoreManager.Instance != null)
			{
				CoreManager.Instance.RegisterService<UIManager>(this);
			}
		}

		public void Initialize()
		{
			this.Log("Initializing UIManager...");

			_tierManager = CoreManager.Instance.GetService<TierManager>();
			if (_tierManager == null)
			{
				this.LogError("TierManager not found. UI cannot initialize.");
				return;
			}

			if (tierUIPrefab == null || uiContainer == null)
			{
				this.LogError("TierUIPrefab or UIContainer is missing in UIManager!");
				return;
			}

			LoadVisualData();
			SpawnUI();
		}

		private void LoadVisualData()
		{
			// Load all ScriptableObjects from a Resources folder named "Visuals"
			TierVisualData[] loadedData = Resources.LoadAll<TierVisualData>("Visuals");
			foreach (var data in loadedData)
			{
				_visualDataDict[data.TierID] = data;
			}
			this.Log($"Loaded {_visualDataDict.Count} TierVisualData objects.");
		}

		private void SpawnUI()
		{
			// Find the container dynamically in the loaded Main scene
			GameObject containerObj = GameObject.Find("UI_Container");
			if (containerObj != null)
			{
				uiContainer = containerObj.transform;
			}
			else
			{
				this.LogError("Could not find 'UI_Container' in the scene! Make sure it is named exactly like this.");
				return;
			}

			foreach (var controller in _tierManager.ActiveTiers)
			{
				GameObject uiObj = Instantiate(tierUIPrefab, uiContainer);
				uiObj.name = $"UI_{controller.gameObject.name}";

				TierUIView uiView = uiObj.GetComponent<TierUIView>();
				if (uiView != null)
				{
					_visualDataDict.TryGetValue(controller.StaticData.TierID, out TierVisualData visualData);
					uiView.Initialize(controller, visualData);
				}
				else
				{
					this.LogError("TierUIPrefab is missing the TierUIView script!");
				}
			}

			this.LogSuccess("All Tier UIs spawned and connected to logic.");
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------