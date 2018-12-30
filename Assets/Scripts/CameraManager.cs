using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;

public class CameraManager : MonoBehaviour {

	public GameObject Player;
	private Vector3 _offset;

	// Use this for initialization
	void Start () {
		_offset = transform.position - Player.transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	private float _cameraOffset_X = 2.30f;
	private float _cameraOffset_Y = 12.00f;

    private bool _isShaking = false;
	void LateUpdate () {

        if (_isShaking){
            return;
        }

		//Debug.Log ("camera update " + transform.position);
		Vector3 newPos = Player.transform.position + _offset;
		// 位置を調整
		if (newPos.x > _cameraOffset_X) {
			newPos.x = _cameraOffset_X;
		}
		if (newPos.x < _cameraOffset_X * -1) {
			newPos.x = _cameraOffset_X * -1;
		}
		if (newPos.y > _cameraOffset_Y) {
			newPos.y = _cameraOffset_Y;
		}
		if (newPos.y < 0.1f) {
			newPos.y = 0.1f;
		}
		transform.position = newPos;
	}

    public void Shake(float sec)
    {
        _isShaking = true;
        transform.DOShakePosition(sec).OnComplete(() => { _isShaking = false; });
    }


}
