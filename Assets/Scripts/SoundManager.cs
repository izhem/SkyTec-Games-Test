using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace SkyTecGamesTest
{
	public class SoundManager : MonoBehaviour
	{
		static SoundManager _instance;
		public static SoundManager Instance { get { if (_instance == null) _instance = FindObjectOfType<SoundManager>(); return _instance; } }

		Stack<AudioSource> _inactiveShootSources = new Stack<AudioSource>();
		Stack<AudioSource> _inactiveDestroySources = new Stack<AudioSource>();
		int _shootsSounds;
		int _explosionsSounds;

		[SerializeField] AudioSource _musicSource;
		[SerializeField] AudioSource _explosionSourceExample;
		[SerializeField] AudioSource _shootSourceExample;
		[SerializeField] GameObject _otherSourcesRoot;
		[SerializeField] int _maxShootSounds = 7;
		[SerializeField] int _maxExplosionsSounds = 4;
		[SerializeField] AudioClip[] _shootSoundsClips;
		[SerializeField] AudioClip[] _explosionSoundsClips;
		[SerializeField] AudioClip _music;

		//private void Update()
		//{
		//	if (Input.GetKeyDown(KeyCode.E))
		//	{
		//		PlayExpolosionSound();
		//	}

		//	if (Input.GetKeyDown(KeyCode.S))
		//	{
		//		PlayShootSound();
		//	}
		//}

		public void PlayExpolosionSound()
		{
			if (_explosionsSounds >= _maxExplosionsSounds) return;
			if (_explosionSoundsClips.Length == 0) return;

			_explosionsSounds++;
			var source = GetAudioSource(false);
			var index = _explosionSoundsClips.Length == 1 ? 0 : UnityEngine.Random.Range(0, _explosionSoundsClips.Length);
			source.clip = _explosionSoundsClips[index];
			source.Play();
			StartCoroutine(CheckForSourceEnd(source, false));
		}

		public void PlayShootSound()
		{
			if (_shootsSounds >= _maxShootSounds) return;
			if (_shootSoundsClips.Length == 0) return;

			_shootsSounds++;
			var source = GetAudioSource(true);
			var index = _shootSoundsClips.Length == 1 ? 0 : UnityEngine.Random.Range(0, _shootSoundsClips.Length);
			source.clip = _shootSoundsClips[index];
			source.Play();
			StartCoroutine(CheckForSourceEnd(source, true));
		}

		System.Collections.IEnumerator CheckForSourceEnd(AudioSource source, bool shoot)
		{
			while (source.isPlaying)
			{
				yield return new WaitForFixedUpdate();
			}

			ReturnAudioSource(source, shoot);
		}

		void ReturnAudioSource(AudioSource source, bool shoot)
		{
			if (shoot)
			{
				_shootsSounds--;
				_inactiveShootSources.Push(source);
			}
			else
			{
				_explosionsSounds--;
				_inactiveDestroySources.Push(source);
			}
		}

		AudioSource GetAudioSource(bool shoot)
		{
			if (shoot)
			{
				if (_inactiveShootSources.Count > 0)
				{
					return _inactiveShootSources.Pop();
				}
				else
				{
					return GameObject.Instantiate(_shootSourceExample, _otherSourcesRoot.transform).GetComponent<AudioSource>();
				}
			}
			else
			{
				if (_inactiveDestroySources.Count > 0)
				{
					return _inactiveDestroySources.Pop();
				}
				else
				{
					return GameObject.Instantiate(_explosionSourceExample, _otherSourcesRoot.transform).GetComponent<AudioSource>();
				}
			}
		}

		void Awake()
		{
			if (_instance == null)
			{
				_instance = this;
				DontDestroyOnLoad(this);
			}
			else
			{
				if (_instance != this)
				{
					Destroy(gameObject);
				}
			}
		}
	}
}
