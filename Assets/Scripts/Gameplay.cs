using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace SkyTecGamesTest
{
	public class Gameplay : NetworkBehaviour
	{
		[Inject] Asteroid.AsteroidsPool _asteroidPool;
		[Inject] Bonus.BonusesPool _bonusPool;
		[Inject] GameSpeedChanged _gameSpeedChanged;
		[Inject] GamePausedFromServer _gamePaused;
		[Inject] GameResumedFromServer _gameResumed;
		bool _wasInjected;

		float _asteroidSpawnTimer;
		float _bonusSpawnTimer;
		float _raiseTimer;
		float _slowTimeBonus;

		[SyncVar(hook = "OnGameSpeedChanged")] float _gameSpeed;
		[SyncVar] bool _slowTimeBounus;
		[SyncVar] bool _pause;

		[Serializable]
		public class Line
		{
			public Vector2 P1, P2;

			public Vector2 GetRandomPoint()
			{
				var t = UnityEngine.Random.Range(0f, 1f);
				return t * P1 + (1 - t) * P2;
			}
		}
		[Header("Spawn geometry")]
		[SerializeField] Transform _leftSpawnPoint;
		[SerializeField] Transform _righSpawnPoint;
		[SerializeField] Transform _leftTopMidPoint, _rightTopMidPoint, _leftBotMidPoint, _rightBotMidPoint;
		[SerializeField] Transform _leftEndPoint, _rightEndPoint;

		[Header("Timing")]
		[SerializeField] float _baseGameSpeed;
		[SerializeField] float _raiseSpeedPeriod;
		[SerializeField] float _raiseModificator;
		

		[Header("Asteroids spawn Settigns")]
		[MinMaxRange(0.5f, 5f)]
		[SerializeField] MinMaxRange _baseSpawnAsteroidPeriodRange;
		[SerializeField] AsteroidType[] _asteroidTypes;

		[Header("Bonuses spawn Settigns")]
		[MinMaxRange(0.5f, 5f)]
		[SerializeField] MinMaxRange _baseSpawnBonusPeriodRange;
		[SerializeField] BonusType[] _bonusTypes;

		public float GameSpeed { get { return _gameSpeed; } set { _gameSpeed = value; } }
		public bool Pause { get { return _pause; } }

		public void OnGamePaused()
		{
			if (isServer)
			{
				_pause = true;
			}
			
			Time.timeScale = 0;
		}

		public void OnGameResumed()
		{
			if (isServer)
			{
				_pause = false;
			}

			Time.timeScale = 1;
		}

		Vector2 GetRandomPointOnLine(Vector2 p1, Vector2 p2)
		{
			var t = UnityEngine.Random.Range(0f, 1f);
			return t * p1 + (1 - t) * p2;
		}

		Vector2 GetRandomPointInRectangle(Vector2 lt, Vector2 rt, Vector2 lb, Vector2 rb)
		{
			var pt = GetRandomPointOnLine(lt, rt);
			var pb = GetRandomPointOnLine(lb, rb);
			var t = UnityEngine.Random.Range(0f, 1f);
			return t * pt + (1 - t) * pb;
		}

		AsteroidCurve GetRandomCurve()
		{
			return new AsteroidCurve()
			{
				P1 = GetRandomPointOnLine(_leftSpawnPoint.position, _righSpawnPoint.position),
				P2 = GetRandomPointInRectangle(_leftTopMidPoint.position,
													_rightTopMidPoint.position,
													_leftBotMidPoint.position,
													_rightBotMidPoint.position),
				P3 = GetRandomPointOnLine(_leftEndPoint.position, _rightEndPoint.position)
			};
		}

		AsteroidType GetRandomAsteroidType()
		{
			var totalWeight = 0f;
			foreach(var type in _asteroidTypes)
			{
				totalWeight += type.SpawnWeight;
			}
			var t = UnityEngine.Random.Range(0, totalWeight);
			float currentWeight = 0;
			AsteroidType resultType = null;
			var enumerator = _asteroidTypes.GetEnumerator();
			enumerator.MoveNext();
			resultType = enumerator.Current as AsteroidType;

			if (t == 0) return resultType;
			else currentWeight += resultType.SpawnWeight;

			while (currentWeight <= t)
			{
				if (enumerator.MoveNext())
				{
					resultType = enumerator.Current as AsteroidType;
				}
				else
				{
					break;
				}
				currentWeight += resultType.SpawnWeight;
			}
			return resultType;
		}

		BonusType GetRandomBonusType()
		{
			var totalWeight = 0f;
			foreach (var type in _bonusTypes)
			{
				totalWeight += type.SpawnWeight;
			}
			var t = UnityEngine.Random.Range(0, totalWeight);
			float currentWeight = 0;
			BonusType resultType = null;
			var enumerator = _bonusTypes.GetEnumerator();
			enumerator.MoveNext();
			resultType = enumerator.Current as BonusType;

			if (t == 0) return resultType;
			else currentWeight += resultType.SpawnWeight;

			while (currentWeight <= t)
			{
				if (enumerator.MoveNext())
				{
					resultType = enumerator.Current as BonusType;
				}
				else
				{
					break;
				}
				currentWeight += resultType.SpawnWeight;
			}
			return resultType;
		}

		public AsteroidCurve GetCurveForSplinter(Vector3 parentPosition, Vector3 endPosition)
		{
			return new AsteroidCurve()
			{
				P1 = parentPosition,
				P2 = GetRandomPointOnLine(parentPosition, endPosition),
				P3 = GetRandomPointOnLine(_leftEndPoint.position, _rightEndPoint.position)
			};
		}

		void SpawnAsteroid()
		{
			_asteroidPool.Spawn(GetRandomCurve(), GetRandomAsteroidType());
		}

		void SpawnBonus()
		{
			_bonusPool.Spawn(GetRandomCurve(), GetRandomBonusType());
		}

		void UpdateAsteroidSpawnTimer()
		{
			var spawnPeriod = _baseSpawnAsteroidPeriodRange.GetRandomValue();
			_asteroidSpawnTimer = spawnPeriod / _gameSpeed;
		}

		void UpdateBonusSpawnTimer()
		{
			var spawnPeriod = _baseSpawnBonusPeriodRange.GetRandomValue();
			_bonusSpawnTimer = spawnPeriod;
		}

		void UpdateRaiseTimer()
		{
			_raiseTimer = _raiseSpeedPeriod;
		}

		void Start()
		{
			if (!isServer) return;

			UpdateAsteroidSpawnTimer();
			UpdateRaiseTimer();
			UpdateBonusSpawnTimer();
		}

		void FixedUpdate()
		{
			if (!isServer) return;

			_raiseTimer -= Time.fixedDeltaTime;
			if (_raiseTimer <= 0)
			{
				_gameSpeed += _raiseModificator;
				_gameSpeedChanged.Fire(_gameSpeed);
				UpdateRaiseTimer();
			}

			_asteroidSpawnTimer -= Time.fixedDeltaTime;
			if (_asteroidSpawnTimer <= 0)
			{
				SpawnAsteroid();
				UpdateAsteroidSpawnTimer();
			}

			_bonusSpawnTimer -= Time.fixedDeltaTime;
			if (_bonusSpawnTimer <= 0)
			{
				SpawnBonus();
				UpdateBonusSpawnTimer();
			}
		}

		void OnGameSpeedChanged(float gameSpeed)
		{
			_gameSpeedChanged.Fire(_gameSpeed);
		}

		public override void OnStartServer()
		{
			_gameSpeed = _baseGameSpeed;
			OnGameSpeedChanged(_gameSpeed);
		}

		public override void OnStartClient()
		{
			if (!_wasInjected)
			{
				var context = FindObjectOfType<SceneContext>();
				if (context != null)
				{
					context.Container.Inject(this);
				}
			}

			OnGameSpeedChanged(_gameSpeed);

			if (_pause)
			{
				_gamePaused.Fire();
			}
			else
			{
				_gameResumed.Fire();
			}
		}
	}
}
