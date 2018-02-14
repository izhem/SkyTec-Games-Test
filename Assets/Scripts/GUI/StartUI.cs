using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

namespace SkyTecGamesTest
{
	public class StartUI : MonoBehaviour
	{
		[SerializeField] Text _ipText;
		[SerializeField] Text _messageText;
		[SerializeField] TransitionalObject _messageAnim;
		[SerializeField] float _timeForOneChar = 0.2f;
		[SerializeField] string _noNameMsg = "Please, enter player name.";
		[SerializeField] string _noIpMsg = "Please, enter host ip address.";

		[Header("Main panel")]
		[SerializeField] InputField _nameInput;
		[SerializeField] Button _startButton;
		[SerializeField] Button _joinMainButton;
		[SerializeField] Button _exitButton;
		[SerializeField] TransitionalObject _mainPanel;

		[Header("Join panel")]
		[SerializeField] InputField _hostInput;
		[SerializeField] InputField _portInput;
		[SerializeField] Button _joinButton;
		[SerializeField] Button _backButton;
		[SerializeField] TransitionalObject _joinPanel;

		public string Ip { get { return _ipText.text; } set { _ipText.text = value; } }

		void Awake()
		{
			_startButton.onClick.AddListener(OnStart);
			_joinMainButton.onClick.AddListener(OnJoinMain);
			_exitButton.onClick.AddListener(OnExit);
			_joinButton.onClick.AddListener(OnJoin);
			_backButton.onClick.AddListener(OnBack);

			_nameInput.text = GameManager.LocalPlayerName;
		}

		void Start()
		{
			_mainPanel.TriggerTransition();

			_ipText.text = "e " + GameManager.ExternalIP + ":" + GameManager.ExternalPort + "; d " + GameManager.DeviceExternalIP + "; l " + GameManager.InternalIP;
		}

		public void ShowMessage(string msg)
		{
			_messageAnim.transitions[0].displayTime = msg.Length * _timeForOneChar;
			_messageText.text = msg;
			_messageAnim.TriggerTransition();
		}

		void OnExit()
		{
			if (Application.isEditor)
			{
#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
#endif
			}
			else
			{
				Application.Quit();
			}
		}

		void OnJoinMain()
		{
			_mainPanel.TriggerFadeOut();
			_joinPanel.TriggerTransition();
		}

		void OnBack()
		{
			_mainPanel.TriggerTransition();
			_joinPanel.TriggerFadeOut();
		}

		void OnStart()
		{
			if (CheckName())
			{
				GameManager.LocalPlayerName = _nameInput.text;
				GameManager.Host();
			}
		}

		void OnJoin()
		{
			if (CheckName())
			{
				if (CheckIP())
				{
					GameManager.LocalPlayerName = _nameInput.text;
					int port = 0;
					if (!int.TryParse(_portInput.text, out port)) port = 0;
					GameManager.JoinServer(_hostInput.text, port);
				}
			}
		}

		bool CheckName()
		{
			if (string.IsNullOrEmpty(_nameInput.text))
			{
				ShowMessage(_noNameMsg);
				return false;
			}
			return true;
		}

		bool CheckIP()
		{
			if (string.IsNullOrEmpty(_hostInput.text))
			{
				ShowMessage(_noIpMsg);
				return false;
			}
			return true;
		}
	}
}