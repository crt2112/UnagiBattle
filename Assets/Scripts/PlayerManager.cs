
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct DamageInfo
{
    public int InstanceID;
    public bool IsValied;
    public Vector3 EnemyPos;
    public Vector2 NockBack;
}

public class PlayerManager : MonoBehaviour {
	// アニメーション ステート
	public readonly string PLAYER_ANIM_STATE_ATTACK = "Base Layer.attack@Player";
	public readonly string PLAYER_ANIM_STATE_ATTACK_B = "Base Layer.attack_b@Player";
	public readonly string PLAYER_ANIM_STATE_ROLING = "Base Layer.roling@Player";
    public readonly string PLAYER_ANIM_STATE_DAMAGE = "Base Layer.damage@Player";

    public LayerMask FloorLayer;

	private Rigidbody2D _rbody;
	private Animator _animator;

	// Manager
	public GameObject Manager;
	private GameManager _gameManager;

    // MainCamera
    public GameObject MainCamera;
    private CameraManager _mainCameraManager;

    // Sounds
    private AudioSource _audioSource;
    public AudioClip Attack1SE;
    public AudioClip Attack2SE;
    public AudioClip Damage1SE;
    public AudioClip RolingSE;
    public AudioClip JumpSE;


	private const float MOVE_SPEED = 5;  // 移動速度固定値　
	private const float MOVE_SPEED_ROLING = 15;  // ローリング中の移動速度固定値
	private float _moveSpeed; // 移動速度
	private float _jumpPower = 600; // ジャンプの力
	private bool _goJump = false; // ジャンプしたか否か
	private bool _canJump = false; // ジャンプして良いか
	private bool _goAttack = false; // 攻撃指示があるか
	private bool _isAttack = false; // 攻撃指中か

    private bool _isDamage = false; // ダメージ中か
	private bool _goDamage = false; // ダメージ指示
	private bool _goDamageNockBack = false; // ダメージのノックバック指示
    private DamageInfo _damageInfo; // 被弾時の情報


    // Mecanim が非同期処理しているらしいので一応 volatile。が、 スレッドセーフにだから、いらないかもとも？
    volatile private bool _canAttackCombo = false; // 攻撃のコンボ受付中か
	volatile private int _comboAttackLevel = 0; // 現在何連コンボ中か
	private string _nextAttackAnimation = string.Empty; // コンボ成立時、次に再生する攻撃アニメを予約する

	private bool _goRoling = false; // ローリング指示
	private bool _isRoling = false; // ローリング中か
	// ローリング方向
	public enum ROLING_DIR
	{
		LEFT,
		RIGHT
	}
	private ROLING_DIR _rolingDirection = ROLING_DIR.RIGHT;


    // プレイヤーの向き
    public enum PLAYER_DIR
    {
        LEFT,
        RIGHT
    }

    private PLAYER_DIR _dir = PLAYER_DIR.RIGHT;
    public PLAYER_DIR Dir
    {
        get
        {
            return this._dir;
        }
         set
        {
            switch (value)
            {
                case PLAYER_DIR.LEFT:
                    transform.localScale = new Vector2(-1, 1);
                    break;
                case PLAYER_DIR.RIGHT:
                    transform.localScale = new Vector2(1, 1);
                    break;
            }
            _dir = value;
        }
    }

    public static GameObject GetGameObject()
    {
        return GameObject.FindGameObjectWithTag("Player");
    }
    public static Transform GetTranform()
    {
        return GameObject.FindGameObjectWithTag("Player").transform;
    }
    public static Animator GetAnimator()
    {
        return GameObject.FindGameObjectWithTag("Player").GetComponent<Animator>();
    }



    [SerializeField]
	private Joystick _joystick = null;

	public enum MOVE_DIR
	{
		STOP,
		LEFT,
		RIGHT,
	}
	private MOVE_DIR _moveDirection = MOVE_DIR.STOP; // 移動方向

