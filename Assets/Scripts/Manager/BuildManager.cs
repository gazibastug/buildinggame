﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BuildManager : Singleton<BuildManager>
{
    #region
    [SerializeField]
    private GameObject gridHightLight;
    [SerializeField]
    private Material mat_grid_green;
    [SerializeField]
    private Material mat_grid_red;

    private BuildingBase currentBuilding;
    private bool isCurOverlap = false;//当前建筑是否重叠
    private bool isTurn = false;//当前建筑是否旋转
    private Vector2Int[] targetGrids;
    private Vector2Int lastGrid;

    private UnityAction<Vector3> moveAc = (Vector3 p) => Instance.OnMouseMoveSetBuildingPos(p);
    private UnityAction<float> rotateAc = (float dir) => Instance.OnRotateBuilding(dir);
    private UnityAction confirmAc = () => Instance.OnConfirmBuild();
    private UnityAction cancelAc = () => Instance.OnCancelBuild();
    #endregion

    private void Start()
    {
        LoadAB.Init();
        ShowGrid(false);
    }
    #region 公共函数
    public void BuildTest()
    {
        CreateBuildingOnMouse("building.l1_northhouse", "L1_Northhouse001");
    }

    /// <summary>
    /// 盖建筑
    /// </summary>
    public void CreateBuildingOnMouse(string bundleName, string pfbName)
    {
        Debug.Log("load:" + bundleName + " " + pfbName);
        GameObject pfb = LoadAB.Load(bundleName, pfbName);
        GameObject building = Instantiate(pfb, transform);
        currentBuilding = building.GetComponent<BuildingBase>();
        building.transform.position = Input.mousePosition;
        WhenStartBuild();
    }

    /// <summary>
    /// 修路
    /// </summary>
    public void CreateRoads()
    {
        EventManager.StartListening(ConstEvent.OnMouseLeftButtonDown, OnConfirmRoadStartPos);
        EventManager.StartListening(ConstEvent.OnMouseRightButtonDown, OnConfirmBuildRoad);
    }

    #endregion

    #region 私有函数

    private void OnMouseMoveSetGridPos()
    {

    }
    private void OnConfirmRoadStartPos()
    {
        EventManager.StopListening(ConstEvent.OnMouseLeftButtonDown, OnConfirmRoadStartPos);
    }

    private void OnConfirmRoadEndPos()
    {

    }
    private void OnConfirmBuildRoad()
    {
        EventManager.StopListening(ConstEvent.OnMouseLeftButtonDown, OnConfirmRoadStartPos);
        EventManager.StopListening(ConstEvent.OnMouseRightButtonDown, OnConfirmBuildRoad);
    }

    private void OnCancelLastRoad()
    {
        EventManager.StopListening(ConstEvent.OnMouseLeftButtonDown, RoadManager.Instance.BuildRoads);
    }

    private void OnMouseMoveSetBuildingPos(Vector3 p)
    {
        currentBuilding.transform.position = CalculateCenterPos(p, currentBuilding.Size, isTurn);
        gridHightLight.transform.position = CalculateCenterPos(p, Vector2Int.zero) + new Vector3(0, 0.02f, 0);
        CheckOverlap();
    }

    private void OnRotateBuilding(float dir)
    {
        currentBuilding.transform.Rotate(Vector3.up, dir, Space.World);
        isTurn = !isTurn;
        currentBuilding.transform.position = CalculateCenterPos(InputManager.Instance.LastGroundRayPos, currentBuilding.Size, isTurn);
        gridHightLight.transform.position = CalculateCenterPos(InputManager.Instance.LastGroundRayPos, Vector2Int.zero) + new Vector3(0, 0.02f, 0);
        CheckOverlap();
    }

    private void CheckOverlap()
    {
        Vector3 curPos = currentBuilding.transform.position;
        targetGrids = GetAllGrids(currentBuilding.Size.x,currentBuilding.Size.y, curPos);
        isCurOverlap = MapManager.CheckGridOverlap(targetGrids);
        gridHightLight.GetComponent<MeshRenderer>().material = isCurOverlap ? mat_grid_red : mat_grid_green;
    }
    private void OnConfirmBuild()
    {
        if (!isCurOverlap)
        {
            currentBuilding.OnConfirmBuild();
            MapManager.SetGridTypeToOccupy(targetGrids);
            MapManager.Instance.ShowGrid(targetGrids);
            WhenFinishBuild();
        }
        else
        {
            Debug.Log("当前建筑重叠，无法建造！");
        }
    }

    private void OnCancelBuild()
    {
        Destroy(currentBuilding.gameObject);
        WhenFinishBuild();
    }

    private void WhenStartBuild()
    {
        ShowGrid(true);
        isTurn = false;
        isCurOverlap = false;
        EventManager.StartListening(ConstEvent.OnGroundRayPosMove, moveAc);
        EventManager.StartListening(ConstEvent.OnMouseLeftButtonDown, confirmAc);
        EventManager.StartListening(ConstEvent.OnMouseRightButtonDown, cancelAc);
        EventManager.StartListening(ConstEvent.OnRotateBuilding, rotateAc);
    }
    private void WhenFinishBuild()
    {
        ShowGrid(false);
        EventManager.StopListening(ConstEvent.OnGroundRayPosMove, moveAc);
        EventManager.StopListening(ConstEvent.OnRotateBuilding, rotateAc);
        EventManager.StopListening(ConstEvent.OnMouseLeftButtonDown, confirmAc);
        EventManager.StopListening(ConstEvent.OnMouseRightButtonDown, cancelAc);
        EventManager.TriggerEvent(ConstEvent.OnFinishBuilding);
        currentBuilding = null;
        targetGrids = null;
    }
    private void ShowGrid(bool isShow)
    {
        gridHightLight.SetActive(isShow);
    }

    /// <summary>
    /// 对齐网格
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    private Vector3 CalculateCenterPos(Vector3 pos, Vector2Int size, bool isExchange = false)
    {
        Vector2Int vector2Int = size;
        if (isExchange)
        {
            vector2Int = new Vector2Int(size.y, size.x);
        }

        Vector3 newPos = pos;
        if (vector2Int.x % 2 != 0)
        {
            newPos.x = Mathf.Round(pos.x / 3) * 3;
        }
        else
        {
            newPos.x = (Mathf.Round(pos.x / 3 - 0.5f) + 0.5f) * 3;
        }
        if (vector2Int.y % 2 != 0)
        {
            newPos.z = Mathf.Round(pos.z / 3) * 3;
        }
        else
        {
            newPos.z = (Mathf.Round(pos.z / 3 - 0.5f) + 0.5f) * 3;
        }
        return newPos;
    }

    /// <summary>
    /// 获取当前待造建筑所占用的所有格子
    /// </summary>
    /// <returns></returns>
    private Vector2Int[] GetAllGrids(int sizeX,int sizeY, Vector3 centerPos)
    {
        int startX, endX, startZ, endZ;
        int width = isTurn ? sizeY : sizeX;
        int height = isTurn ? sizeX : sizeY;
        Vector3 centerGrid = centerPos / 3;
        if (width % 2 == 0)
        {
            startX = Mathf.FloorToInt(centerGrid.x) - width / 2 + 1;
            endX = Mathf.FloorToInt(centerGrid.x) + width / 2;
        }
        else
        {
            startX = Mathf.RoundToInt(centerGrid.x) - (width - 1) / 2;
            endX = Mathf.RoundToInt(centerGrid.x) + (width - 1) / 2;
        }
        if (height % 2 == 0)
        {
            startZ = Mathf.FloorToInt(centerGrid.z) - height / 2 + 1;
            endZ = Mathf.FloorToInt(centerGrid.z) + height / 2;
        }
        else
        {
            startZ = Mathf.RoundToInt(centerGrid.z) - (height - 1) / 2;
            endZ = Mathf.RoundToInt(centerGrid.z) + (height - 1) / 2;
        }

        Vector2Int[] grids = new Vector2Int[width * height];
        int index = 0;
        for (int i = startX; i <= endX; i ++)
        {
            for (int j = startZ; j <= endZ; j ++)
            {
                grids[index] = new Vector2Int(i, j);
                index++;
            }
        }
        return grids;
    }

    #endregion
}

