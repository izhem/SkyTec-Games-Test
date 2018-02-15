using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using Mono.Nat;

namespace SkyTecGamesTest
{
	public class GameManager : NetworkManager
	{
		[Inject] DiContainer _container;
		[Inject] Player.PlayerFactory _playerFactory;

		static string _externalIPCahsed;

		public static int InternalPort { get; set; }
		public static string LocalPlayerName { get; set; }
		public static string ExternalIP { get; set; }
		public static int ExternalPort { get; set; }
		public static string InternalIP { get; set; }
		public static string DeviceExternalIP { get; set; }

		[SerializeField] bool _useExternalIp;
		[SerializeField] int _externalPort;
		[SerializeField] int _internalPort;

		float _time;

		[SerializeField] float _timeToFindExternalIp = 5;


		static GameManager _instance;
		public static GameManager Instance { get { if (_instance == null) _instance = FindObjectOfType<GameManager>(); return _instance; } }

		public static void Host()
		{
			Instance.networkPort = InternalPort;
			if (Instance._useExternalIp)
			{
				Instance.networkAddress = ExternalIP;
				Instance.networkPort = Instance._externalPort;
			}
			else
			{
				Instance.networkAddress = InternalIP;
				Instance.networkPort = Instance._internalPort;
			}

			Instance.serverBindAddress = Instance.networkAddress;
			Instance.serverBindToIP = true;
			NetworkServer.Reset();
			Instance.StartHost();
		}

		public static void JoinServer(string hostIP, int hostPort)
		{
			if (hostPort == 0) Instance.networkPort = InternalPort;
			else Instance.networkPort = hostPort;
			Instance.networkAddress = hostIP;
			Instance.StartClient().RegisterHandler(MsgType.Error, OnError);
		}

		static void OnError(NetworkMessage msg)
		{
			var startUI = FindObjectOfType<StartUI>();
			if (startUI != null)
			{
				startUI.ShowMessage("Unable to connect.");
			}
		}

		System.Collections.IEnumerator WaitAndDisconnect(float time)
		{
			yield return new WaitForSecondsRealtime(time);
			Disconnect();
		}

		public static void Disconnect(float secs)
		{
			Instance.StartCoroutine(Instance.WaitAndDisconnect(secs));
		}

		public static void Disconnect()
		{
			if (Instance.client != null)
			{
				Instance.client.Disconnect();
				Instance.client = null;
				SceneManager.LoadScene(Instance.offlineScene);
			}
		}
		

		int GenerateUniquePlayerId()
		{
			int id = 1;
			bool unique = false;
			while (!unique)
			{
				unique = true;
				foreach (var conn in NetworkServer.localConnections)
				{
					if (conn.playerControllers.Count > 0 && conn.playerControllers[0].gameObject.GetComponent<Player>().PlayerId == id)
					{
						unique = false;
						id++;
						break;
					}
				}
			}
			return id;
		}

		public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
		{
			if (_container == null)
			{
				var context = FindObjectOfType<SceneContext>();
				_container = context.Container;
				_container.Inject(this);
			}

			var playerId = GenerateUniquePlayerId();
			var player = _playerFactory.Create(playerPrefab, playerId);
			NetworkServer.AddPlayerForConnection(conn, player.gameObject, 0);
		}

		public override void OnClientSceneChanged(NetworkConnection conn)
		{
			if (_container == null)
			{
				var context = FindObjectOfType<SceneContext>();
				if (context != null)
				{
					_container = context.Container;
					_container.Inject(this);
				}
			}

			base.OnClientSceneChanged(conn);
		}

		void DeviceFound(object sender, DeviceEventArgs args)
		{
			INatDevice device = args.Device;

			DeviceExternalIP = device.GetExternalIP().ToString();
			device.CreatePortMap(new Mapping(Protocol.Tcp, networkPort, networkPort));

		}

		void DeviceLost(object sender, DeviceEventArgs args)
		{
			INatDevice device = args.Device;

			device.DeletePortMap(new Mapping(Protocol.Tcp, ExternalPort, networkPort));
		}

		void Awake()
		{
			if (_instance == null)
			{
				_instance = this;
				DontDestroyOnLoad(this);
				LocalPlayerName = "Host";

				InternalPort = networkPort;

				StartCoroutine(NetworkSetup());

				NatUtility.DeviceFound += DeviceFound;
				NatUtility.DeviceLost += DeviceLost;
				NatUtility.StartDiscovery();

				StartCoroutine(CheckExternalIPAndSet());
			}
			else
			{
				if (_instance != this)
				{
					Destroy(gameObject);
				}
			}
		}

		System.Collections.IEnumerator CheckExternalIPAndSet()
		{
			while (true)
			{
				if (DeviceExternalIP != _externalIPCahsed)
				{
					_externalIPCahsed = DeviceExternalIP;
					var ui = FindObjectOfType<StartUI>();
					if (ui != null)
					{
						ui.Ip = "e " + ExternalIP + ":" + ExternalPort + "; d " + DeviceExternalIP + "; l " + InternalIP;
					}
				}
				yield return new WaitForSecondsRealtime(1);
			}
		}

		public System.Collections.IEnumerator NetworkSetup()
		{
			Network.Connect("127.0.0.1");
			_time = 0;
			while (Network.player.externalIP == "UNASSIGNED_SYSTEM_ADDRESS")
			{
				_timeToFindExternalIp += Time.deltaTime + 0.01f;

				if (_time > _timeToFindExternalIp)
				{
					Debug.LogError(" Unable to obtain external ip: Are you sure your connected to the internet");
				}

				yield return new WaitForEndOfFrame();
			}
			_time = 0;
			ExternalIP = Network.player.externalIP;
			ExternalPort = Network.player.externalPort;
			InternalIP = Network.player.ipAddress;
			Debug.Log(Network.player.ipAddress);
			Debug.Log(Network.player.externalIP + ":" + Network.player.externalPort);
			var ui = FindObjectOfType<StartUI>();
			if (ui != null)
			{
				ui.Ip = "e " + ExternalIP + ":" + ExternalPort + "; d " + DeviceExternalIP + "; l " + InternalIP;
			}
			Network.Disconnect();
		}
	}
}
