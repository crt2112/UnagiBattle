using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;

// 重複させないアクション
public enum EnemyControllerState {
    Idle,
    Move,
    FindPlayer,
    PunchScale,
    MoveLeft,
    MoveRight,

}

public class EnemyController : MonoBehaviour
{
    // アニメーション ステート
    public readonly string ENEMY_ANIM_STATE_ATTACK = "Base Layer.attack@Enemy";
    public readonly string ENEMY_ANIM_STATE_ATTACK_B = "Base Layer.attack_b@Enemy";
    public readonly string ENEMY_ANIM_STATE_ATTACK_FIRE1 = "Base Layer.fire1@Enemy";

    private Animator _animator;
    private GameObject _player;
    private EnemyAI _enemyAI;

    public GameObject Manager;

    private AudioSource _audioSource;
    public AudioClip SEDamage;
    public AudioClip SEPunchBig;
    public AudioClip SEPunchSmall;
    public AudioClip SEFire;
    public AudioClip SEFind;
    public AudioClip SEMove;
    public AudioClip SEAttackBefore;
    public AudioClip SEAttack;

    public GameObject ComboSystemObject;
    private ComboSystem _comboSystem;


    [System.NonSerialized]
    public bool attackEnabled = false;
    [System.NonSerialized]
    public Vector2 attackNockBackVector = Vector3.zero;

    // 併用しない状態
    public EnemyControllerState EnemyControllerState
    {
        get
        {
            if ((_seqMove != null) && (_seqMove.IsPlaying()))
            {
                return EnemyControllerState.Move;
            }
            if ((_seqFindPlayer != null) && (_seqFindPlayer.IsPlaying()))
            {
                return EnemyControllerState.FindPlayer;
            }
            if ((_seqPunchScale != null) && (_seqPunchScale.IsPlaying()))
            {
                return EnemyControllerState.PunchScale;
            }
            if ((_seqMoveLeft != null) && (_seqMoveLeft.IsPlaying()))
            {
                return EnemyControllerState.MoveLeft;
            }
            if ((_seqMoveRight != null) && (_seqMoveRight.IsPlaying()))
            {
                return EnemyControllerState.MoveRight;
            }
            return EnemyControllerState.Idle;
        }
    }
    // EnemyControllerState と併用するかもしれない状態
    private bool _isAttack = false;
    public bool IsAttack
    {
        get
        {
            return _isAttack;
        }
    }

    // シーケンス
    Sequence _seqMove;
    Sequence _seqFindPlayer;
    Sequence _seqPunchScale;
    Sequence _seqMoveLeft;
    Sequence _seqMoveRight;

    // Use this for initialization
    void Start()
    {
        _enemyAI = GetComponentInParent<EnemyAI>();
        _player = _enemyAI.Player;
        _animator = GetComponent<Animator>();
        _audioSource = Manager.GetComponent<AudioSource>();
        _comboSystem = ComboSystemObject.GetComponent<ComboSystem>();

    }

    // Update is called once per frame
    void Update()
    {
        // 攻撃中かの判定
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if ((stateInfo.fullPathHash == Animator.StringToHash(ENEMY_ANIM_STATE_ATTACK)) ||
            (stateInfo.fullPathHash == Animator.StringToHash(ENEMY_ANIM_STATE_ATTACK_FIRE1)) ||
            (stateInfo.fullPathHash == Animator.StringToHash(ENEMY_ANIM_STATE_ATTACK_B)))
        {
            _isAttack = true;
        }
        else
        {
            _isAttack = false;
        }

    }

    // ダメージを受けた時
    public void Damage() {
        _audioSource.PlayOneShot(SEDamage);
        _comboSystem.IncreaseCombo();
    }

    // プレイヤーのほうを向く
    public void LookPlayer()
    {

        Vector2 v2 = transform.localScale;

        // 移動方向によって表示向きをかえる
        if (transform.position.x > _player.transform.position.x)
        {
            // 左を向く
            //transform.localScale = new Vector2 (1,1); 
            transform.localScale = new Vector2(Mathf.Abs(v2.x), v2.y);
        }
        else if (transform.position.x < _player.transform.position.x)
        {
            // 右を向く
            //transform.localScale = new Vector2 (-1,1); 
            transform.localScale = new Vector2(0f - Mathf.Abs(v2.x), v2.y);
        }

    }

    public void Attack()
    {
        if (_isAttack)
        {
            return;
        }

        LookPlayer();
        this.attackNockBackVector = new Vector2(300.0f, 300.0f);
        _animator.SetTrigger("OnAttack1");
        _audioSource.PlayOneShot(SEAttackBefore);

    }

    public void Fire1()
    {
        if (_isAttack)
        {
            return;
        }

        LookPlayer();
        _animator.SetTrigger("OnFire1");
        _audioSource.PlayOneShot(SEFire);

    }

    public void Move(Vector3 v3, bool andAttack)
    {
        if ((_seqMove != null) && (_seqMove.IsPlaying()))
        {
            return;
        }

        _seqMove = DOTween.Sequence();

        if (andAttack)
        {
            // 移動ご攻撃あり
            _seqMove.Append(transform.DOLocalMove(v3, 0.5f, false).SetEase(Ease.OutQuad).OnComplete(Attack));
        }
        else
        {
            // 単純に移動だけ
            _seqMove.Append(transform.DOLocalMove(v3, 0.5f, false).SetEase(Ease.OutQuad));
            // Join だと最終のアニメーションと合成しながら
            //seq.Join(transform.DOLocalMove(v3, 0.5f, false));
        }
        _audioSource.PlayOneShot(SEMove);


    }

