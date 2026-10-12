using System;
using UnityEngine;
using System.Collections.Generic;

public enum ArtifactGroup { Damage, Mobility, Defense, Arrow }
public enum ArtifactTargeting { Seft, Point, Direction }

[CreateAssetMenu(fileName = "New Artifact Data", menuName = "Inventory/Artifact Data")]
public class ArtifactData : ItemData
{
    [Header("Artifact")]
    [Tooltip("Chi la nhan thiet ke + luat kiem tra chi so buff (Mobility: AttackSpeed/MoveSpeed, Defense: Defense)")]
    public ArtifactGroup group;
    public ArtifactTargeting targeting = ArtifactTargeting.Seft;
    public float cooldown = 10f;
    
    [Header("Cast (castTime = 0: tuc thi, khong khoa di chuyen)")]
    [Tooltip("Thoi gian khoa di chuyen/tan cong de cast. Xong la di chuyen lai binh thuong, khong cho don trung quai")]
    public float castTime = 0f;
    [Tooltip("Thoi diem (giay ke tu luc bat dau cast) cac Action duoc kich hoat. Tu gioi han <= castTime")]
    public float executeTime = 0f;
    [Tooltip("Ten trigger trong Animator cua nhan vat (de trong = khong phat animation)")]
    public string animationTrigger;
    [Tooltip("Chi dung khi targeting = Point: khoang cach cast toi da tinh tu nguoi choi")]
    public float maxRange = 10f;
    
    [Header("Actions (thuc hien theo thu tu)")]
    [SerializeReference, SerializeReferenceDropdown]
    public List<ArtifactAction> actions = new List<ArtifactAction>();
    
    public bool LocksMovement => castTime > 0f;
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        executeTime = Mathf.Clamp(executeTime, 0f, castTime);
        
        
    }
#endif
}