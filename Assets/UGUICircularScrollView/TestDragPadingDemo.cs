using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Item
{
	public string name;
}

/// <summary>
/// 滚动请求下一页Demo
/// </summary>
public class TestDragPadingDemo : MonoBehaviour
{
	public DragPagingCircularScrollView m_DragPading;
	List<Item> mItemList = new List<Item>();

	private int totalCount = 0;
	// Use this for initialization
	void Start ()
	{
		InitTestData();
		m_DragPading.Init(NormalCallBack);
		m_DragPading.ShowList(mItemList.Count);
	}
	
	private void NormalCallBack(GameObject cell, int index)
	{
		cell.transform.Find("Text1").GetComponent<Text>().text = mItemList[index-1].name;
		m_DragPading.RequestMoreData(mItemList.Count,cell, ItemCornerEnum.LEFTTOP ,RequestMoreData);
		// cell.transform.Find("Text2").GetComponent<Text>().text = index.ToString();
	}

	/// <summary>
	/// 请求更多数据
	/// </summary>
	/// <param name="go"></param>
	private void RequestMoreData(GameObject go)
	{
		InitTestData();
		m_DragPading.ShowItem(mItemList.Count);
	}

	private void InitTestData()
	{
		int count = totalCount;
		totalCount += 10;
		for (int i = count; i < totalCount; i++)
		{
			Item item = new Item();
			item.name = "哈哈test" +i;
			mItemList.Add(item);
		}
	}

	public void DeleteItem(int index)
	{
		mItemList.Remove(mItemList[index]);
		m_DragPading.ShowItem(mItemList.Count);
	}
}
