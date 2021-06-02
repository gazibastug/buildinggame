﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoggingCampBuilding : BuildingBase
{
    public override void InitBuildingFunction()
    {
        base.InitBuildingFunction();
    }

    protected override void Output()
    {
        base.Output();
    }

    protected override void Input()
    {
        base.Input();
    }

    List<TreeSystem> trees = new List<TreeSystem>();
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Tree"))
        {
            trees.Add(other.GetComponent<TreeSystem>());
        }
    }

    private TreeSystem GetNearestTree(out bool success)
    {
        TreeSystem res = null;
        float dis = Mathf.Infinity;
        for (int i = trees.Count - 1; i >= 0; i--)
        {
            if (trees[i] != null)
            {
                float curDis = GetManhattanDistance(trees[i].transform.position, transform.position);
                if (curDis < dis)
                {
                    dis = curDis;
                    res = trees[i];
                }
            }
            else
            {
                trees.Remove(trees[i]);
            }
        }
        success = res != null;
        return res;
    }

    private float GetManhattanDistance(Vector3 pos1, Vector3 pos2)
    {
        return Mathf.Abs(pos1.x - pos2.x) + Mathf.Abs(pos1.y - pos2.y) + Mathf.Abs(pos1.z - pos2.z);
    }
}
