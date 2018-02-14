using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace SkyTecGamesTest
{
	public class Background : MonoBehaviour
	{
		int _current;
		float _height;
		float _modificator;
		float _speed;

		[SerializeField] Transform[] _backs;
		[SerializeField] float _baseSpeed;

		public void OnGameSpeedChanged(float speed)
		{
			_modificator = speed;
			_speed = _baseSpeed * speed;
		}

		void Start()
		{
			if (_backs.Length == 0)
			{
				enabled = false;
				return;
			}

			_height = _backs[0].GetComponent<SpriteRenderer>().bounds.size.y;

			for(int i = 0; i < _backs.Length; i++)
			{
				var pos = _backs[i].position;
				_backs[i].position = new Vector3(pos.x, _height * i, pos.z);
			}
		}

		void Update()
		{
			var delta = _speed * Time.deltaTime;
			foreach(var back in _backs)
			{
				back.Translate(0, -delta, 0);
			}

			if (_backs[_current].position.y < -_height)
			{
				var pos = _backs[_current].position;
				var last = (_current + _backs.Length - 1) % _backs.Length;
				_backs[_current].position = new Vector3(pos.x, _backs[last].position.y + _height, pos.z);
				_current = (_current + 1) % _backs.Length;
			}
		}
	}
}