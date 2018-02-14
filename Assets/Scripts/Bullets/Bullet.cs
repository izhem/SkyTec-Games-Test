using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace SkyTecGamesTest
{
	public class Bullet : NetworkBehaviour
	{
		[InjectOptional(Id = "BulletsParent")] Transform _parent;
		[Inject(Id = "Bullet")] Effect.EffectsPool _bulletpool;
		[Inject] BulletsPool _pool;
		bool _wasInjected;

		Player _playerOwner;
		int _damage;

		[SyncVar(hook = "OnSpeedChanged")] float _speed;
		[SyncVar(hook = "OnLightColorChanged")] Color _lightColor;
		[SyncVar(hook = "OnSpriteNameChanged")] string _spriteName;

		[SerializeField] string _spritesPath;

		Light __light;
		Light _light { get { if (__light == null) __light = GetComponentInChildren<Light>(); return __light; } }
		Rigidbody2D __rigidbody;
		Rigidbody2D _rigidbody { get { if (__rigidbody == null) __rigidbody = GetComponent<Rigidbody2D>(); return __rigidbody; } }
		SpriteRenderer __spriteRenderer;
		SpriteRenderer _spriteRenderer { get { if (__spriteRenderer == null) __spriteRenderer = GetComponent<SpriteRenderer>(); return __spriteRenderer; } }

		public Player PlayerOwner { get { return _playerOwner; } }
		public int Damage { get {return  _damage; } }

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

			OnLightColorChanged(_lightColor);
			OnSpriteNameChanged(_spriteName);

			transform.parent = _parent;
		}

		[Inject] void Inject() { _wasInjected = true; }

		void OnLightColorChanged(Color lightColor)
		{
			_light.color = lightColor;
		}

		void OnSpriteNameChanged(string spriteName)
		{
			foreach(var sprite in Resources.LoadAll<Sprite>(_spritesPath))
			{
				if (sprite.name == spriteName)
				{
					_spriteRenderer.sprite = sprite;
					break;
				}
			}
		}

		void OnSpeedChanged(float speed)
		{
			_rigidbody.velocity = Vector2.up * speed;
		}

		[ClientRpc]
		void RpcSpawn(float speed, string spriteName, Color lightColor)
		{
			SoundManager.Instance.PlayShootSound();
			OnLightColorChanged(lightColor);
			OnSpriteNameChanged(spriteName);
			OnSpeedChanged(speed);
		}

		[ClientRpc]
		void RpcUnSpawn()
		{
			gameObject.SetActive(false);
		}

		void OnCollisionEnter2D(Collision2D collision)
		{
			if (!isServer) return;

			_bulletpool.Spawn(transform.position);
			_pool.Despawn(this);
		}

		void OnTriggerEnter2D(Collider2D collision)
		{
			if (!isServer) return;

			_pool.Despawn(this);
		}

		public class BulletsPool : MemoryPool<BulletType, Vector3, Player, Bullet>
		{
			[InjectOptional(Id = "BulletsParent")] Transform _parent;
			[Inject] DiContainer _container;

			protected override void OnCreated(Bullet item)
			{
				if (!item._wasInjected) _container.Inject(item);
				item.transform.parent = _parent;
				item.gameObject.SetActive(false);
			}

			protected override void OnSpawned(Bullet item)
			{

			}

			protected override void OnDespawned(Bullet item)
			{
				item.gameObject.SetActive(false);
				NetworkServer.UnSpawn(item.gameObject);
				item.RpcUnSpawn();
			}

			protected override void Reinitialize(BulletType type, Vector3 position, Player player, Bullet item)
			{
				item.transform.position = position;
				item._lightColor = type.LightColor;
				item._spriteRenderer.sprite = type.Sprite;
				item._damage = type.Damage;
				item._speed = type.Speed;
				item._rigidbody.velocity = Vector2.up * item._speed;
				item._playerOwner = player;
				item.gameObject.SetActive(true);
				NetworkServer.Spawn(item.gameObject);
				item.RpcSpawn(item._speed, item._spriteRenderer.sprite.name, item._lightColor);
			}
		}
	}
}