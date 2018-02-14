using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace SkyTecGamesTest
{
	public class Bonus : NetworkBehaviour
	{
		[Inject] BonusesPool _pool;
		[InjectOptional(Id = "BonusesParent")] Transform _parent;
		bool _wasInjected;

		Rigidbody2D __rigidbody;
		public Rigidbody2D Rigidbody { get { if (__rigidbody == null) __rigidbody = GetComponent<Rigidbody2D>(); return __rigidbody; } }
		SpriteRenderer __spriteRenderer;
		public SpriteRenderer SpriteRenderer { get { if (__spriteRenderer == null) __spriteRenderer = GetComponent<SpriteRenderer>(); return __spriteRenderer; } }

		Queue<Vector2> _way;
		Vector2 _target;
		bool _wayEnd;
		Vector2 _lastDir;
		BonusType _bonusType;

		[SyncVar] float _speed;
		[SyncVar(hook = "OnSpriteNameChanged")] string _spriteName;

		[SerializeField] string _spritesInResources;
		[SerializeField] string _playerTag;
		[SerializeField] string _destroyTag;

		public BonusType BonusType { get { return _bonusType; } }

		void OnSpriteNameChanged(string spriteName)
		{
			ApplySpriteName(spriteName);
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
			OnSpriteNameChanged(_spriteName);
		}

		[Inject] void Inject()
		{
			_wasInjected = true;
		}

		[ClientRpc]
		void RpcSpawn(string spriteName)
		{
			gameObject.SetActive(true);
			ApplySpriteName(spriteName);
		}

		void UnSpawn()
		{
			_pool.Despawn(this);
			RpcUnSpawn();
		}

		[ClientRpc] void RpcUnSpawn()
		{
			gameObject.SetActive(false);
		}

		void OnTriggerEnter2D(Collider2D collision) 
		{
			if (!isServer) return;

			if (collision.gameObject.tag == _playerTag)
			{
				if (collision.gameObject.GetComponent<SpaceShip>().OwnerPlayer.TryGetBonus(this))
				{
					UnSpawn();
				}
				
			}
			else if (collision.gameObject.tag == _destroyTag)
			{
				UnSpawn();
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

		public class BonusesPool : MemoryPool<AsteroidCurve, BonusType, Bonus>
		{
			[Inject] DiContainer _container;
			[InjectOptional(Id = "BonusesParent")] Transform _parent;
			[Inject(Id = "WayDelta")] float _deltaT;

			protected override void OnCreated(Bonus item)
			{
				if (!item._wasInjected) _container.Inject(item);
				item.transform.parent = _parent;
				item.gameObject.SetActive(false);
			}

			protected override void OnSpawned(Bonus item)
			{
				
			}

			protected override void OnDespawned(Bonus item)
			{
				item.gameObject.SetActive(false);
				NetworkServer.UnSpawn(item.gameObject);
			}

			protected override void Reinitialize(AsteroidCurve curve, BonusType type, Bonus item)
			{
				item.SpriteRenderer.sprite = type.Sprite;
				item._bonusType = type;
				item._way = curve.GetPath(_deltaT);
				item._wayEnd = false;
				var spawnPoint = item._way.Dequeue();
				item.transform.position = new Vector3(spawnPoint.x, spawnPoint.y, 0);
				item._target = item._way.Dequeue();
				item._speed = type.Speed;
				item._spriteName = item.SpriteRenderer.sprite.name;
				item.gameObject.SetActive(true);
				NetworkServer.Spawn(item.gameObject);
				item.RpcSpawn(item.SpriteRenderer.sprite.name);
			}
		}
	}
}