	// Use this for initialization
	void Start () {
        _damageInfo.IsValied = false;
        _damageInfo.InstanceID = -1;
		_rbody = GetComponent<Rigidbody2D> ();
		_animator = GetComponent<Animator> ();
		_gameManager = Manager.GetComponent<GameManager> ();
        _mainCameraManager = MainCamera.GetComponent<CameraManager>();
        _audioSource = Manager.GetComponent<AudioSource>();
	}

	// Update is called once per frame
	void Update () {

        //------------------------
        // ダメージ
        //------------------------
        // ダメージ中かの判定
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.fullPathHash == Animator.StringToHash(PLAYER_ANIM_STATE_DAMAGE))
        {
            _isDamage = true;
        }
        else
        {
            _isDamage = false;
        }
        if (_isDamage)
        {
			_goDamage = false;
			//_goDamageNockBack = false;
            _goAttack = false;
            _goRoling = false;
            _canAttackCombo = false;
            _canJump = false;
            return;
        }
		// ダメージ実行
		if (_goDamage) {
			_goDamage = false;
			_goAttack = false;
			_goRoling = false;
			_canJump = false;
			_moveDirection = MOVE_DIR.STOP;
			_animator.SetBool ("isWalk", false);
			_animator.SetBool ("onFloor", true);
			// ノックバック
			_animator.SetTrigger ("onDamage");
            _animator.SetTrigger("onSuperArmor");
            _mainCameraManager.Shake(0.3f);
			_goDamageNockBack = true; // ノックバックは AddForce するので FixedUpdate で処理する
			return;
		}

        // 床への設置判定
        bool isFloor = 
			Physics2D.Linecast (transform.position - (transform.right * 0.3f),
				transform.position - (transform.up * 0.1f), FloorLayer) ||
			Physics2D.Linecast (transform.position + (transform.right * 0.3f),
				transform.position - (transform.up * 0.1f), FloorLayer);
		
		//------------------------
		// 攻撃
		//------------------------
		// 攻撃中かの判定
		stateInfo = _animator.GetCurrentAnimatorStateInfo (0);
		if ((stateInfo.fullPathHash == Animator.StringToHash (PLAYER_ANIM_STATE_ATTACK)) ||
			(stateInfo.fullPathHash == Animator.StringToHash (PLAYER_ANIM_STATE_ATTACK_B)) ){
			_isAttack = true;
		} else {
			_isAttack = false;
		}
		if (_isAttack) {
			if (_goAttack) {
				// 攻撃中に攻撃指示あり
				if (_canAttackCombo) {
					_canAttackCombo = false;
					// 次に再生する攻撃アニメを設定しておく
					// このタイミングでアニメを切り替えると、中半端なフレームで切り替わるかもなので
					// アニメーションクリップのイベントで切り替える
					switch (_comboAttackLevel) {
					case 1: 
						_nextAttackAnimation = PLAYER_ANIM_STATE_ATTACK_B;
						break;
						default:
						_nextAttackAnimation = string.Empty;
						break;
					}
				}
			}
			_goAttack = false;
			_goRoling = false;
			return;
		}
		// 攻撃実行
		if (_goAttack) {
			_goAttack = false;
			_goRoling = false;
			_canJump = false;
			_moveDirection = MOVE_DIR.STOP;
			_animator.SetBool ("isWalk", false);
			_animator.SetBool ("onFloor", true);
			_animator.SetTrigger ("onAttack1");
            _audioSource.PlayOneShot(Attack1SE);
			return;
		}
			
		//------------------------
		// ローリング
		//------------------------
		// ローリング中かの判定
		stateInfo = _animator.GetCurrentAnimatorStateInfo (0);
		if (stateInfo.fullPathHash == Animator.StringToHash (PLAYER_ANIM_STATE_ROLING)) {
			_isRoling = true;
		} else {
			_isRoling = false;
		}
		if (_isRoling) {
			// ローリング中の指示は取り消し
			_goAttack = false;
			_goRoling = false;
			return;
		}
		// ローリング実行
		if (_goRoling) {
			_goAttack = false;
			_goRoling = false;
			_canJump = false;
			_moveDirection = MOVE_DIR.STOP;
			_animator.SetBool ("isWalk", false);
			_animator.SetBool ("onFloor", true);
			_animator.SetTrigger("onRoling");
            _audioSource.PlayOneShot(RolingSE);
			return;
		}

