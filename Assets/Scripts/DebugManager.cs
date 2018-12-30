using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour {
	public GameObject Manager;
	private GameManager _gameManager;

	public GameObject TextUseXBOXController;

	// Use this for initialization
	void Start () {
		_gameManager = Manager.GetComponent<GameManager> ();
		TextUseXBOXController.SetActive (_gameManager.IsUseXBOXController);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
