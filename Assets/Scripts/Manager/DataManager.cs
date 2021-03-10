﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : ScriptableObject
{
    static DataManager mInstance;
    public static DataManager Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = Resources.Load<DataManager>("Data/ScriptData/Data");
            }
            return mInstance;
        }
    }

    public ItemData[] ItemArray;
    public LevelData[] LevelArray;
    public TechData[] TechArray;
    public BuildData[] BuildArray;
    public LocalizationData LocalizationData;
    public Dictionary<BuildTabType, List<BuildData>> TabDic = new Dictionary<BuildTabType, List<BuildData>>();
    public static string[] foodNames;
    public static int[] foodIds;


    public void InitTabDic()
    {
        for (int i = 0; i < BuildArray.Length; i++)
        {
            BuildTabType tabType = BuildArray[i].tabType;
            if (TabDic.ContainsKey(tabType))
            {
                List<BuildData> buildDatas = TabDic[tabType];
                buildDatas.Add(BuildArray[i]);
                TabDic[tabType] = buildDatas;
            }
            else
            {
                List<BuildData> buildDatas = new List<BuildData>();
                buildDatas.Add(BuildArray[i]);
                TabDic.Add(tabType, buildDatas);
            }
        }
        Debug.Log("创建TabDic成功！");
    }

    public static LevelData GetLevelData(int levelId)
    {
        for (int i = 0; i < Instance.LevelArray.Length; i++)
        {
            if (Instance.LevelArray[i].Id == levelId)
            {
                return Instance.LevelArray[i];
            }
        }
        Debug.Log("无效的关卡ID");
        return null;
    }

    public static string GetItemNameById(int ID)
    {
        for (int i = 0; i < Instance.ItemArray.Length; i++)
        {
            if (Instance.ItemArray[i].Id == ID)
            {
                return Instance.ItemArray[i].Name;
            }
        }
        Debug.Log("无效的物品ID"+ ID);
        return string.Empty;
    }

    public static string[] GetFoodNameList()
    {
        if (foodNames == null)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < Instance.ItemArray.Length; i++)
            {
                if (Instance.ItemArray[i].ItemType == (int)ItemType.food)
                {
                    list.Add(Instance.ItemArray[i].Name);
                }
            }
            foodNames = list.ToArray();
        }
        return foodNames;
    }

    public static int[] GetFoodIDList()
    {
        if (foodIds == null)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < Instance.ItemArray.Length; i++)
            {
                if (Instance.ItemArray[i].ItemType == (int)ItemType.food)
                {
                    list.Add(Instance.ItemArray[i].Id);
                }
            }
            foodIds = list.ToArray();
        }
        return foodIds;
    }
    public static int GetItemIdByName(string name)
    {
        for (int i = 0; i < Instance.ItemArray.Length; i++)
        {
            if (Instance.ItemArray[i].Name == name)
            {
                return Instance.ItemArray[i].Id;
            }
        }
        Debug.Log("无效的物品名称" + name);
        return 0;
    }
}
