using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace SkyTecGamesTest
{
	public class Player : NetworkBehaviour
	{
		[Inject] LocalPlayerInstalled _localPlayerInstalled;
		[Inject] PlayerScoreChanged _playerScoreChagned;
		[Inject] PlayerEnteredMatch _playerEnteredMatch;
		[Inject] PlayerLeavedMatch _playerLeavedMatch;
		[Inject] PlayerNameChanged _playerNameChanged;
		[Inject] ScoreObjectSpawned _scoreObjectSpawned;
		[Inject] SpaceShip.SpaceShipFactory _spaceshipFactory;
		[Inject] GamePausedFromServer _gamePausedFromServer;
		[Inject] GameResumedFromServer _gameResumedFromServer;
		[Inject] GamePaused _gamePaused;
		[Inject] GameResumed _gameResumed;
		[Inject] Gameplay _gameplay;
		bool _wasInjected;
		bool _pause;
		bool _pauseFlag;
		SpaceShip _spaceShip;

		[SyncVar(hook = "OnNameChanged")] string _name;
		[SyncVar] int _playerId;
		[SyncVar(hook = "OnScoreChanged")] int _score;
		[SyncVar(hook = "OnGunsPowerChanged")] int _gunsPower = 0;
		[SyncVar(hook = "OnFireRateModificatorChanged")] float _fireRateModificator = 1;
		[SyncVar(hook = "OnGunsCountChanged")] int _gunsCount = 1;
		[SyncVar(hook = "OnScoreModificatorChanged")] int _scoreModificator = 1;
		[SyncVar(hook = "OnHasShieldChanged")] bool _hasShield = false;


		public string Name { get { return _name; } }
		public int PlayerId { get { return _playerId; } }
		public int Score { get { return _score; } }
		public int GunsPower { get { return _gunsPower; } }
		public int GunsCount { get { return _gunsCount; } }
		public float FireRateModificator { get { return _fireRateModificator; } }
		public bool HasShield { get { return _hasShield; } }

		[SerializeField] GameObject _spaceShipPrefab;

		[Header("Bonuses")]
		[SerializeField] BonusType _speedBonus;
		[SerializeField] BonusType _gunBonus;
		[SerializeField] BonusType _powerBonus;
		[SerializeField] BonusType _timeBonus;
		[SerializeField] BonusType _shieldBonus;
		[SerializeField] BonusType _scoreBonus;

		[Inject] void Inject()
		{
			_wasInjected = true;
		}

		public override void OnStartServer()
		{
			if (!_wasInjected)
			{
				var context = FindObjectOfType<SceneContext>();
				if (context != null)
				{
					context.Container.Inject(this);
				}
			}

			_fireRateModificator = 1;
			_gunsCount = 1;
			_scoreModificator = 1;
		}

		public override void OnStartClient()
		{
			base.OnStartClient();

			if (!_wasInjected)
			{
				var context = FindObjectOfType<SceneContext>();
				if (context != null)
				{
					context.Container.Inject(this);
				}
			}

			_playerEnteredMatch.Fire(this);
		}

		public override void OnStartLocalPlayer()
		{
			CmdSetName(GameManager.LocalPlayerName);
			_localPlayerInstalled.Fire(this);
			CmdSpawnShip();
		}

		public override void OnNetworkDestroy()
		{
			base.OnNetworkDestroy();

			_playerLeavedMatch.Fire(this);
		}

		void Start()
		{
			_gamePaused += OnPause;
			_gamePausedFromServer += OnPauseFromServer;
			_gameResumed += OnResume;
			_gameResumedFromServer += OnResumeFromServer;
		}

		private void OnDestroy()
		{
			_gamePaused -= OnPause;
			_gamePausedFromServer -= OnPauseFromServer;
			_gameResumed -= OnResume;
			_gameResumedFromServer -= OnResumeFromServer;

			//_playerLeavedMatch.Fire(this);
		}

		[Command] public void CmdRemoveShield()
		{
			_hasShield = false;
		}

		[Command] void CmdSpawnShip()
		{
			_spaceShip = _spaceshipFactory.Create(_spaceShipPrefab, this);
			NetworkServer.SpawnWithClientAuthority(_spaceShip.gameObject, connectionToClient);
			RpcRegisterSpaceShip(_spaceShip.gameObject);
		}

		[ClientRpc] void RpcRegisterSpaceShip(GameObject spaceshipGO)
		{
			_spaceShip = spaceshipGO.GetComponent<SpaceShip>();
		}
		
		public void OnPause()
		{
			CmdPause();
		}

		public void OnResume()
		{
			CmdResume();
		}

		public void OnPauseFromServer()
		{
			if (!_pause || !_pauseFlag)
			{
				_pauseFlag = true;
				_pause = true;
				_gamePausedFromServer.Fire();
			}
		}

		public void OnResumeFromServer()
		{
			if (_pause || !_pauseFlag)
			{
				_pauseFlag = true;
				_pause = false;
				_gameResumedFromServer.Fire();
			}
		}

		[Command] void CmdPause()
		{
			if (!_gameplay.Pause)
			{
				_gamePausedFromServer.Fire();
			}

			RpcPause();
		}

		[ClientRpc] void RpcPause()
		{
			OnPauseFromServer();
		}

		[Command] void CmdResume()
		{
			if (_gameplay.Pause)
			{
				_gameResumedFromServer.Fire();
			}
			
			RpcResume();
		}

		[ClientRpc] void RpcResume()
		{
			OnResumeFromServer();
		}

		[Command] void CmdSetName(string name)
		{
			_name = name;
			_playerNameChanged.Fire(this, name);
		}

		[ClientRpc] void RpcSetName(string name)
		{
			gameObject.name = name;
		}

		void OnNameChanged(string name)
		{
			_playerNameChanged.Fire(this, name);
		}

		void OnScoreChanged(int score)
		{
			_playerScoreChagned.Fire(this, score);
		}

		public void AddScore(int scoreBase, Vector3 position)
		{
			if (!isServer) return;

			var score = scoreBase * _scoreModificator;
			_score += score;
			_playerScoreChagned.Fire(this, _score);

			TargetRpc(connectionToClient, score, position);
		}

		[TargetRpc]
		void TargetRpc(NetworkConnection conn, int score, Vector3 position)
		{
			_scoreObjectSpawned.Fire(score, position);
		}

		public bool TryGetBonus(Bonus bonus)
		{
			if (!isServer) return false;

			if (bonus.BonusType == _speedBonus)
			{
				_fireRateModificator = 3;
			}
			else if (bonus.BonusType == _gunBonus && _gunsCount < 3)
			{
				_gunsCount++;
				_spaceShip.OnGunsCountChanged(_gunsCount);
			}
			else if (bonus.BonusType == _powerBonus && _gunsPower < 2)
			{
				_gunsPower++;
				_spaceShip.OnGunsPowerChanged(_gunsPower);
			}
			else if (bonus.BonusType == _scoreBonus)
			{
				_scoreModificator = 2;
			}
			else if (bonus.BonusType == _timeBonus)
			{
				_gameplay.GameSpeed /= 2;
			}
			else if (bonus.BonusType == _shieldBonus)
			{
				_hasShield = true;
			}

			return true;
		}

		void OnGunsPowerChanged(int gunsPower)
		{

		}

		void OnFireRateModificatorChanged(float modificator)
		{

		}

		void OnGunsCountChanged(int gunsCount)
		{

		}

		void OnScoreModificatorChanged(int scoreModificator)
		{

		}

		void OnHasShieldChanged(bool hasShield)
		{

		}

		public class PlayerCustomFactory : IFactory<GameObject, int, Player>
		{
			[Inject] DiContainer _container;

			public Player Create(GameObject prefab, int playerId)
			{
				var go = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
				var player = go.GetComponent<Player>();
				player._playerId = playerId;
				_container.Inject(player);
				return player;
			}
		}

		public class PlayerFactory : Factory<GameObject, int, Player>
		{
		}
	}
}
