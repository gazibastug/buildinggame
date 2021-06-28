﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : Singleton<MapManager>
{
    public Vector2Int MapSize { get; private set; }
    private GridNode[][] _grids;
    private LevelData _leveldata;
    private static Vector3[] _vertices;//存储地形顶点数据
    public const int unit = 2;//地形一格的长度（单位米）
    public const int TexLength = 8;//贴图长度为8
    public List<BuildingBase> _buildings = new List<BuildingBase>();
    [SerializeField] private static TerrainGenerator generator;
    //[SerializeField] GameObject gridPfb;
    public static string noticeContent;
    private Dictionary<Vector2Int, BuildingBase> _buildingEntryDic;
    private MapData mapData;

    public void InitMapMnager()
    {
        InitLevelData();
        InitGrid(GameManager.saveData);
        InitBuilidngEntryDic();
    }
    /// <summary>
    /// 加载关卡的时候调用
    /// </summary>
    private void InitLevelData()
    {
        //MapSize = GameManager.saveData.mapSize.Vector2Int;
        generator = GameObject.Find("TerrainGenerator").GetComponent<TerrainGenerator>();
        mapData = TerrainGenerator.Instance.GetMapData();
    }
    private void InitGrid(SaveData saveData)
    {
        /*if (MapSize == null || MapSize.x <= 0 || MapSize.y <= 0)
        {
            Debug.LogError("地图尺寸没有初始化！");
            return;
        }*/
        if (saveData != null)
        {
            Debug.Log("InitGrids");
            _grids = saveData.gridNodes;
            MapSize = saveData.mapSize.Vector2Int;
        }

        Debug.Log("初始化中");
        Invoke("InitRoad", 0.017f);
    }


    private void InitBuilidngEntryDic()
    {
        _buildingEntryDic = new Dictionary<Vector2Int, BuildingBase>();
    }
    [ContextMenu("SetUp")]
    public void SetUp()
    {
        _grids = SetUpGrid();
    }

    public void AddBuildingEntry(Vector2Int entry,BuildingBase building)
    {
        if (!_buildingEntryDic.ContainsKey(entry))
        {
            _buildingEntryDic.Add(entry, building);
        }
        else
        {
            Debug.Log("这个入口已经被占用：" + entry);
            Debug.Log(_buildingEntryDic.Keys.Count);
        }
    }

    public bool IsBuildingEntryAvalible(Vector2Int entry)
    {
        return !_buildingEntryDic.ContainsKey(entry);
    }

    public void RemoveBuildingEntry(Vector2Int entry)
    {
        if (_buildingEntryDic.ContainsKey(entry))
        {
            _buildingEntryDic.Remove(entry);
        }
        else
        {
            Debug.LogError("输入的建筑入口不合法");
        }
    }

    public BuildingBase GetBuilidngByEntry(Vector2Int entry)
    {
        if (_buildingEntryDic.ContainsKey(entry))
        {
            return _buildingEntryDic[entry];
        }
        else
        {
            //当目标建筑被拆除了，会返回空
            return null;
        }
    }
    public GridNode[][] GetGrids()
    {
        return _grids;
    }

    public GridNode[][] SetUpGrid()
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        _leveldata = DataManager.GetLevelData(30001);
        MapSize = new Vector2Int(_leveldata.Width, _leveldata.Length);

        Mesh mesh = TerrainGenerator.Instance.GetComponent<MeshFilter>().sharedMesh;
        Vector2[] uv = mesh.uv;
        Vector3[] verticles = mesh.vertices;
        int texLength = 8;
        _grids = new GridNode[MapSize.x][];
        for (int i = 0; i < MapSize.x; i++)
        {
            _grids[i] = new GridNode[MapSize.y];
            for (int j = 0; j < MapSize.y; j++)
            {
                int index = (i + j * MapSize.y) * 4;
                int x = Mathf.FloorToInt(uv[index].x * texLength);
                int y = Mathf.FloorToInt(uv[index].y * texLength);
                int tex = 8 * (7 - y) + x;
                float deltaX = -(x * 0.125f + 0.0625f - uv[index].x);
                float deltaY = -(y * 0.125f + 0.0625f - uv[index].y);
                int dir = GetDir(deltaX, deltaY);
                _grids[i][j] = new GridNode();
                _grids[i][j].x = i;
                _grids[i][j].y = j;
                _grids[i][j].passSpeed = GetPassSpeed(tex);
                _grids[i][j].enterCost = GetEnterCost(tex);
                _grids[i][j].direction = (Direction)dir;
                _grids[i][j].height = verticles[index].y;
                /*if(i == 152)
                {
                    Debug.Log(deltaX+" "+deltaY);
                    Debug.Log(dir);
                }*/
                _grids[i][j].gridType = GetGridType(tex);
                /*if (GetTerrainPosition(_grids[i][j].GridPos.Vector2Int).y<9.1F)
                {
                    _grids[i][j].gridType = GridType.water;
                }*/
            }
        }
        for (int i = 0; i < MapSize.x; i++)
        {
            for (int j = 0; j < MapSize.y; j++)
            {
                if (i % 2 == 0 && j < MapSize.y - 1)
                {
                    _grids[i][j + 1].AddNearbyNode(_grids[i][j]);
                }
                else if (j > 0)
                {
                    _grids[i][j - 1].AddNearbyNode(_grids[i][j]);
                }
                if (j % 2 == 0 && i < MapSize.x - 1)
                {
                    _grids[i][j].AddNearbyNode(_grids[i + 1][j]);
                }
                else if (i > 0)
                {
                    _grids[i][j].AddNearbyNode(_grids[i - 1][j]);
                }
            }
        }
        System.TimeSpan dt = sw.Elapsed;
        Debug.Log("记录地图耗时:" + dt.TotalSeconds + "秒");
        return _grids;
    }

    public int GetDir(float deltaX, float deltaY)
    {
        if (deltaX > 0)
        {
            if (deltaY > 0)
            {
                return 0;
            }
            else
            {
                return 3;
            }
        }
        else
        {
            if (deltaY > 0)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }
    }

    public GridType GetGridType(int tex)
    {
        if(tex == 4 || tex == 5 || tex == 6
           ||tex == 9 || tex == 10 || tex == 8
           || tex == 12 || tex == 13 || tex == 14)
        {
            return GridType.road;
        }
        else if(tex == 2||tex ==11||tex == 15)
        {
            return GridType.occupy;
        }
        else
        {
            return GridType.empty;
        }
    }

    public static float GetPassSpeed(int tex)
    {
        if (tex == 0 || tex == 19 || tex == 18 || tex == 16 || tex == 17)
        {
            return 0.7f;
        }
        else if (tex == 1 || tex == 2 || tex == 3 || tex == 11)
        {
            return 0.5f;
        }
        else if (tex == 12 || tex == 13 || tex == 14)
        {
            return 1f;
        }
        else if (tex == 9 || tex == 10 || tex == 8)
        {
            return 1.2f;
        }
        else if (tex == 4 || tex == 5 || tex == 6)
        {
            return 1.5f;
        }
        return 0.1f;
    }

    public static int GetEnterCost(int tex)
    {
        if (tex == 0 || tex == 19 || tex == 18 || tex == 16 || tex == 17)
        {
            return 7;
        }
        else if (tex == 1 || tex == 2 || tex == 3 || tex == 11)
        {
            return 10;
        }
        else if (tex == 12 || tex == 13 || tex == 14)
        {
            return 3;
        }
        else if (tex == 9 || tex == 10 || tex == 8)
        {
            return 2;
        }
        else if (tex == 4 || tex == 5 || tex == 6)
        {
            return 1;
        }
        return 100;
    }

    public static float GetMineRichness(Vector2Int vector2Int)
    {
        return Instance.mapData.mine[vector2Int.x][vector2Int.y];
    }

    public void InitRoad()
    {

        List<StaticBuilding> lists = StaticBuilding.lists;
        //RoadManager.Instance.InitRoadManager();
        SetGrid(lists);
    }

    void SetGrid(List<StaticBuilding> lists)
    {
        for (int i = 0; i < lists.Count; i++)
        {
            //Debug.Log("??");
            lists[i].SetGrids();
        }
        Debug.Log("地图已初始化！");
    }

    public List<Vector2Int> GetAllRoadGrid()
    {
        List<Vector2Int> res = new List<Vector2Int>();
        /*foreach(var item in _gridDic)
        {
            if(item.Value.GridType == GridType.road)
            {
                res.Add(item.Key);
            }
        }*/
        return res;
    }

    /// <summary>
    /// 为建筑设置起点
    /// </summary>
    public void SetBuildingsGrid()
    {
        for (int i = 0; i < _buildings.Count; i++)
        {
            BuildingBase currentBuilding = _buildings[i];
            //RoadManager.Instance.AddCrossNode(currentBuilding.parkingGridIn, currentBuilding.direction);
            //RoadManager.Instance.AddCrossNode(currentBuilding.parkingGridOut, currentBuilding.direction);
            //RoadManager.Instance.AddTurnNode(currentBuilding.parkingGridIn, currentBuilding.parkingGridOut);
        }
    }
    /// <summary>
    /// 刷地基
    /// </summary>
    public void BuildFoundation(Vector2Int[] takenGirds, int tex, int dir = 0)
    {
        for (int i = 0; i < takenGirds.Length; i++)
        {
            generator.RefreshUV(tex, 8, takenGirds[i].x + takenGirds[i].y * MapSize.x, dir);
            SetPassInfo(takenGirds[i], tex);
        }
        generator.ReCalculateNormal();
    }

    public void BuildOriginFoundation(Vector2Int[] takenGirds)
    {
        for (int i = 0; i < takenGirds.Length; i++)
        {
            int index = takenGirds[i].x + takenGirds[i].y * MapSize.x;
            int tex = GameManager.saveData.meshTex[index];
            //int dir = GameManager.saveData.meshDir[index];
            generator.RefreshUV(tex, 8, index,4);
            SetPassInfo(takenGirds[i], tex);
        }
        generator.ReCalculateNormal();
    }
    public void BuildOutCornerRoad(int level, Vector2Int roadGrid, Direction direction)
    {
        int index = roadGrid.x + roadGrid.y * MapSize.x;
        generator.RefreshUV(12 - level * 4 + 2, 8, index, (int)direction);
        SetPassInfo(roadGrid, 12 - level * 4 + 2);
    }
    public void BuildInCornerRoad(int level, Vector2Int roadGrid, Direction direction)
    {
        int index = roadGrid.x + roadGrid.y * MapSize.x;
        generator.RefreshUV(12 - level * 4 + 1, 8, index, (int)direction);
        SetPassInfo(roadGrid, 12 - level * 4 + 2);
    }
    public void BuildStraightRoad(int level, Vector2Int roadGrid, Direction direction)
    {
        int index = roadGrid.x + roadGrid.y * MapSize.x;
        generator.RefreshUV(12 - level * 4, 8, index, (int)direction);
        SetPassInfo(roadGrid, 12 - level * 4 + 2);
    }
    public void GenerateRoad(Vector2Int[] roadGrid, int level = 1)
    {
        for (int i = 0; i < roadGrid.Length; i++)
        {
            SetGridTypeToRoad(roadGrid[i]);
        }
        for (int i = 0; i < roadGrid.Length; i++)
        {
            RoadOption roadOption;
            Direction direction;
            GetRoadTypeAndDir(roadGrid[i], out roadOption, out direction);
            SetGridType(roadGrid[i], GridType.road);
            switch (roadOption)
            {
                case RoadOption.straight:
                    BuildStraightRoad(level - 1, roadGrid[i], direction);
                    break;
                case RoadOption.inner:
                    BuildInCornerRoad(level - 1, roadGrid[i], direction);
                    break;
                case RoadOption.outter:
                    BuildOutCornerRoad(level - 1, roadGrid[i], direction);
                    break;
            }
        }
        //Debug.Log("Build");
        generator.ReCalculateNormal();
        //RoadManager.Instance.InitRoadNodeDic();
    }

    public void GetRoadTypeAndDir(Vector2Int roadGrid, out RoadOption roadOption, out Direction direction)
    {
        roadOption = RoadOption.straight;
        direction = Direction.right;
        bool[] around = new bool[8];
        around[0] = GridType.road == GetGridType(roadGrid + new Vector2Int(-1, -1));
        around[1] = GridType.road == GetGridType(roadGrid + new Vector2Int(1, -1));
        around[2] = GridType.road == GetGridType(roadGrid + new Vector2Int(1, 1));
        around[3] = GridType.road == GetGridType(roadGrid + new Vector2Int(-1, 1));
        around[4] = GridType.road == GetGridType(roadGrid + new Vector2Int(0, -1));
        around[5] = GridType.road == GetGridType(roadGrid + new Vector2Int(1, 0));
        around[6] = GridType.road == GetGridType(roadGrid + new Vector2Int(0, 1));
        around[7] = GridType.road == GetGridType(roadGrid + new Vector2Int(-1, 0));
        int count = 0;
        for (int i = 0; i < around.Length; i++)
        {
            if (around[i]) count++;
        }
        //Debug.Log(count);
        if (count == 7)
        {
            roadOption = RoadOption.inner;
            for (int i = 0; i < 4; i++)
            {
                if (around[i] && !around[(i + 1) % 4])
                {
                    direction = (Direction)System.Enum.ToObject(typeof(Direction), (i) % 4);
                    return;
                }
            }
        }
        else
        if (count > 3)
        {
            roadOption = RoadOption.straight;
            for (int i = 0; i < 4; i++)
            {
                if (around[i] && around[(i + 1) % 4] && count != 6)
                {
                    direction = (Direction)System.Enum.ToObject(typeof(Direction), (i + 1) % 4);
                    return;
                }
                if (!around[(i + 4)] && count == 6)
                {
                    direction = (Direction)System.Enum.ToObject(typeof(Direction), (i + 3) % 4);
                    //Debug.Log((int)direction);
                    return;
                }
            }
        }
        else
        {
            roadOption = RoadOption.outter;
            for (int i = 0; i < 4; i++)
            {
                if (around[i] && !around[(i + 1) % 4])
                {
                    direction = (Direction)System.Enum.ToObject(typeof(Direction), (i + 1) % 4);
                    return;
                }
            }
        }
    }

    public static GridNode GetGridNode(Vector2Int gridPos)
    {
        //Debug.Log(gridPos);
        return MapManager.Instance._grids[gridPos.x][gridPos.y];
    }

    public static void SetPassInfo(Vector2Int gridPos,int tex)
    {
        GridNode node = GetGridNode(gridPos);
        node.passSpeed = GetPassSpeed(tex);
        node.enterCost = GetEnterCost(tex);

    }

    /// <summary>
    /// 获取地形在世界空间的位置
    /// </summary>
    /// <returns></returns>
    public static Vector3 GetTerrainWorldPosition()
    {
        return TerrainGenerator.Instance.transform.position;
    }
    /// <summary>
    /// 获取地面某处的坐标
    /// </summary>
    /// <returns></returns>
    /// 
    public static Vector3 GetTerrainPosition(Vector2Int gridPos)
    {

        return new Vector3(gridPos.x * 2, GetGridNode(gridPos).height, gridPos.y * 2); 
    }

    public static Vector3 GetNotInWaterPosition(Vector2Int gridPos)
    {
        Vector3 vec = GetTerrainPosition(gridPos);
        if (vec.y < 10)
        {
            return new Vector3(vec.x, 10, vec.z);
        }
        return vec;
    }

    public static Vector3 GetTerrainStaticPosition(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * 2, 10, gridPos.y * 2);
    }
    public List<Vector3> GetTerrainPosition(List<Vector2Int> gridPos)
    {
        List<Vector3> result = new List<Vector3>();
        for (int i = 0; i < gridPos.Count; i++)
        {
            result.Add(GetTerrainPosition(gridPos[i]));
        }
        return result;
    }
    public static Vector3 GetTerrainPosition(Vector3 mistakeHeightWorldPos)
    {
        Vector3 localPos = mistakeHeightWorldPos - GetTerrainWorldPosition();
        Vector2Int gridPos = GetCenterGrid(localPos);
        return GetTerrainPosition(gridPos);
    }
    public static Vector2Int GetCenterGrid(Vector3 centerPos)
    {
        Vector3 centerGrid = centerPos / 2;
        int x = Mathf.FloorToInt(centerGrid.x);
        int z = Mathf.FloorToInt(centerGrid.z);
        return new Vector2Int(x, z);
    }
    public GridType GetGridType(Vector2Int grid)
    {
        //Debug.Log(grid);
        return _grids[grid.x][grid.y].gridType;
    }

    /// <summary>
    /// 道路不能与已建造的建筑重合
    /// </summary>
    /// <param name="vector2Int"></param>
    /// <returns></returns>
    public static bool CheckRoadOverlap(Vector2Int vector2Int)
    {
        GridType type = Instance.GetGridType(vector2Int);
        if (type == GridType.occupy)
        {
            return true;
        }
        return false;
    }
    /*
    public SingleGrid GetSingleGrid(Vector2Int grid)
    {
        SingleGrid result;
        if (_gridDic.TryGetValue(grid, out result))
        {
            return result;
        }
        else
        {
            Debug.LogError("不合法输入" + grid.ToString());
            return null;
        }
    }
    */
    public static bool CheckCanBuild(Vector2Int[] grids, Vector2Int parkingPos, bool checkInSea)
    {
        //检测安放地点占用
        bool hasOverlap = CheckOverlap(grids);
        //检测道路是否贴近
        bool hasNearRoad = CheckNearRoad(parkingPos);
        //检测是否靠近海岸线
        bool isInSea = (!checkInSea || CheckIsInWater(grids));
        //检测是否是贴着桥造的
        bool isInWater = CheckIsInWater(parkingPos);
        if (!hasNearRoad)
        {
            noticeContent = Localization.ToSettingLanguage(ConstString.NoticeBuildFailNoNearRoad);
        }
        else
        if (hasOverlap)
        {
            noticeContent = Localization.ToSettingLanguage(ConstString.NoticeBuildFailNoPlace);
        }
        else
        if (!isInSea)
        {
            noticeContent = Localization.ToSettingLanguage(ConstString.NoticeBuildFailNoNearSea);
        }
        else if (isInWater)
        {
            noticeContent = "不能在水面下建造建筑";
        }
        //if (!isInSea) Debug.Log("不在海里");
        return !hasOverlap && hasNearRoad && isInSea && !isInWater;
    }

    /// <summary>
    /// 检测目标格子对于的地面高度是否有在海平面下的部分
    /// </summary>
    /// <param name="vector2Ints"></param>
    /// <returns></returns>
    public static bool CheckIsInWater(Vector2Int[] vector2Ints)
    {
        return CheckIsInWater(vector2Ints[vector2Ints.Length / 2]) && (CheckIsInWater(vector2Ints[0]) || CheckIsInWater(vector2Ints[vector2Ints.Length - 1]));
    }

    public static bool CheckIsInWater(Vector2Int vector2Int)
    {
        Vector3 groundPos = GetTerrainPosition(vector2Int);
        return groundPos.y - 10 < -0.9f;
    }

    /// <summary>
    /// 获得平地位置
    /// </summary>
    /// <returns></returns>
    public static Vector3 GetOnGroundPosition(Vector2Int gridPos)
    {
        if (gridPos.x > 0 && gridPos.y > 0 && gridPos.x < 300 && gridPos.y < 300)
        {
            int p = gridPos.x * 4 + gridPos.y * 300 * 4;
            Vector3 res = TerrainGenerator.GetTerrainMeshVertices()[p];

            return new Vector3(res.x, 10f, res.z);
        }
        else return Vector3.zero;
    }
    public static bool CheckOverlap(Vector2Int[] grids)
    {
        for (int i = 0; i < grids.Length; i++)
        {
            if (Instance.GetGridType(grids[i]) != GridType.empty)
            {
                //Debug.Log("建筑重叠");
                return true;
            }
        }
        return false;
    }

    public static bool CheckIsRoad(Vector2Int[] grids)
    {
        for (int i = 0; i < grids.Length; i++)
        {
            if (Instance.GetGridType(grids[i]) != GridType.road)
            {
                return false;
            }
        }
        return true;
    }
    public static bool CheckNearRoad(Vector2Int parkingPos)
    {
        return Instance.GetGridType(parkingPos) == GridType.road;
    }
    public static bool CheckNearRoad(Vector2Int[] grids, int width, int height)
    {
        Vector2Int start = grids[0];
        for (int i = 0; i < width; i++)
        {
            if (Instance.GetGridType(start + new Vector2Int(i, -1)) == GridType.road)
            {
                //direction = Direction.down;
                return true;
            }
            if (Instance.GetGridType(start + new Vector2Int(i, height)) == GridType.road)
            {
                //direction = Direction.up;
                return true;
            }
        }
        for (int i = 0; i < height; i++)
        {
            if (Instance.GetGridType(start + new Vector2Int(-1, i)) == GridType.road)
            {
                //direction = Direction.left;
                return true;
            }
            if (Instance.GetGridType(start + new Vector2Int(width, i)) == GridType.road)
            {
                //direction = Direction.right;
                return true;
            }
        }
        //direction = Direction.right;
        //Debug.Log("不贴合道路");
        return false;
    }

    private void SetGridType(Vector2Int grid, GridType gridType)
    {
        if (_grids != null)
        {
            GridNode gridNode = _grids[grid.x][grid.y];
            gridNode.gridType = gridType;
        }
    }

    public void ShowGrid(Vector2Int[] grids)
    {
        for (int i = 0; i < grids.Length; i++)
        {
            //Instantiate(gridPfb, new Vector3(grids[i].x*3, 0.1f, grids[i].y*3), Quaternion.identity, transform);
        }
    }
    public static void SetGridTypeToEmpty(Vector2Int grid)
    {
        Instance.SetGridType(grid, GridType.empty);
    }

    public static void SetGridTypeToEmpty(Vector2Int[] grids)
    {
        for (int i = 0; i < grids.Length; i++)
        {
            Instance.SetGridType(grids[i], GridType.empty);
        }
    }

    public static void SetGridTypeToOccupy(Vector2Int grid)
    {
        Instance.SetGridType(grid, GridType.occupy);
    }

    public static void SetGridTypeToOccupy(Vector2Int[] grids)
    {
        for (int i = 0; i < grids.Length; i++)
        {
            Instance.SetGridType(grids[i], GridType.occupy);
        }
    }

    public static void SetGridTypeToRoad(Vector2Int grid)
    {
        Instance.SetGridType(grid, GridType.road);
    }

    public static GameObject GetNearestMarket(Vector2Int grid)
    {
        float dis = Mathf.Infinity;
        GameObject p = null;
        for (int i = 0; i < Instance._buildings.Count; i++)
        {
            //Debug.Log(Instance._buildings[i].GetComponent<BuildingBase>().runtimeBuildData.Id);
            int id = Instance._buildings[i].GetComponent<BuildingBase>().runtimeBuildData.Id;
            if (id == 20004 || id == 20012 || id == 20013)
            {
                float cur = GetDistance(Instance._buildings[i].parkingGridIn, grid);
                if (cur < dis)
                {
                    dis = cur;
                    p = Instance._buildings[i].gameObject;
                }
            }
        }
        return p;
    }

    public static GameObject GetNearestStorage(Vector2Int grid)
    {
        float dis = Mathf.Infinity;
        GameObject p = null;
        for (int i = 0; i < Instance._buildings.Count; i++)
        {
            if (Instance._buildings[i].GetComponent<BuildingBase>().runtimeBuildData.Id == 20003)
            {
                float cur = GetDistance(Instance._buildings[i].GetComponent<BuildingBase>().parkingGridIn, grid);
                if (cur < dis)
                {
                    dis = cur;
                    p = Instance._buildings[i].gameObject;
                }
            }
        }
        return p;
    }

    public float GetHappiness()
    {
        float sum = 0; ;
        for (int i = 0; i < _buildings.Count; i++)
        {
            sum += _buildings[i].runtimeBuildData.CurLevel;
        }
        return (sum / _buildings.Count) * 10 + 80;
    }

    private static int GetDistance(Vector2Int cur, Vector2Int target)
    {
        return Mathf.Abs(cur.x - target.x) + Mathf.Abs(cur.y - target.y);
    }

    public class compare: IComparer<GridNode>
    {
        public int Compare(GridNode x,GridNode y)
        {
            return x.F - y.F;
        }
    }

    public List<Vector3> GetWayPoints(Vector2Int start, Vector2Int end)
    {
        //Debug.Log(start + " " + end);
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        //ClearRoadNodeH();
        List<GridNode> path = new List<GridNode>();
        List<GridNode> openList = new List<GridNode>();
        List<GridNode> closeList = new List<GridNode>();
        GridNode startNode = GetGridNode(start);
        GridNode endNode = GetGridNode(end);
        openList.Add(startNode);
        path.Add(startNode);
        GridNode temp = startNode;
        temp.G = 0;
        temp.F = 0;
        int n = 600;
        while (temp != endNode && n-- > 0)
        {
            if (openList.Count <= 0)
            {
                Debug.LogError("路径被阻挡！"+ start+"=>"+end);
                return null;
            }
            //Debug.Log("——");
            temp = openList[0];
            /*while (openList.Count > 10)
            {
                openList.RemoveRange(10, openList.Count - 10);
            }*/
            openList.Remove(temp);
            closeList.Add(temp);
            //Debug.Log("选择:"+temp.GridPos);
            if (temp == endNode)
            {
                //sw.Stop();
                //System.TimeSpan dt = sw.Elapsed;
                //Debug.Log("寻路耗时:" + dt.TotalSeconds + "秒");
                //sw.Restart();
                List<Vector3> list = new List<Vector3>();
                while (temp != startNode)
                {
                    //Debug.Log(MapManager.Instance.GetTerrainPosition(temp.GridPos));
                    list.Add(MapManager.GetNotInWaterPosition(temp.GridPos) + new Vector3(1, 0, 1));
                    temp = temp.Parent;
                    //Instantiate(pfb, MapManager.Instance.GetTerrainPosition(temp.GridPos), Quaternion.identity, transform);
                }
                //Debug.Log(MapManager.Instance.GetTerrainPosition(startNode.GridPos));
                list.Add(MapManager.GetNotInWaterPosition(startNode.GridPos) + new Vector3(1, 0, 1));
                list.Reverse(); 
                //sw.Stop();
                //System.TimeSpan dt1 = sw.Elapsed;
                //Debug.Log("翻转道路:" + dt1.TotalSeconds + "秒");
                //WayPointDic.Add(start.ToString() + end.ToString(), list);
                return list;
            }
            //Debug.Log("当前结点："+temp.GridPos+ " "+temp.NearbyNode.Count);
            for (int i = 0; i < temp.NearbyNode.Count; i++)
            {
                GridNode node = temp.NearbyNode[i];
                //Debug.Log(temp.GridPos + "=>" + node.GridPos);
                if (closeList.Contains(node)) continue;
                if (openList.Contains(node))
                {
                    int g = node.G;
                    if(g > temp.G + node.enterCost)
                    {
                        node.G = temp.G + node.enterCost;
                        node.F = node.G + 3 * GetDistance(node.GridPos, end);
                        node.Parent = temp;
                    }
                }
                else
                {
                    node.G = temp.G + node.enterCost;
                    node.F = node.G + 3 * GetDistance(node.GridPos, end);
                    node.Parent = temp;
                    openList.Add(node);
                }
            }
            openList.Sort(new compare());
        }
        Debug.LogError("寻路失败"+start+" "+end);
        return null;
    }
}

/*
[System.Serializable]
public class SingleGrid
{
    public Vector2IntSerializer GridPos { get; private set; }
    public GridType GridType { get; set; }

    public int TexIndex;

    public int Dir;

    public int enterCost;//经过的代价
    public SingleGrid(Vector2Int pos, GridType gridType)
    {
        this.GridPos = new Vector2IntSerializer(pos.x, pos.y);
        GridType = gridType;
    }

}*/
