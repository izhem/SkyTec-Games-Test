using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkyTecGamesTest
{
	[CreateAssetMenu(menuName = "Game/BulletType")]
	public class BulletType : ScriptableObject
	{
		[Header("Base")]
		[SerializeField] int _gunLevel;
		[SerializeField] Sprite _sprite;
		[SerializeField] float _speed;
		[SerializeField] int _damage;
		[SerializeField] Color _lightColor;

		public int GunLevel { get { return _gunLevel; } }
		public Sprite Sprite { get { return _sprite; } }
		public float Speed { get { return _speed; } }
		public int Damage { get { return _damage; } }
		public Color LightColor { get { return _lightColor; } }
	}
}