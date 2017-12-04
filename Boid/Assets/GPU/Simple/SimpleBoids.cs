using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimpleBoids : MonoBehaviour
{
	//スレッドグループのスレッドサイズ
	private const int BLOCK_SIZE = 256;

	//Boidの数
    public int BoidsNum = 256;

	//Boidを召喚する空間の大きさ
	[SerializeField] private float _spawnSize = 10f;
	
	//質量
	[SerializeField] private float _mass = 1f;
	//分離
	[SerializeField] private BoidSetting _separate = new BoidSetting(1f, 3f);
	//整列
	[SerializeField] private BoidSetting _alignment = new BoidSetting(2f, 1f);
	//結合
	[SerializeField] private BoidSetting _cohesion = new BoidSetting(2f, 1f);
	//壁を避ける力
	[SerializeField] private float _avoidingPower = 10f;
	
	//速度の最大値
	[SerializeField] private float _maxSpeed = 5f;
	//操舵力の最大値
	[SerializeField] private float _maxSteerForce = 0.5f;
	
	//壁の大きさ
	[SerializeField] private Vector3 _wallMin = new Vector3(-20, -20, -20);
	[SerializeField] private Vector3 _wallMax = new Vector3( 20,  20,  20);
	
	//シミュレーションを行うComputeShader
	[SerializeField] private ComputeShader _boidComputeShader;
	
	//BoidのBuffer
	public ComputeBuffer BoidBuffer { get; private set; }
	
	//シミュレーション空間の中心
	public Vector3 WallCenter
	{
		get { return (_wallMin + _wallMax) / 2f; }
	}

	//シミュレーション空間の大きさ
	public Vector3 WallSize
	{
		get { return _wallMax - _wallMin; }
	}
	
	// Use this for initialization
	private void Start () {
		Initialize();
	}
	
	// Update is called once per frame
	private void Update () {
		Simurate();
	}

	private void OnDestroy()
	{
		if (BoidBuffer == null) return;
		BoidBuffer.Release();
		BoidBuffer = null;
	}

	private void Initialize()
	{
		//Bufferの初期化
		BoidBuffer = new ComputeBuffer(BoidsNum, Marshal.SizeOf(typeof(Boid)));

		var boidArray = new Boid[BoidsNum];
		for (var i = 0; i < BoidsNum; i++)
		{
			boidArray[i].Postion = Random.insideUnitSphere * _spawnSize;
			boidArray[i].Velocity = Random.insideUnitSphere * 0.1f;
		}
		
		BoidBuffer.SetData(boidArray);
		
		//ComputeShaderに値を渡す
		_boidComputeShader.SetInt("_BoidsNum", BoidsNum);
		_boidComputeShader.SetFloat("_Mass", _mass);
		_boidComputeShader.SetFloat("_SeparateRadius", _separate.Radius);
		_boidComputeShader.SetFloat("_SeparateWeight",_separate.Weight);
		_boidComputeShader.SetFloat("_AlignmentRadius", _alignment.Radius);
		_boidComputeShader.SetFloat("_AlignmentWeight", _alignment.Weight);
		_boidComputeShader.SetFloat("_CohesionRadius", _cohesion.Radius);
		_boidComputeShader.SetFloat("_CohesionWeight", _cohesion.Weight);
		_boidComputeShader.SetFloat("_AvoidingPower", _avoidingPower);
		_boidComputeShader.SetFloat("_MaxSpeed", _maxSpeed);
		_boidComputeShader.SetFloat("_MaxSteerForce", _maxSteerForce);
		_boidComputeShader.SetVector("_WallMax",_wallMax);
		_boidComputeShader.SetVector("_WallMin", _wallMin);	
	}

	private void Simurate()
	{
		var id = _boidComputeShader.FindKernel("SimulateCS"); //カーネルIDを取得
		var threadGroupSize = Mathf.CeilToInt(BoidsNum / BLOCK_SIZE); //スレッドグループサイズが丁度良くなるようにしないと効率が悪い
		_boidComputeShader.SetFloat("_DeltaTime", Time.deltaTime);
		_boidComputeShader.SetBuffer(id, "_BoidBuffer", BoidBuffer);

		_boidComputeShader.Dispatch(id, threadGroupSize, 1, 1);
		//BoidBuffer.GetData(b);
		//print(b[0].Postion);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(WallCenter, WallSize);
	}
	
	[Serializable]
	private struct Boid
	{
		public Vector3 Velocity;
		public Vector3 Postion;
	}
	
}
