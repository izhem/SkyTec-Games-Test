using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace SkyTecGamesTest
{
	public class Asteroid : NetworkBehaviour
	{
		[Inject] AsteroidsPool _pool;
		[Inject] Gameplay _gameplay;
		[Inject] PlayerScoreChanged _playerScoreChnaged;
		[InjectOptional(Id = "AsteroidsParent")] Transform _parent;
		[Inject(Id = "Destroy")] Effect.EffectsPool _destroyPool;
		bool _wasInjected;

		Rigidbody2D __rigidbody;
		public Rigidbody2D Rigidbody { get { if (__rigidbody == null) __rigidbody = GetComponent<Rigidbody2D>(); return __rigidbody; } }
		SpriteRenderer __spriteRenderer;
		public SpriteRenderer SpriteRenderer { get { if (__spriteRenderer == null) __spriteRenderer = GetComponent<SpriteRenderer>(); return __spriteRenderer; } }

		Vector3 _endPosition;
		Queue<Vector2> _way;
		Vector2 _target;
		bool _wayEnd;
		Vector2 _lastDir;
		List<AsteroidType> _splintersTypes = new List<AsteroidType>();
		int _splintersCount;

		[SyncVar(hook = "OnHPChanged")] int _hp;
		[SyncVar] int _bounty;
		[SyncVar] float _speed;
		[SyncVar] bool _hasSplinters;
		[SyncVar(hook = "OnSortingOrderChanged")] int _sortingOrder;
		[SyncVar(hook = "OnAngularVelocityChanged")] float _angularVelocity;
		[SyncVar(hook = "OnSpriteNameChanged")] string _spriteName;
		[SyncVar(hook = "OnColliderIdChanged")] int _collId;

		[SerializeField] string _spritesInResources;
		[SerializeField] string _playerTag;
		[SerializeField] string _bulletTag;
		[SerializeField] string _destroyTag;

		void OnAngularVelocityChanged(float angularVelocity)
		{
			Rigidbody.angularVelocity = angularVelocity;
		}

		void OnColliderIdChanged(int collId)
		{
			ApplyColliderId(collId);
		}

		void OnSortingOrderChanged(int sortingOrder)
		{
			SpriteRenderer.sortingOrder = sortingOrder;
		}

		void OnSpriteNameChanged(string spriteName)
		{
			ApplySpriteName(spriteName);
		}

		void OnHPChanged(int hp)
		{

		}

		void ApplyColliderId(int collId)
		{
			foreach (var collider in GetComponentsInChildren<AsteroidCollider>())
			{
				collider.gameObject.SetActive(collider.Id == collId);
			}
		}

		void ApplySpriteName(string spriteName)
		{
			var sprites = Resources.LoadAll<Sprite>(_spritesInResources);
			foreach (var sprite in sprites)
			{
				if (sprite.name == spriteName)
				{
					SpriteRenderer.sprite = sprite;
					break;
				}
			}
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

			transform.parent = _parent;
			OnSortingOrderChanged(_sortingOrder);
			OnSpriteNameChanged(_spriteName);
			OnColliderIdChanged(_collId);
			OnAngularVelocityChanged(_angularVelocity);
		}

		[Inject] void Inject()
		{
			_wasInjected = true;
		}

		[ClientRpc]
		void RpcSpawn(int colliderId, string spriteName, int sortingOrder)
		{
			gameObject.SetActive(true);
			SpriteRenderer.sortingOrder = sortingOrder;
			ApplySpriteName(spriteName);
			ApplyColliderId(colliderId);
			Rigidbody.angularVelocity = _angularVelocity;
		}

		void UnSpawn(bool killed)
		{
			_pool.Despawn(this);
			if (killed)
			{
				SoundManager.Instance.PlayExpolosionSound();
			}
			RpcUnSpawn(killed);
		}

		[ClientRpc] void RpcUnSpawn(bool killed)
		{
			if (killed)
			{
				SoundManager.Instance.PlayExpolosionSound();
			}
			gameObject.SetActive(false);
		}

		void OnCollisionEnter2D(Collision2D collision)
		{
			if (!isServer) return;

			if (collision.gameObject.tag == _playerTag)
			{
				UnSpawn(true);
			}
			else if (collision.gameObject.tag == _bulletTag)
			{
				var bullet = collision.gameObject.GetComponent<Bullet>();
				_hp -= bullet.Damage;
				if (_hp <= 0)
				{
					bullet.PlayerOwner.AddScore(_bounty, transform.position);
					_destroyPool.Spawn(transform.position);
					
					if (_hasSplinters)
					{
						for (int i = 0; i < _splintersCount; i++)
						{
							var type = UnityEngine.Random.Range(0, _splintersTypes.Count);
							_pool.Spawn(_gameplay.GetCurveForSplinter(transform.position, _endPosition), _splintersTypes[type]);
						}
					}

					UnSpawn(true);
				}
			}
		}

		

		private void OnTriggerEnter2D(Collider2D collision)
		{
			if (!isServer) return;

			if (collision.gameObject.tag == _destroyTag)
			{
				UnSpawn(false);
			}
		}

		void Update()
		{
			if (!isServer) return;

			var delta = _speed * Time.deltaTime;

			if (_wayEnd)
			{
				Rigidbody.MovePosition((Vector2)transform.position + _lastDir.normalized * delta);
				return;
			}

			var last = (Vector2)transform.position;
			_lastDir = _target - last;
			_wayEnd = false;
			while (_lastDir.sqrMagnitude < delta * delta)
			{
				delta -= _lastDir.magnitude;
				last = _target;
				if (_way.Count == 0)
				{
					_wayEnd = true;
					break;
				}
				_target = _way.Dequeue();
				_lastDir = _target - last;
			}


			Rigidbody.MovePosition(last + _lastDir.normalized * delta);
		}

		public class AsteroidsPool : MemoryPool<AsteroidCurve, AsteroidType, Asteroid>
		{
			[Inject] DiContainer _container;
			[InjectOptional(Id = "AsteroidsParent")] Transform _parent;
			[Inject(Id = "WayDelta")] float _deltaT;
			List<int> _usedSortingOrders = new List<int>();
			float _gameSpeed;

			public AsteroidsPool([Inject] GameSpeedChanged gameSpeedChanged)
			{
				gameSpeedChanged.Listen((speed) => _gameSpeed = speed);
			}

			int GetSortingOrder()
			{
				int sOrder = 0;
				bool unique = false;
				bool found = false;
				while (!unique)
				{
					unique = true;
					found = false;
					foreach(var order in _usedSortingOrders)
					{
						if (order == sOrder)
						{
							found = true;
							unique = false;
							break;
						}
					}
					if (found) sOrder++;
				}
				return sOrder;
			}

			protected override void OnCreated(Asteroid item)
			{
				if (!item._wasInjected) _container.Inject(item);
				item.transform.parent = _parent;
				item.gameObject.SetActive(false);
			}

			protected override void OnSpawned(Asteroid item)
			{
				
			}

			protected override void OnDespawned(Asteroid item)
			{
				item.gameObject.SetActive(false);
				_usedSortingOrders.Remove(item.SpriteRenderer.sortingOrder);
				NetworkServer.UnSpawn(item.gameObject);
			}

			protected override void Reinitialize(AsteroidCurve curve, AsteroidType type, Asteroid item)
			{
				var sOrder = GetSortingOrder();
				_usedSortingOrders.Add(sOrder);
				item.SpriteRenderer.sprite = type.GetRandomSprite();
				item.SpriteRenderer.sortingOrder = sOrder;
				var collId = type.SetColliderForSprite(item.SpriteRenderer.sprite, item);

				item._way = curve.GetPath(_deltaT);
				item._wayEnd = false;
				var spawnPoint = item._way.Dequeue();
				item.transform.position = new Vector3(spawnPoint.x, spawnPoint.y, 0);
				item._target = item._way.Dequeue();
				item._bounty = type.Bounty;
				item._hp = type.HP;
				item._speed = type.GetRandomBaseSpeed() * _gameSpeed;
				item._hasSplinters = type.HasSplinters;
				item._splintersCount = type.GetRandomSplintersCount();
				item._splintersTypes = type.GetSplintersTypes();
				item._endPosition = curve.P3;
				item._sortingOrder = sOrder;
				item._spriteName = item.SpriteRenderer.sprite.name;
				item._collId = collId;
				item._angularVelocity = type.GetRandomRotation();
				item.gameObject.SetActive(true);
				NetworkServer.Spawn(item.gameObject);
				item.RpcSpawn(collId, item.SpriteRenderer.sprite.name, sOrder);
			}
		}
	}
}