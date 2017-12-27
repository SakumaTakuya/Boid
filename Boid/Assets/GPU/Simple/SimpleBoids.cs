using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimpleBoids : MonoBehaviour
{
	//スレッドグループのスレッドサイズ
	protected const int BLOCK_SIZE = 256;

	//Boidの数
    public int BoidsNum = 256;

	//Boidを召喚する空間の大きさ
	[SerializeField] protected float SpawnSize = 10f;
	
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
	[SerializeField] protected Vector3 WallMin = new Vector3(-20, -20, -20);
	[SerializeField] protected Vector3 WallMax = new Vector3( 20,  20,  20);
	
	//シミュレーションを行うComputeShader
	[SerializeField] protected ComputeShader BoidComputeShader;
	
	//BoidのBuffer
	public ComputeBuffer BoidBuffer { get; protected set; }

	protected int KernelId;
	
	//シミュレーション空間の中心
	public Vector3 WallCenter
	{
		get { return (WallMin + WallMax) / 2f; }
	}

	//シミュレーション空間の大きさ
	public Vector3 WallSize
	{
		get { return WallMax - WallMin; }
	}
	
	// Use this for initialization
	private void Start () {
		InitializeValues();
		InitializeBuffers();
	}
	
	// Update is called once per frame
	private void Update () {
		Simulate();
	}

	private void OnDestroy()
	{
		if (BoidBuffer == null) return;
		BoidBuffer.Release();
		BoidBuffer = null;
	}

	protected virtual void InitializeValues()
	{
		//ComputeShaderに値を渡す
		BoidComputeShader.SetInt("_BoidsNum", BoidsNum);
		BoidComputeShader.SetFloat("_Mass", _mass);
		BoidComputeShader.SetFloat("_SeparateRadius", _separate.Radius);
		BoidComputeShader.SetFloat("_SeparateWeight",_separate.Weight);
		BoidComputeShader.SetFloat("_AlignmentRadius", _alignment.Radius);
		BoidComputeShader.SetFloat("_AlignmentWeight", _alignment.Weight);
		BoidComputeShader.SetFloat("_CohesionRadius", _cohesion.Radius);
		BoidComputeShader.SetFloat("_CohesionWeight", _cohesion.Weight);
		BoidComputeShader.SetFloat("_AvoidingPower", _avoidingPower);
		BoidComputeShader.SetFloat("_MaxSpeed", _maxSpeed);
		BoidComputeShader.SetFloat("_MaxSteerForce", _maxSteerForce);
		BoidComputeShader.SetVector("_WallMax",WallMax);
		BoidComputeShader.SetVector("_WallMin", WallMin);
		
		KernelId = BoidComputeShader.FindKernel("SimulateCS"); //カーネルIDを取得
	}

	protected virtual void InitializeBuffers()
	{
		//Bufferの初期化
		BoidBuffer = new ComputeBuffer(BoidsNum, Marshal.SizeOf(typeof(BoidData)));

		var boidArray = new BoidData[BoidsNum];
		for (var i = 0; i < BoidsNum; i++)
		{
			boidArray[i].Postion = Random.insideUnitSphere * SpawnSize;
			boidArray[i].Velocity = Random.insideUnitSphere * 0.1f;
		}
		
		BoidBuffer.SetData(boidArray);
		BoidComputeShader.SetBuffer(KernelId, "_BoidBuffer", BoidBuffer);
	}

	protected void Simulate()
	{
		var threadGroupSize = Mathf.CeilToInt(BoidsNum / BLOCK_SIZE); //スレッドグループサイズが丁度良くなるようにしないと効率が悪い
		BoidComputeShader.SetFloat("_DeltaTime", Time.deltaTime);
		BoidComputeShader.Dispatch(KernelId, threadGroupSize, 1, 1);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(WallCenter, WallSize);
	}
	
	protected struct BoidData
	{
		public Vector3 Velocity;
		public Vector3 Postion;
	}
	
}
