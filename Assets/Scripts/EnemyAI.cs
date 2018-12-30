using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EnemyAI : MonoBehaviour {
    // Manager
    public GameObject Manager;
    private GameManager _gameManager;


    public GameObject Player;

	private EnemyController _enemyController;
	private Animator _animator;

	// Use this for initialization
	void Start () {
		_enemyController = GetComponent<EnemyController> ();
		_animator = GetComponent<Animator> ();
        _gameManager = Manager.GetComponent<GameManager>();
		_lastActionDateTime = DateTime.UtcNow;

	}

	private DateTime _lastActionDateTime; // 前回行動した時間
	// Update is called once per frame
	void Update () {

        var now = DateTime.UtcNow;
		if ((now - _lastActionDateTime) >= TimeSpan.FromSeconds(2f)){

            _lastActionDateTime = DateTime.UtcNow;
            if ((!_enemyController.IsAttack) && 
                (_enemyController.EnemyControllerState == EnemyControllerState.Idle))
            {
                float f = UnityEngine.Random.Range(0.0f, 100.0f);

                if (f > 90f)
                {
                    _enemyController.PunchScale();

                }
                else if (f > 60f)
                {
                    _enemyController.Fire1();
                }
                else if (f > 50f)
                {
                    _enemyController.FindPlayer(Player.transform.position);
                }
                else if (f > 40f)
                {
                    _enemyController.Move(Player.transform.position, true);
                }
                else if (f > 20f)
                {
                    _enemyController.Attack();
                }
                else if (f > 10)
                {
                    _enemyController.MoveLeft();
                }
                else
                {
                    _enemyController.MoveRight();
                }




            }
		}

        // test controll
        if (_gameManager.IsUseXBOXController)
        {
            if (Input.GetKey(KeyCode.P))
            {
                Debug.Log(string.Format("!!!!!!!!!!!!!!!!!!! On EnemyFire {0}  !!!!!!!!!!!!!!!!!!!!", System.DateTime.UtcNow));
                //_enemyController.PunchScale();
                _enemyController.Fire1();

            }
            if (Input.GetKey(KeyCode.M))
            {
                _enemyController.Move(Player.transform.position, true);
            }

            if (Input.GetKey(KeyCode.R))
            {
                _enemyController.MoveRight();
            }
            if (Input.GetKey(KeyCode.L))
            {
                _enemyController.MoveLeft();
            }
            if (Input.GetKey(KeyCode.F))
            {
                //_enemyController.FindPlayer(Player.transform.position);
                _enemyController.Fire1();
            }
            if (Input.GetKey(KeyCode.J))
            {
                _enemyController.Attack();
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

	public void OnDamage(){
		_animator.SetTrigger ("OnSuperArmor");
        _enemyController.Damage();
	}


}
