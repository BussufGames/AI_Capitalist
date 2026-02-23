/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-31
 * ----------------------------------------------------------------------------
 * Description:
 * The backbone of the application. 
 * Manages the dependency injection and initialization order of all core services.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-31 - Bussuf Senior Dev - Added PrestigeManager to the initialization sequence.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Data;
using AI_Capitalist.Economy;
using AI_Capitalist.Gameplay;
using AI_Capitalist.Services;
using AI_Capitalist.UI;
using BussufGames.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AI_Capitalist.Core
{
	public class CoreManager : MonoBehaviour
	{
		public static CoreManager Instance { get; private set; }

		private Dictionary<Type, IService> _services = new Dictionary<Type, IService>();

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				this.LogWarning("Duplicate CoreManager detected. Destroying...");
				Destroy(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);
			this.Log("CoreManager awoken and set to DontDestroyOnLoad.");
		}

		public void RegisterService<T>(T service) where T : class, IService
		{
			Type type = typeof(T);
			if (!_services.ContainsKey(type))
			{
				_services.Add(type, service);
				this.Log($"Service registered: {type.Name}");
			}
			else
			{
				this.LogError($"Service of type {type.Name} is already registered!");
			}
		}

		public T GetService<T>() where T : class, IService
		{
			Type type = typeof(T);
			if (_services.TryGetValue(type, out IService service))
			{
				return service as T;
			}

			this.LogError($"Service of type {type.Name} not found!");
			return null;
		}

		public void StartGameSequence()
		{
			this.Log("Starting Game Sequence...");

			// Authenticate (Async)
			InitializeService<UGSManager>();

			// Load Data
			InitializeService<DataManager>();

			// Setup Economy
			InitializeService<EconomyManager>();

			// Setup Prestige BEFORE offline progress so multipliers apply correctly
			InitializeService<PrestigeManager>();

			// Calculate offline time
			InitializeService<OfflineProgressManager>();

			// Spawn Tiers
			InitializeService<TierManager>();

			// Spawn UIs
			InitializeService<UIManager>();

			this.LogSuccess("All services initialized successfully.");

			this.Log("Loading Main Scene...");
			SceneManager.LoadScene(1);
		}

		private void InitializeService<T>() where T : class, IService
		{
			T service = GetService<T>();
			if (service != null)
			{
				service.Initialize();
			}
			else
			{
				this.LogError($"Cannot initialize {typeof(T).Name} because it is missing!");
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------