		//------------------------
		// 移動・ジャンプ
		//------------------------
		_canJump = true;
		// 移動判定
		if (_gameManager.IsUseXBOXController) {
			// コントローラー操作
			float x = Input.GetAxisRaw("Horizontal");
			if (x == 0) {
				_moveDirection = MOVE_DIR.STOP;
			} else {
				if (x < 0) {
					_moveDirection = MOVE_DIR.LEFT;
				} else {
					_moveDirection = MOVE_DIR.RIGHT;
				}
			}
		} else {
			// スマホ操作
			if (_joystick.Position.x > 0.01f) {
				_moveDirection = MOVE_DIR.RIGHT;
			} else  
			{
				if (_joystick.Position.x < -0.01f)
				{
					_moveDirection = MOVE_DIR.LEFT;
				} else {
					_moveDirection = MOVE_DIR.STOP;
				}
			}
		}

		// アニメーション切り替え
		if (isFloor){
			// 床に設定している
			_animator.SetBool ("onFloor", true);
			bool isWalk = _moveSpeed != 0;
			_animator.SetBool ("isWalk", isWalk);
		} else {
			// 空中にいる
			_animator.SetBool ("isWalk", false);
			_animator.SetBool ("onFloor", false);
		}

	}

    void FixedUpdate (){


		// 攻撃中は停止
		if (_isAttack) {
			_rbody.isKinematic = true;
			_rbody.velocity = Vector2.zero;
			return;
		} else {
			_rbody.isKinematic = false;
		}

		// ダメージノックバック
		if (_goDamageNockBack) {
			_goDamageNockBack = false;
			// 敵の方を向く
            if (_damageInfo.IsValied)
            {
                Debug.Log(string.Format("--- go nock back {0}  ----------------", System.DateTime.UtcNow));
                _damageInfo.IsValied = false;
                this.Dir = (this.transform.position.x < _damageInfo.EnemyPos.x) ? PLAYER_DIR.RIGHT : PLAYER_DIR.LEFT;
                Debug.Log(string.Format("--- dir {0}  ----------------", this.Dir));
                _rbody.velocity = new Vector2(0.0f, 0.0f);
                //this.AddForceAnimatorVy(_damageEnemyController.attackNockBackVector.x);
                //this.AddForceAnimatorVy(_damageEnemyController.attackNockBackVector.y);
                _audioSource.PlayOneShot(Damage1SE);
                this.AddForceAnimator(_damageInfo.NockBack);
            }
            else 
            {
                Debug.Log(string.Format("--- not go nock back {0}  ----------------", System.DateTime.UtcNow));
                 
            }
            return;
		}
        if (_isDamage)
        {
            return;
        }


		// 移動処理
		if (!_isRoling) {
			// 通常の移動
			switch (_moveDirection) {
			case MOVE_DIR.STOP:
				_moveSpeed = 0;
				break;
			case MOVE_DIR.LEFT:
				_moveSpeed = MOVE_SPEED * -1;
                Dir = PLAYER_DIR.LEFT;
				break;
			case MOVE_DIR.RIGHT:
    			_moveSpeed = MOVE_SPEED;    
                Dir = PLAYER_DIR.RIGHT;
                break;
			}
			_rbody.velocity = new Vector2 (_moveSpeed, _rbody.velocity.y);
		} else {
			// ローリングの移動
			switch (_rolingDirection) {
			case ROLING_DIR.LEFT:
				_moveSpeed = MOVE_SPEED_ROLING * -1;
                Dir =  PLAYER_DIR.LEFT;
				break;
			case ROLING_DIR.RIGHT:
				_moveSpeed = MOVE_SPEED_ROLING;
                Dir = PLAYER_DIR.RIGHT;
                break;
			}
			// ローリングになったら Y 軸上の速度はリセットする(空中でも平行に移動する)
			_rbody.velocity = new Vector2 (_moveSpeed, 0.0f);
		}

		// ジャンプ処理
		if (_goJump) {
			// Y 軸上に速度がある場合、AddForce が累乗されないようにする
			if (Mathf.Abs(_rbody.velocity.y) > 0.0f) {
				Vector2 v2 = _rbody.velocity;
				v2.y = 0.0f;
				_rbody.velocity = v2;
			}

			_rbody.AddForce (Vector2.up * _jumpPower);
            _audioSource.PlayOneShot(JumpSE);
			_goJump = false;
		}
	}

    public void AddForceAnimator(Vector3 vec)
    {
        Vector2 tmpVec = new Vector3(0f, 0f, 0f);
        if (vec.x != 0.0f)
        {
            int i = this.Dir == PLAYER_DIR.LEFT ? 1 : -1;
            tmpVec.x = vec.x * i;
        }
        if (vec.y != 0.0f)
        {
            tmpVec.y = vec.y;
        }
        //addForceVxEnabled = true;
        //addForceVxStartTime = Time.fixedTime;


        _rbody.AddForce(tmpVec);
    }

    public void AddForceAnimatorVx(float vx)
    {
        //Debug.Log (string.Format("--- AddForceAnimatorVx {0} ----------------",vx));
        if (vx != 0.0f)
        {
            int i = this.Dir == PLAYER_DIR.LEFT ? 1 : -1;
            _rbody.AddForce(new Vector2(vx * i, 0.0f));
            //addForceVxEnabled = true;
            //addForceVxStartTime = Time.fixedTime;
        }
    }

    public void AddForceAnimatorVy(float vy)
    {
        //Debug.Log (string.Format("--- AddForceAnimatorVy {0} ----------------",vy));
        if (vy != 0.0f)
        {
            _rbody.AddForce(new Vector2(0.0f, vy));


            //_rbody.AddForce(new Vector2(-1000.0f, 0.0f));

            //jumped = true;
            //umpStartTime = Time.fixedTime;
        }
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        if (_damageInfo.InstanceID == other.GetInstanceID()) {
            return;
        }
        // トリガーチェック
        if (other.tag == "EnemyAttack_Arm")
        {
            //Debug.Log(string.Format("EnemyArm Hit {0}",ec.attackEnable));
            //if (enemyCtrl.attackEnabled)  // attackEnabled プロパティ不要かも
            {
                var enemy  = other.GetComponentInParent<EnemyController>();
                _damageInfo.InstanceID = other.GetInstanceID();
                _damageInfo.IsValied = true;
                _damageInfo.EnemyPos = enemy.transform.position;
                _damageInfo.NockBack = enemy.attackNockBackVector;

                Debug.Log(string.Format("--- go damage on Arm {0}  ----------------", System.DateTime.UtcNow));
                _goDamage = true;
            }
        }
        else if (other.tag == "Enemy_Megane")
        {
            //Debug.Log(string.Format("EnemyArm Hit {0}",ec.attackEnable));
            //if (enemyCtrl.attackEnabled)  // attackEnabled プロパティ不要かも
            {
                var enemy = other.GetComponentInParent<Enemy_A_MeganeManager>();
                _damageInfo.InstanceID = other.GetInstanceID();
                _damageInfo.IsValied = true;
                _damageInfo.EnemyPos = enemy.transform.position;
                _damageInfo.NockBack = enemy.attackNockBackVector;
                Debug.Log(string.Format("--- go damage on Megane {0}  ----------------", System.DateTime.UtcNow));
                _goDamage = true;
            }
        }

        /*
        else if (other.tag == "EnemyArmBullet")
        {
            FireBullet fireBullet = other.transform.GetComponent<FireBullet>();
            if (fireBullet.attackEnabled)
            {
                fireBullet.attackEnabled = false;
                playerCtrl.dir = (playerCtrl.transform.position.x < fireBullet.transform.position.x) ? +1 : -1;
                playerCtrl.AddForceAnimatorVx(-fireBullet.attackNockBackVector.x);
                playerCtrl.AddForceAnimatorVy(fireBullet.attackNockBackVector.y);
                playerCtrl.ActionDamage(fireBullet.attackDamage);
                Destroy(other.gameObject);
            }
        }

        else if (other.tag == "Item")
        {
            if (other.name == "Item_Koban")
            {
                PlayerController.score += 10;
                AppSound.instance.SE_ITEM_KOBAN.Play();
            }
            else
            if (other.name == "Item_Ohoban")
            {
                PlayerController.score += 100000;
                AppSound.instance.SE_ITEM_OHBAN.Play();
            }
            else
            if (other.name == "Item_Hyoutan")
            {
                playerCtrl.SetHP(playerCtrl.hp + playerCtrl.hpMax / 3, playerCtrl.hpMax);
                AppSound.instance.SE_ITEM_HYOUTAN.Play();
            }
            else
            if (other.name == "Item_Makimono")
            {
                playerCtrl.superMode = true;
                playerCtrl.GetComponent<Stage_AfterImage>().afterImageEnabled = true;
                playerCtrl.basScaleX = 2.0f;
                playerCtrl.transform.localScale = new Vector3(playerCtrl.basScaleX, 2.0f, 1.0f);
                Invoke("SuperModeEnd", 10.0f);
                AppSound.instance.SE_ITEM_MAKIMONO.Play();
            }
            else
            if (other.name == "Item_Key_A")
            {
                PlayerController.score += 10000;
                PlayerController.itemKeyA = true;
                GameObject.Find("Stage_Item_Key_A").GetComponent<SpriteRenderer>().enabled = true;
                AppSound.instance.SE_ITEM_KEY.Play();
            }
            else
            if (other.name == "Item_Key_B")
            {
                PlayerController.score += 10000;
                PlayerController.itemKeyB = true;
                GameObject.Find("Stage_Item_Key_B").GetComponent<SpriteRenderer>().enabled = true;
                AppSound.instance.SE_ITEM_KEY.Play();
            }
            else
            if (other.name == "Item_Key_C")
            {
                PlayerController.score += 10000;
                PlayerController.itemKeyC = true;
                GameObject.Find("Stage_Item_Key_C").GetComponent<SpriteRenderer>().enabled = true;
                AppSound.instance.SE_ITEM_KEY.Play();
            }
            Destroy(other.gameObject);
        }
        */
    }


    public void PushJumpButton() {
		if (_canJump) {
			_goJump = true;
		}
	}

	public void PushAttackButton() {
		_goAttack = true;
	}

	public void PushLeftButton() {
		if (!_isRoling) {
			_rolingDirection = ROLING_DIR.LEFT;
			_goRoling = true;
		}
	}

	public void PushRightButton() {
		if (!_isRoling) {
			_rolingDirection = ROLING_DIR.RIGHT;
			_goRoling = true;
		}
	}

	// PLAYER_ANIM_STATE_ATTACK クリップから呼ばれるイベント(Param: 現在のコンボ状況)
	// コンボの受付開始
	public void OnEnterComboEnableZone_Attack(int currentComboLevel){
		// コンボ受付開始
		_canAttackCombo = true;
		_comboAttackLevel = currentComboLevel;
	}
		
	// PLAYER_ANIM_STATE_ATTACK クリップから呼ばれるイベント
	// コンボの受付終了
	public void OnLeaveComboEnableZone_Attack(){
		// コンボ受付終了
		_canAttackCombo = false;
		// コンボが確定していたらコンボアニメメーション開始
		if (_nextAttackAnimation != string.Empty) {
			var str = _nextAttackAnimation;
			_nextAttackAnimation = string.Empty;
			_animator.Play (str);
            _audioSource.PlayOneShot(Attack2SE);

		}
	}

}
