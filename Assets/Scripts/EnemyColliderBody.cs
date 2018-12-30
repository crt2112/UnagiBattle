using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyColliderBody : MonoBehaviour {

	private EnemyAI _enemyAI;
	private GameObject _player;
	private PlayerManager _playerManager;
	private Animator _playerAnimator;
	private int _playerAnimatorCurrentHash = 0;

	// Use this for initialization
	void Start () {
		_enemyAI = GetComponentInParent<EnemyAI> ();
		_player = _enemyAI.Player;
		_playerManager = _player.GetComponent<PlayerManager> ();
		_playerAnimator = _player.GetComponent<Animator> ();
	}
	
	// Update is called once per frame
	void Update () {
		AnimatorStateInfo stateInfo = _playerAnimator.GetCurrentAnimatorStateInfo (0);
		if ((_playerAnimatorCurrentHash != 0) &&
			(stateInfo.fullPathHash != Animator.StringToHash(_playerManager.PLAYER_ANIM_STATE_ATTACK)) &&
			(stateInfo.fullPathHash != Animator.StringToHash(_playerManager.PLAYER_ANIM_STATE_ATTACK_B))) {
			_playerAnimatorCurrentHash = 0;
		}
	}

	void OnTriggerEnter2D(Collider2D col)
	{
		//Debug.Log ("Enenmy Trigger Enter: " + col.tag);
		// プレイヤーの攻撃か
		if (col.tag == "PlayerAttack") {
			// 一度の攻撃で２度ヒットしないようアニメーションの状態を確認する
			AnimatorStateInfo stateInfo = _playerAnimator.GetCurrentAnimatorStateInfo (0);
			if (stateInfo.fullPathHash != _playerAnimatorCurrentHash){
				_playerAnimatorCurrentHash = stateInfo.fullPathHash;
				_enemyAI.OnDamage ();
			}
		}
	}
}
