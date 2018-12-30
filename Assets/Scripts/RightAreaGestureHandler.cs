using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TouchScript.Gestures;


public class RightAreaGestureHandler : MonoBehaviour {
	// Gesture
	private FlickGesture _flickGesture;
	private TapGesture _tapGesture;

	// Player
	public GameObject Player;
	private PlayerManager _playerManager;

	// Manager
	public GameObject Manager;
	private GameManager _gameManager;

	// Use this for initialization
	void Start () {
		_playerManager = Player.GetComponent<PlayerManager> ();
		_gameManager = Manager.GetComponent<GameManager> ();

	}
	
	// Update is called once per frame
	void Update () {
		if (_gameManager.IsUseXBOXController) {
			if (Input.GetButtonDown ("FireUp") || Input.GetKey(KeyCode.W)) {
				fireUp ();
			}
			if (Input.GetButtonDown ("FireDown") || Input.GetKey(KeyCode.S)) {
				fireDown ();
			}
			if (Input.GetButtonDown ("FireLeft") || Input.GetKey(KeyCode.A)) {
				fireLeft ();
			}
			if (Input.GetButtonDown ("FireRight") || Input.GetKey(KeyCode.D)) {
				fireRight ();
			}
			if (Input.GetButtonDown ("FireRButton")|| Input.GetKey(KeyCode.Space)) {
				this.fireTap();
			}
			// L/R Trigger
			/*
			float trigerInput =  Input.GetAxis("FireTrigger");
			Debug.Log ("trigger " + trigerInput);
			if (trigerInput < 0.0f) {
				Debug.Log ("Fire L Trigger");
			} else if (trigerInput > 0.0f) {
				Debug.Log ("Fire R Trigger");
			}
			if (trigerInput > 0.0f) {
				Debug.Log ("Fire R Trigger");
			}
			*/
		}
	}

	private void OnEnable()
	{
		// オブジェクトが有効化されたときにイベントハンドラを登録する
		_flickGesture = GetComponent<FlickGesture> ();
		_tapGesture = GetComponent<TapGesture> ();
		_tapGesture.Tapped += tappedHandler;
		_flickGesture.Flicked += flickedEventHandler;
		_flickGesture .FlickTime = 0.1f;
		_flickGesture.MinDistance = 0.2f;
		//Debug.Log ("MinDis " + GetComponent<FlickGesture> ().MinDistance);
		//Debug.Log ("FlickTime " + GetComponent<FlickGesture> ().FlickTime);
	}

	private void OnDisable()
	{
		// オブジェクトが無効化されたときにイベントハンドラを削除する
		_tapGesture.Tapped -= tappedHandler;
		_flickGesture.Flicked -= flickedEventHandler;
	}

	// タップイベントのイベントハンドラ
	private void tappedHandler(object sender, System.EventArgs e)
	{
		this.fireTap();
	}

	// フリックのイベントハンドラ
	private void flickedEventHandler(object sender, System.EventArgs e)
	{

		var gesture = sender as FlickGesture;

		//Debug.Log ("flick x=" + gesture.ScreenFlickVector.x + "  y=" + gesture.ScreenFlickVector.y);
		Vector2 v2 = gesture.ScreenFlickVector;

		if (Mathf.Abs (v2.x) > Mathf.Abs (v2.y)) {
			if (v2.x > 0) {
				fireRight ();
			} else {
				fireLeft ();
			}
		} else {
			if (v2.y > 0) {
				fireUp ();
			} else {
				fireDown ();
			}
		}
	}

	private void fireLeft() 
	{
		_playerManager.PushLeftButton ();
	}

	private void fireRight()
	{
		_playerManager.PushRightButton ();
	}

	private void fireUp()
	{
		//Debug.Log ("fire Up !");
		_playerManager.PushJumpButton();
	}

	private void fireDown() 
	{
		Debug.Log ("fire Down !");
	}

	private void fireTap()
	{
		_playerManager.PushAttackButton();
	}

}
