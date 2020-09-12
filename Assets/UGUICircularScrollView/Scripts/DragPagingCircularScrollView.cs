using System;
using System.Collections;
using System.Collections.Generic;
using CircularScrollView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = System.Object;

public class DragPagingCircularScrollView : UICircularScrollView
{
    /// <summary>
    /// 加载更多数据拖拽偏移量，数值越大需要拖拽的距离越远
    /// </summary>
    public float mLoadMoreDragOffset = 10;
    /// <summary>
    /// 记录Item在世界空间中四个角的位置信息
    /// </summary>
    private Vector3[] mItemWorldCorners = new Vector3[4];
    /// <summary>
    /// 是否更新Content的位置，默认第一次需要更新，之后分页请求数据/删除数据时不需要更新位置
    /// </summary>
    private bool mIsUpdateContentPosition;
    /// <summary>
    /// 请求队列
    /// </summary>
    private Queue<int> mRequestQueue = new Queue<int>();
    /// <summary>
    /// 请求队列是否闲置
    /// </summary>
    private bool mRequestQueueIsFree = true;
    private RectTransform mViewPortRectTransform;
    private ScrollRect mScrollRect;
    
    # region 重写基类方法
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="callBack"></param>
    public override void Init(Action<GameObject, int> callBack)
    {
        base.Init(callBack);
        mIsUpdateContentPosition = true;
        mScrollRect = gameObject.GetComponent<ScrollRect>();
        if (mScrollRect != null)
        {
            mViewPortRectTransform = mScrollRect.viewport;
        }
    }
    
    /// <summary>
    /// 只重写了一个地方，请求更多数据，或者删除数据时不刷新ScrollView的位置
    /// </summary>
    /// <param name="num"></param>
    public override void ShowList(int num)
    {
        mRequestQueueIsFree = false;
        m_MinIndex = -1;
        m_MaxIndex = -1;
        //-> 计算 Content 尺寸
        if (m_Direction == e_Direction.Vertical)
        {
            float contentSize = (m_Spacing + m_CellObjectHeight) * Mathf.CeilToInt((float) num / m_Row);
            m_ContentHeight = contentSize;
            m_ContentWidth = m_ContentRectTrans.sizeDelta.x;
            contentSize = contentSize < rectTrans.rect.height ? rectTrans.rect.height : contentSize;
            m_ContentRectTrans.sizeDelta = new Vector2(m_ContentWidth, contentSize);
            if (mIsUpdateContentPosition)
            {
                m_ContentRectTrans.anchoredPosition = new Vector2(m_ContentRectTrans.anchoredPosition.x, 0);
            }
        }
        else
        {
            float contentSize = (m_Spacing + m_CellObjectWidth) * Mathf.CeilToInt((float) num / m_Row);
            m_ContentWidth = contentSize;
            m_ContentHeight = m_ContentRectTrans.sizeDelta.x;
            contentSize = contentSize < rectTrans.rect.width ? rectTrans.rect.width : contentSize;
            m_ContentRectTrans.sizeDelta = new Vector2(contentSize, m_ContentHeight);
            if (mIsUpdateContentPosition)
            {
                m_ContentRectTrans.anchoredPosition = new Vector2(0, m_ContentRectTrans.anchoredPosition.y);
            }
        }

        //-> 计算 开始索引
        int lastEndIndex = 0;

        //-> 过多的物体 扔到对象池 ( 首次调 ShowList函数时 则无效 )
        if (m_IsInited)
        {
            lastEndIndex = num - m_MaxCount > 0 ? m_MaxCount : num;
            lastEndIndex = m_IsClearList ? 0 : lastEndIndex;

            int count = m_IsClearList ? m_CellInfos.Length : m_MaxCount;
            for (int i = lastEndIndex; i < count; i++)
            {
                if (m_CellInfos[i].obj != null)
                {
                    SetPoolsObj(m_CellInfos[i].obj);
                    m_CellInfos[i].obj = null;
                }
            }
        }

        //-> 以下四行代码 在for循环所用
        CellInfo[] tempCellInfos = m_CellInfos;
        m_CellInfos = new CellInfo[num];

        //-> 1: 计算 每个Cell坐标并存储 2: 显示范围内的 Cell
        for (int i = 0; i < num; i++)
        {
            // * -> 存储 已有的数据 ( 首次调 ShowList函数时 则无效 )
            if (m_MaxCount != -1 && i < lastEndIndex)
            {
                CellInfo tempCellInfo = tempCellInfos[i];
                //-> 计算是否超出范围
                float rPos = m_Direction == e_Direction.Vertical ? tempCellInfo.pos.y : tempCellInfo.pos.x;
                if (!IsOutRange(rPos))
                {
                    //-> 记录显示范围中的 首位index 和 末尾index
                    m_MinIndex = m_MinIndex == -1 ? i : m_MinIndex; //首位index
                    m_MaxIndex = i; // 末尾index

                    if (tempCellInfo.obj == null)
                    {
                        tempCellInfo.obj = GetPoolsObj();
                    }

                    tempCellInfo.obj.transform.GetComponent<RectTransform>().anchoredPosition = tempCellInfo.pos;
                    tempCellInfo.obj.name = i.ToString();
                    tempCellInfo.obj.SetActive(true);

                    Func(m_FuncCallBackFunc, tempCellInfo.obj);
                }
                else
                {
                    SetPoolsObj(tempCellInfo.obj);
                    tempCellInfo.obj = null;
                }

                m_CellInfos[i] = tempCellInfo;
                continue;
            }

            CellInfo cellInfo = new CellInfo();

            float pos = 0; //坐标( isVertical ? 记录Y : 记录X )
            float rowPos = 0; //计算每排里面的cell 坐标

            // * -> 计算每个Cell坐标
            if (m_Direction == e_Direction.Vertical)
            {
                pos = m_CellObjectHeight * Mathf.FloorToInt(i / m_Row) + m_Spacing * Mathf.FloorToInt(i / m_Row);
                rowPos = m_CellObjectWidth * (i % m_Row) + m_Spacing * (i % m_Row);
                cellInfo.pos = new Vector3(rowPos, -pos, 0);
            }
            else
            {
                pos = m_CellObjectWidth * Mathf.FloorToInt(i / m_Row) + m_Spacing * Mathf.FloorToInt(i / m_Row);
                rowPos = m_CellObjectHeight * (i % m_Row) + m_Spacing * (i % m_Row);
                cellInfo.pos = new Vector3(pos, -rowPos, 0);
            }

            //-> 计算是否超出范围
            float cellPos = m_Direction == e_Direction.Vertical ? cellInfo.pos.y : cellInfo.pos.x;
            if (IsOutRange(cellPos))
            {
                cellInfo.obj = null;
                m_CellInfos[i] = cellInfo;
                continue;
            }

            //-> 记录显示范围中的 首位index 和 末尾index
            m_MinIndex = m_MinIndex == -1 ? i : m_MinIndex; //首位index
            m_MaxIndex = i; // 末尾index

            //-> 取或创建 Cell
            GameObject cell = GetPoolsObj();
            cell.transform.GetComponent<RectTransform>().anchoredPosition = cellInfo.pos;
            cell.gameObject.name = i.ToString();

            //-> 存数据
            cellInfo.obj = cell;
            m_CellInfos[i] = cellInfo;
            //-> 回调  函数
            Func(m_FuncCallBackFunc, cell);
        }

        m_MaxCount = num;
        m_IsInited = true;
        mIsUpdateContentPosition = false;
        OnDragListener(Vector2.zero);
        mRequestQueueIsFree = true;
    }
    # endregion
    
