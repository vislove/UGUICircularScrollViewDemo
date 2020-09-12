/****************************************************
 文件：CellClick.cs
 作者：Sor
 邮箱: soooooor@163.com
 日期：#CreateTime#
 功能：Nothing
*****************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CellClick : MonoBehaviour
{
	public TestDragPadingDemo demo;
	private Button btn;
	// Use this for initialization
	void Start ()
	{
		btn = GetComponent<Button>();
		btn.onClick.AddListener(OnClickBtn);
	}

	private void OnClickBtn()
	{
		Debug.LogError("点击了：" +gameObject.name);
		demo.DeleteItem(int.Parse(gameObject.name));
	}

	// Update is called once per frame
	void Update () 
	{
		
	}
}
