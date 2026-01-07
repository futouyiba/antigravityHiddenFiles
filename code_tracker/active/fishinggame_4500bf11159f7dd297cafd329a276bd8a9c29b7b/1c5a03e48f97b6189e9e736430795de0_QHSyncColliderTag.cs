�using System;
using Sirenix.OdinInspector;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

public enum QHColliderAreaType
{
    [LabelText("局部池")] LocalStock = 10001,
    [LabelText("开放水域")] OpenWater = 3000,
    [LabelText("水草")] WaterGrass = 1001,
    [LabelText("石头")] Stone = 1002,
    [LabelText("沉木")] Driftwood = 1003,
    [LabelText("桥墩")] PIER = 1004,
    [LabelText("深坑")] DeepPit = 1005,
    [LabelText("尖脊")] Ridge = 1006,
    [LabelText("断层")] Fault = 1007,
    [LabelText("岩架")] RockShelf = 1008,
    [LabelText("湾子")] Bay = 1009,
    [LabelText("泥底")] Mud = 1010,
    [LabelText("碎石底")] Gravel = 1011,
}

/// <summary>
/// 同步服务器的碰撞盒
/// </summary>
public class QHSyncColliderTag : MonoBehaviour
{
    public const string Tag = "SyncColliderTag";
    public const string Layout = "Ignore Raycast";
    [ShowInInspector]
    protected Collider[] _colliderList;
    public QHColliderAreaType AreaType = QHColliderAreaType.OpenWater;

    [ValidateInput("IsValidateStockName")]
    [ShowIf("@this.AreaType == QHColliderAreaType.LocalStock")]
    public string stockName;

#if UNITY_EDITOR
    private bool IsValidateStockName(string stockName)
    {
        if (string.IsNullOrEmpty(stockName))
            return false;

        return stockName.IndexOf(' ') < 0;
    }

#endif

    private void Awake()
    {
        gameObject.tag = Tag;
        gameObject.layer = LayerMask.NameToLayer(Layout);
        GetCollider();
    }

    protected Collider[] GetCollider()
    {
        if (_colliderList == null)
        {
            _colliderList = this.GetComponentsInChildren<Collider>();
        }

        return _colliderList;
    }

    public void Expansion(Vector3 expansion)
    {
        // var boxList = GetCollider();
        //斧头哥说直接+Scale
        this.gameObject.transform.localScale += expansion;
        // for (int i = 0; i < boxList.Length; i++)
        // {
        //     if (boxList[i] is BoxCollider)
        //     {
        //         var box= boxList[i] as BoxCollider;
        //         box.size += expansion;
        //     }
        // }
    }

    public bool IsHit(Vector3 pos)
    {
        foreach (var v in GetCollider())
        {

            // Vector3 closestPoint = v.ClosestPoint(pos);
            // if (Vector3.Distance(closestPoint, pos) < 0.001f) return true;

            if (v.bounds.Contains(pos))
                return true;
        }

        return false;
    }

    [Button("测试", ButtonStyle.Box)]
    public void Test(Vector3 pos)
    {
        if (IsHit(pos))
        {
            Debug.LogError($"[QHSyncColliderTag] 位置 ={pos} 命中了 {AreaType}");
        }
    }
}�"(4500bf11159f7dd297cafd329a276bd8a9c29b7b2gfile:///d:/fishinggame/Assets/Plugins/RenderingSystem/VegetationSystem/Runtime/Tag/QHSyncColliderTag.cs:file:///d:/fishinggame