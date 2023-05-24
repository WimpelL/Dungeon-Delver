using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Dray : MonoBehaviour, IFacingMover, IKeyMaster
{
    public enum eMode{ idle, move, attack, transition, knockback}

    [Header("Set in Inspector")]
    public float speed = 5;
    public float attackDuration = 0.25f;
    public float attackDelay = 0.5f;
    public float transitionDelay = 0.5f;
    public int maxHealth = 10;
    public float knockbackDuration = 0.25f;
    public float knockbackSpeed = 10;
    public float invincibleDuration = 0.5f;

    [Header("Set Dynamically")]
    public int dirHeld = -1;
    public int facing = 1;
    public eMode mode = eMode.idle;
    public int numKeys = 0;
    public bool invincible = false;
    public bool hasGrappler = false;
    public Vector3 lastSafeLoc;
    public int lastSafeFacing;

    [SerializeField]
    private int _health;



    private float timeAtkDone = 0;
    private float timeAtkNext = 0;
    private float transitionDone = 0;
    private Vector2 trasitionPos;
    private float knovbackDone = 0;
    private float invincibleDone = 0;
    private Vector3 knovbackVel;
    private SpriteRenderer sRend;


    private Rigidbody rigid;
    private Animator anim;
    private InRoom inRm;
    private Vector3[] directions = new Vector3[]
    { Vector3.right, Vector3.up, Vector3.left, Vector3.down };
    private KeyCode[] keys = new KeyCode[]
    { KeyCode.RightArrow, KeyCode.UpArrow,KeyCode.LeftArrow,KeyCode.DownArrow}; 
    void Awake()
    {
        sRend = GetComponent<SpriteRenderer>();
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        inRm = GetComponent<InRoom>();
        health = maxHealth;
        lastSafeLoc = transform.position;
        lastSafeFacing = facing;
    }
    void Update()
    {
        if(invincible && Time.time > invincibleDone) invincible = false;

        sRend.color = invincible ? Color.red : Color.white;
        if(mode == eMode.knockback)
        {
            rigid.velocity = knovbackVel;
            if(Time.time < knovbackDone) return;
        }

        if(mode == eMode.transition)
        {
            rigid.velocity = Vector3.zero;
            anim.speed = 0;
            roomPos = trasitionPos;
            if(Time.time < transitionDone) return;
            mode = eMode.idle;
        }
        // ------- Upravlenie s klaviature-----
        dirHeld = -1;
        for(int i = 0; i<4; i++)
        {
            if(Input.GetKey(keys[i])) dirHeld = i;
        }
        if(Input.GetKeyDown(KeyCode.Z) && Time.time >= timeAtkNext)
        {
            mode = eMode.attack;
            timeAtkDone = Time.time + attackDuration;
            timeAtkNext = Time.time + attackDelay;
        }
        if(Time.time >= timeAtkDone)
        {
            mode = eMode.idle;
        }
        if(mode != eMode.attack)
        {
            if(dirHeld == -1)
            {
                mode = eMode.idle;
            }
            else
            {
                facing = dirHeld;
                mode = eMode.move;
            }
        }

        Vector3 vel = Vector3.zero;

        switch(mode)
        {
            case eMode.attack:
                anim.CrossFade("Dray_Attak_" + facing, 0);
                anim.speed = 0;
                break;
            case eMode.idle:
                anim.CrossFade("Dray_Walk_" + facing,0);
                anim.speed = 0;
                break;
            case eMode.move:
                vel = directions[dirHeld];
                anim.CrossFade("Dray_Walk_" + facing,0);
                anim.speed = 1;
                break;
        }

        rigid.velocity = vel*speed;


    }
    void LateUpdate()
    {
        Vector2 rPos = GetRoomPosOnGrid(0.5f);
        int doorNum;
        for(doorNum = 0; doorNum<4; doorNum++)
        {
            if(rPos == InRoom.DOORS[doorNum])
            {
                break;
            }
        }
        if(doorNum >3 || doorNum != facing) return;
        Vector2 rm = roomNum;
        switch (doorNum)
        {
            case 0:
                rm.x += 1;
                break;
            case 1:
                rm.y += 1;
                break;
            case 2:
                rm.x -= 1;
                break;
            case 3:
                rm.y -= 1;
                break;
        }
        if(rm.x >= 0 && rm.x <= InRoom.MAX_RM_X)
        {
            if(rm.y >= 0 && rm.y <= InRoom.MAX_RM_Y)
            {
                roomNum = rm;
                trasitionPos = InRoom.DOORS[ (doorNum+2) % 4];
                roomPos = trasitionPos;
                lastSafeLoc = transform.position;
                lastSafeFacing = facing;
                mode = eMode.transition;
                transitionDone = Time.time + transitionDelay;
            }
        }
    }

    void OnCollisionEnter (Collision coll)
    {   

        if(invincible) return;
        print("invincible= "+invincible);

        DamageEffect def = coll.gameObject.GetComponent<DamageEffect>();

        if(def == null) print("def = null");
        if(def != null) print("def =! null");

        if(def == null) return;


        print("def= "+def.damage);
        health -= def.damage;
        print("health= "+health);

        invincible = true;
        invincibleDone = Time.time + invincibleDuration;

        if(def.knockback)
        {
            Vector3 delta = transform.position - coll.transform.position;
            if(Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                delta.x = (delta.x > 0) ? 1 : -1;
                delta.y = 0;
            }
            else
            {
                delta.y =(delta.y > 0) ? 1 : -1;
                delta.x = 0;
            }
            knovbackVel = delta*knockbackSpeed;
            rigid.velocity = knovbackVel;
            mode = eMode.knockback;
            knovbackDone = Time.time + knockbackDuration;
        }

    }

    void OnTriggerEnter(Collider colld)
    {
        PickUp pup = colld.GetComponent<PickUp>();
        if(pup == null) return;
        switch (pup.itemType)
        {
            case PickUp.eType.health:
                health = Mathf.Min(health+2,maxHealth);
                break;
            case PickUp.eType.key:
                keyCount++;
                break;
            case PickUp.eType.grappler:
                hasGrappler = true;
                break;
        }
        Destroy(colld.gameObject);
    }
    public void ResetInRoom(int healthLoss = 0)
    {
        transform.position = lastSafeLoc;
        facing = lastSafeFacing;
        health -= healthLoss;
        invincible = true;
        invincibleDone = Time.time + invincibleDuration; 
    }

    public int GetFacing()
    {
        return facing;
    }
    public bool moving
    {
        get{
            return(mode == eMode.move);
        }
    }
    public float GetSpeed()
    {
        return speed;
    }
    public float gridMult
    {
        get{
            return inRm.gridMult;
        }
    }
    public Vector2 roomPos
    {
        get{
            return inRm.roomPos;
        }
        set{
            inRm.roomPos = value;
        }
    }
    public Vector2 roomNum
    {
        get{
            return inRm.roomNum;
        }
        set{
            inRm.roomNum = value;
        }
    }
    public Vector2 GetRoomPosOnGrid(float mult = -1)
    {
        return inRm.GetRoomPosOnGrid(mult);
    }
    public int keyCount
    {
        get{return numKeys;}
        set{ numKeys = value;}
    }
    public int health
    {
        get{ return _health; }
        set{ _health = value;}
    }

}
