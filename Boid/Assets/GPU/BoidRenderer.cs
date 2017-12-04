using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidRenderer : MonoBehaviour
{

	//Boidの大きさ
	[SerializeField] private Vector3 _scale = Vector3.one;
	
	[SerializeField] private SimpleBoids _boids;

	//Graphics.DrawMeshInstancedIndirect のための準備
	[SerializeField] private Mesh _instanceMesh;
	[SerializeField] private Material _instanceMaterial;
	private ComputeBuffer _argsBuffer;
	private readonly uint[] _args = new uint[5] {0, 0, 0, 0, 0};
	
	// Use this for initialization
	private void Start () 
	{
		if(!SystemInfo.supportsInstancing) gameObject.SetActive(false);
		
		_argsBuffer = new ComputeBuffer(
			1, 
			_args.Length * sizeof(uint), 
			ComputeBufferType.IndirectArguments
		);
		_instanceMaterial.SetVector("_Scale", _scale);
	}
	
	// Update is called once per frame
	public void Update () 
	{
		RenderInstancedMesh();
	}

	private void OnDestroy()
	{
		if(_argsBuffer == null) return;
		_argsBuffer.Release();
		_argsBuffer = null;
	}

	private void RenderInstancedMesh()
	{
		_instanceMaterial.SetBuffer("_BoidBuffer", _boids.BoidBuffer);
		var numIndices = _instanceMesh ? _instanceMesh.GetIndexCount(0) : 0;
		_args[0] = numIndices;
		_args[1] = (uint) _boids.BoidsNum;
		_argsBuffer.SetData(_args);
		
		Graphics.DrawMeshInstancedIndirect(
			_instanceMesh,
			0,
			_instanceMaterial,
			new Bounds(_boids.WallCenter, _boids.WallSize), 
			_argsBuffer
		);
	}
}