    public void FindPlayer(Vector3 playerV3)
    {
        if ((_seqFindPlayer != null) && (_seqFindPlayer.IsPlaying()))
        {
            return;
        }
        // 探すふりアニメーション
        _seqFindPlayer = DOTween.Sequence();
        Quaternion org_q3 = transform.rotation;
        Vector3 rotate_v3;
        rotate_v3.x = org_q3.x;
        rotate_v3.y = org_q3.y;
        rotate_v3.z = org_q3.z;
        rotate_v3.y = 180;
        _seqFindPlayer.Append(transform.DOLocalRotate(rotate_v3, 0.5f, RotateMode.Fast));
        rotate_v3.y = 0;
        _seqFindPlayer.Append(transform.DOLocalRotate(rotate_v3, 0.5f, RotateMode.Fast));
        rotate_v3.y = 180;
        _seqFindPlayer.Append(transform.DOLocalRotate(rotate_v3, 0.5f, RotateMode.Fast));
        rotate_v3.y = 0;
        _seqFindPlayer.Append(transform.DOLocalRotate(rotate_v3, 0.5f, RotateMode.Fast).OnComplete(LookPlayer));
        _audioSource.PlayOneShot(SEFind);
    }

    private bool _isPunchScale = false;
    private const float PUNCH_SCALE = 2.0f;
    /// <summary>
    /// 拡大・縮小を繰り返すアニメーション
    /// </summary>
    public void PunchScale()
    {
        if ((_seqPunchScale != null) && (_seqPunchScale.IsPlaying()))
        {
            Debug.Log("skip punch scale");
            return;
        }
        _seqPunchScale = DOTween.Sequence();

        if (!_isPunchScale)
        {
            Debug.Log("punch - big");
            _isPunchScale = true;

            //Sequence seq = DOTween.Sequence().SetId("ID_PunshScanle"); // sequence に ID をつける
            // 指定 ID の tween を全て破棄しつつ OnComplete を呼ぶ(Kill(True)) 
            //DOTween.TweensById("ID_PunshScanle").ForEach((tween) => { tween.Kill(true); });

            _seqPunchScale.Append(transform.DOPunchScale(new Vector3(PUNCH_SCALE, PUNCH_SCALE, PUNCH_SCALE), 2.5f, 3, 0));
            _seqPunchScale.Append(transform.DOScale(new Vector3(PUNCH_SCALE, PUNCH_SCALE, PUNCH_SCALE), 0.5f));
            _audioSource.PlayOneShot(SEPunchBig);
        }
        else
        {
            Debug.Log("punch - small");
            _isPunchScale = false;
            _seqPunchScale.Append(transform.DOPunchScale(new Vector3(1f, 1f, 1f), 2.5f, 3, 0));
            _seqPunchScale.Append(transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f));
            _audioSource.PlayOneShot(SEPunchSmall);
        }
    }

    public void MoveLeft()
    {
        if ((_seqMoveLeft != null) && (_seqMoveLeft.IsPlaying()))
        {
            return;
        }
        _seqMoveLeft = DOTween.Sequence();

        // x 移動
        float x = transform.position.x;
        _seqMoveLeft.Append(transform.DOLocalMoveX(x - 2.0f, 0.5f));

        // Y 移動
        //seq.Append (transform.DOLocalMoveY(1.5f, 1.0f));
        //seq.Append (transform.DOLocalMoveY(-1.5f, 1.0f));

        // サイズ変更。敵が奥に逃げるのとかに使えそう
        //seq.Append(transform.DOScale(0.5f, 1.0f));
        //seq.Append(transform.DOScale(1.5f, 1.0f));
        //seq.Append(transform.DOScale(1.0f, 1.0f));

        // 透過
        //SpriteRenderer sprite = transform.GetComponent<SpriteRenderer>();
        //seq.Append(DOTween.ToAlpha (() => sprite.color, a => sprite.color = a, 0.0f, 1.0f));
        //seq.Append(DOTween.ToAlpha (() => sprite.color, a => sprite.color = a, 1.0f, 1.0f));


    }

    public void MoveRight()
    {
        if ((_seqMoveRight != null) && (_seqMoveRight.IsPlaying()))
        {
            return;
        }
        _seqMoveRight = DOTween.Sequence();
        // x 移動
        // x 移動
        float x = transform.position.x;
        _seqMoveRight.Append(transform.DOLocalMoveX(x + 2.0f, 0.5f));
    }

    public GameObject Fire1Object;
    // Fire1 アニメーションイベント
    public void ActionFire1()
    {
        Transform goFire = transform.Find("Enemy_A_Muzzle1");
        GameObject go = Instantiate(Fire1Object, goFire.position, Quaternion.identity) as GameObject;
        var meganeManager = go.GetComponent<Enemy_A_MeganeManager>();
        meganeManager.ownwer = transform;
        if (_isPunchScale)
        {
            meganeManager.transform.localScale = new Vector3(PUNCH_SCALE, PUNCH_SCALE, PUNCH_SCALE);
        }

    }

}
