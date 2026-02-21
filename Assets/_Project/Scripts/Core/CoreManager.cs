/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * The Service Locator and main entry point of the application.
 * Resides in the Bootstrap scene, registers all IServices, initializes them
 * in a STRICT order (UGS -> Data -> Economy), and transitions to Main.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-28 - Bussuf Senior Dev - Enforced strict initialization order to prevent NRE.
 * 2023-10-28 - Bussuf Senior Dev - Added OfflineProgressManager to sequence.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Data;
using AI_Capitalist.Economy;
using AI_Capitalist.Services;
using BussufGames.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AI_Capitalist.Core
{
	[DefaultExecutionOrder(-100)]
	public class CoreManager : MonoBehaviour
	{
		public static CoreManager Instance { get; private set; }

		private readonly Dictionary<Type, IService> _services = new Dictionary<Type, IService>();

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

		public void RegisterService<T>(T service) where T : IService
		{
			var type = typeof(T);
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
			var type = typeof(T);
			if (_services.TryGetValue(type, out var service))
			{
				return service as T;
			}

			this.LogError($"Requested Service of type {type.Name} was not found! Did you register it?");
			return null;
		}

		public void StartGameSequence()
		{
			this.Log("Starting Game Sequence...");

			// STRICT INITIALIZATION ORDER:
			// 1. Cloud & Auth
			InitializeService<UGSManager>();

			// 2. Data
			InitializeService<DataManager>();

			// 3. Economy
			InitializeService<EconomyManager>();

			// 4. Offline Progress (Requires Data & Economy)
			InitializeService<OfflineProgressManager>();

			this.LogSuccess("All services initialized successfully.");

			// 5. Load the Main Game Scene
			this.Log("Loading Main Scene...");
			SceneManager.LoadScene(1);
		}

		private void InitializeService<T>() where T : class, IService
		{
			var service = GetService<T>();
			if (service != null)
			{
				service.Initialize();
			}
			else
			{
				this.LogError($"CRITICAL: Service {typeof(T).Name} is missing from the Bootstrap sequence!");
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// -----------------------------------------------------------------------------------------------------