    /// <summary>
    /// 显示Item
    /// </summary>
    public void ShowItem(int num)
    {
        if (mRequestQueue != null)
        {
            mRequestQueue.Enqueue(num);
        }
    }
    
    void Update()
    {
        if (mRequestQueue.Count > 0)
        {
            if(mRequestQueueIsFree) ShowList(mRequestQueue.Dequeue());
        }
    }

    /// <summary>
    /// 请求更多数据
    /// </summary>
    public void RequestMoreData(int mTotalCount, GameObject mCell,
            ItemCornerEnum mCornerEnum,Action<GameObject> mAction)
    {
        if(mCell == null) return;
        int mCurrentIndex = int.Parse(mCell.name) + 1;
        if (mTotalCount != mCurrentIndex) return; // 如果还没有滑倒底部则返回不请求更多数据
        RectTransform mRect = mCell.GetComponent<RectTransform>();
        mRect.GetWorldCorners(mItemWorldCorners);
        float mLocalpace = 0f;
        float mViewPortSize = 0f;
        if (m_Direction == e_Direction.Vertical)
        {
            mLocalpace = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[(int)mCornerEnum]).y;
            mViewPortSize = mViewPortRectTransform.rect.height;
        }
        else
        {
            mLocalpace = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[(int)mCornerEnum]).x;
            mViewPortSize = mViewPortRectTransform.rect.width;
        }
        
        if (mLocalpace + mViewPortSize >= mLoadMoreDragOffset) 
        {
            Debug.Log("========== 准备请求下一页");
            if (mAction != null) mAction(mCell);
        }
    }
}

/// <summary>
/// Item区域枚举
/// </summary>
public enum ItemCornerEnum
{
    LEFTBOTTOM = 0,
    LEFTTOP = 1,
    RIGHTTOP = 2,
    RIGHTBOTTOM = 3,
}