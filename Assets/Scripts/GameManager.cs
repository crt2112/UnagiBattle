using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

	#if UNITY_EDITOR
	[System.NonSerialized]
	public bool IsUseXBOXController = true;
	#else
	[System.NonSerialized]
	public bool IsUseXBOXController = false;
	#endif
	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
