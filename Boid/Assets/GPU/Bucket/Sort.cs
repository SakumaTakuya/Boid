using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

public class Sort : MonoBehaviour
{
	[SerializeField] private int _length;
	[SerializeField] private int _range;
	[SerializeField] private ComputeShader _compute;
	private string _mojiretsu;
	private ComputeBuffer _arraybuffer;
	private ComputeBuffer _lastbuffer;
	

	private int _sortId;
	private int _lastId;
	
	// Use this for initialization
	private void Start () 
	{
		_arraybuffer = new ComputeBuffer(_length + 1, Marshal.SizeOf(typeof(int)));
		_lastbuffer = new ComputeBuffer(_range + 1, Marshal.SizeOf(typeof(int)));
		
		_sortId = _compute.FindKernel("Sort2CS");
		_lastId = _compute.FindKernel("SetLastCS");
		
		_compute.SetBuffer(_sortId, "_ArrayBuffer", _arraybuffer);
		_compute.SetBuffer(_lastId, "_ArrayBuffer", _arraybuffer);
		_compute.SetBuffer(_lastId, "_LastBuffer", _lastbuffer);

	}
	
	// Update is called once per frame
	private void Update ()
	{
		var arr = new int[_length + 1];
		for (var i = 0; i < _length; i++)
		{
			arr[i] = Random.Range(0, _range);
		}
		arr[_length] = _range;
		_arraybuffer.SetData(arr);
		
		var pre = new int[_length];
		_mojiretsu = "Pre";
		_arraybuffer.GetData(pre);
		for (var i = 0; i < _length; i++)
		{
			_mojiretsu += "," + pre[i] + "(" + i + ")";
		}
		print(_mojiretsu);

		for (var len = 2; len <= _length; len *= 2)
		{
			_compute.SetInt("_Length", len);
			for(var mlen = len / 2; mlen > 0; mlen /= 2)
			{
				_compute.SetInt("_mLen", mlen);
				_compute.Dispatch(_sortId, _length/4, 1, 1);
			}
		}

		
		var res = new int[_length];
		_mojiretsu = "Res";
		_arraybuffer.GetData(res);
		for (var i = 0; i < _length; i++)
		{
			_mojiretsu += "," + res[i] + "(" + i + ")";
		}
		print(_mojiretsu);
		
		var larr = new int[_range];
		_lastbuffer.SetData(larr);
		_compute.Dispatch(_lastId, _length/4, 1, 1);

		var lst = new int[_range];
		_mojiretsu = "lst";
		_lastbuffer.GetData(lst);
		for (var i = 0; i < _range; i++)
		{
			_mojiretsu += "," + i + "(" + (lst[i] - 1) + ")";
		}
		print(_mojiretsu);
	}
}
