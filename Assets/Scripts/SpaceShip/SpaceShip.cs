using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace SkyTecGamesTest
{
	public class SpaceShip : NetworkBehaviour
	{
		[Inject] Bullet.BulletsPool _bulletsPool;
		[Inject(Id = "Destroy")] Effect.EffectsPool _destroyPool;
		bool _wasInjected;

		Animator __animator;
		Animator _animator { get { if (__animator == null) __animator = GetComponent<Animator>(); return __animator; } }
		Rigidbody2D __rigidbody;
		Rigidbody2D _rigidbody { get { if (__rigidbody == null) __rigidbody = GetComponent<Rigidbody2D>(); return __rigidbody; } }

		float _fireTime;
		float _fireOffset;
		int _gunReady = -1;
		float[] _fireTimers = new float[3];
		TrailRenderer _leftTrail, _rightTrail;
		Player _ownerPlayer;
		bool _fire;
		int _gunsCount = 1;
		BulletType _bulletType;

		[SyncVar] int _playerId;
		

		[Header("Ship Controller")]
		[Tooltip("How often ship can fire a bullet")]
		[SerializeField] float _fireRate;
		[Tooltip("Define velocity at max input")]
		[SerializeField] float _speed;

		[Header("Input")]
		[SerializeField] string _moveAxis;
		[SerializeField] string _fireButton;
		
		[Header("Animator")]
		[SerializeField] string _speedAnimatorKey;

		[Header("Tags")]
		[SerializeField] string _enemyTag;

		[Header("Placeholders")]
		[SerializeField] Transform _fireLeftPlace;
		[SerializeField] Transform _fireMidPlace, _fireRightPlace;
		[SerializeField] Transform _engineLeftPlace, _engineRightPlace;

		[Header("Bullets")]
		[SerializeField] BulletType[] _bulletsTypes;

		public bool EngineTrailsSpawned { get; set; }
		public Transform EngineLeftPlace { get { return _engineLeftPlace; } }
		public Transform EngineRightPlace { get { return _engineRightPlace; } }
		public Player OwnerPlayer { get { return _ownerPlayer; } }

		[Inject] void Inject() { _wasInjected = true; }

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

			foreach (var player in FindObjectsOfType<Player>())
			{
				if (player.PlayerId == _playerId)
				{
					_ownerPlayer = player;
					break;
				}
			}
		}

		public void OnFireRateModificatorChanged(float fireRateModificator)
		{
			_fireTime = fireRateModificator * _fireRate;

			if (_gunsCount == 1)
			{
				_fireOffset = 0;
			}
			else
			{
				_fireOffset = _fireTime / _gunsCount;
			}

			UpdateFireTimers();
		}

		public void OnGunsCountChanged(int gunsCount)
		{
			if (!isServer) return;

			_gunsCount = gunsCount;

			if (_gunsCount == 1)
			{
				_fireOffset = 0;
			}
			else
			{
				_fireOffset = _fireTime / _gunsCount;
			}
			
			UpdateFireTimers();
		}

		public void OnGunsPowerChanged(int gunsPower)
		{
			BulletType maxLevelType = _bulletsTypes[0];
			int maxLevel = maxLevelType.GunLevel;
			foreach(var bulletType in _bulletsTypes)
			{
				if (bulletType.GunLevel > maxLevel)
				{
					maxLevel = bulletType.GunLevel;
					maxLevelType = bulletType;
				}

				if (bulletType.GunLevel == gunsPower)
				{
					_bulletType = bulletType;
					return;
				}
			}

			_bulletType = maxLevelType;
		}

		void UpdateFireTimers()
		{
			if (_gunReady >= 0)
			{
				for (int i = 0; i < _gunsCount; i++)
				{
					if (i == _gunReady)
					{
						_fireTimers[i] = 0;
					}
					else
					{
						if (i < _gunReady)
						{
							_fireTimers[i] = (_gunsCount - _gunReady + i) * _fireOffset;
						}
						else
						{
							_fireTimers[i] = (i - _gunReady) * _fireOffset;
						}

					}
				}
			}
			else
			{
				int minIndex = -1;
				float minTime = float.PositiveInfinity;

				for (int i = 0; i < _gunsCount; i++)
				{
					if (_fireTimers[i] < minTime)
					{
						minTime = _fireTimers[i];
						minIndex = i;
					}
				}

				for (int i = 0; i < _gunsCount; i++)
				{
					if (i == minIndex)
					{
						_fireTimers[i] = minTime;
					}
					else
					{
						if (i < minIndex)
						{
							_fireTimers[i] = minTime + (_gunsCount - minIndex + i) * _fireOffset;
						}
						else
						{
							_fireTimers[i] = minTime + (i - minIndex) * _fireOffset;
						}

					}
				}
			}
		}

		[Command]
		void CmdInput(float input, bool fire)
		{
			input = Mathf.Clamp(input, -1, 1);
			var speed = _speed * input * Vector2.right;
			_rigidbody.velocity = speed;
			RpcApplyInput(speed);
			_fire = fire;
		}

		[ClientRpc]
		void RpcApplyInput(Vector2 speed)
		{
			_rigidbody.velocity = speed;
		}

		void Fire(int gunIndex)
		{
			_fireTimers[gunIndex] = _fireTime + _fireOffset;

			Transform firePlaceHolder = _fireMidPlace;

			if (_gunsCount == 2)
			{
				firePlaceHolder = gunIndex == 0 ? _fireLeftPlace : _fireRightPlace;
			}
			else if (_gunsCount == 3)
			{
				if (gunIndex == 0)
				{
					firePlaceHolder = _fireMidPlace;
				}
				else
				{
					firePlaceHolder = gunIndex == 1 ? _fireLeftPlace : _fireRightPlace;
				}
			}

			_bulletsPool.Spawn(_bulletType, firePlaceHolder.position, _ownerPlayer);
		}

		[ClientRpc]
		void RpcDeath()
		{
		}

		void OnCollisionEnter2D(Collision2D collision)
		{
			if (!isServer) return;

			if (collision.gameObject.tag == _enemyTag)
			{
				if (_ownerPlayer.HasShield)
				{
					_ownerPlayer.CmdRemoveShield();
				}
				else
				{
					_destroyPool.Spawn(transform.position);
					SoundManager.Instance.PlayExpolosionSound();
					NetworkServer.Destroy(gameObject);
				}
			}
		}

		

		private void OnDestroy()
		{
			if (isServer) return;
			SoundManager.Instance.PlayExpolosionSound();
			if (!hasAuthority) return;
			GameManager.Disconnect(3);
		}

		void Update()
		{
			if (_ownerPlayer.isLocalPlayer)
			{
				CmdInput(Input.GetAxis(_moveAxis), Input.GetButton(_fireButton));
			}

			if (isServer)
			{
				if (_fire || _gunReady < 0)
				{
					if (_fire && _gunReady >= 0)
					{
						Fire(_gunReady);
						_gunReady = -1;
					}
					
					for (int i = 0; i < _gunsCount; i++)
					{
						if (_fireTimers[i] > 0)
						{
							_fireTimers[i] -= Time.deltaTime;
							if (_fireTimers[i] <= 0) _fireTimers[i] = 0;
						}

						if (_fireTimers[i] == 0)
						{
							if (_fire)
							{
								Fire(i);
							}
							else
							{
								_gunReady = i;
							}
						}
					}
				}
			}

			_animator.SetFloat(_speedAnimatorKey, _rigidbody.velocity.x / _speed);
		}

		public class SpaceShipCustomFactory : IFactory<GameObject, Player, SpaceShip>
		{
			[Inject] DiContainer _container;

			public SpaceShip Create(GameObject prefab, Player player)
			{
				var go = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
				var spaceship = go.GetComponent<SpaceShip>();
				spaceship._ownerPlayer = player;
				spaceship._playerId = player.PlayerId;
				spaceship.OnGunsPowerChanged(player.GunsPower);
				spaceship.OnFireRateModificatorChanged(player.FireRateModificator);
				spaceship.OnGunsCountChanged(player.GunsCount);
				_container.Inject(spaceship);
				return spaceship;
			}
		}

		public class SpaceShipFactory : Factory<GameObject, Player, SpaceShip>
		{
		}
	}
}