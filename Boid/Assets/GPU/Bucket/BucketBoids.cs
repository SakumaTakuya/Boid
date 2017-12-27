using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

public class BucketBoids : SimpleBoids
{
    [SerializeField] private float _bucketLength = 4f;
    private ComputeBuffer _lastIdBuffer;

    private Vector3 _bucketSize;
    private Vector3 _bucketMin;
    private int _bucketSizeXYZ;

    private int _sortId;
    private int _setLastId;
    private int _threadGroupSize;

    private void Start()
    {
        InitializeValues();
        InitializeBuffers();
    }

    private void Update()
    {
        Sort();
        Simulate();
    }
    
    private void OnDestroy()
    {
        if (BoidBuffer == null) return;
        BoidBuffer.Release();
        BoidBuffer = null;
        
        if (_lastIdBuffer == null) return;
        _lastIdBuffer.Release();
        _lastIdBuffer = null;
    }
    
    protected override void InitializeValues()
    {
        base.InitializeValues();
        _bucketSize = (WallSize + Vector3.one * _bucketLength * 2) / _bucketLength;//Wallよりバケット1つ分広めにとる
        _bucketSizeXYZ = (int) (_bucketSize.x * _bucketSize.y * _bucketSize.z);
        _bucketMin = WallMin - Vector3.one * _bucketLength;
        
        BoidComputeShader.SetFloat("_BucketLength", _bucketLength);
        BoidComputeShader.SetInt("_BucketSizeX", (int)_bucketSize.x);
        BoidComputeShader.SetInt("_BucketSizeXY", (int)(_bucketSize.x * _bucketSize.y));
        BoidComputeShader.SetVector("_BucketMin", _bucketMin);
        
        _sortId = BoidComputeShader.FindKernel("SortCS");
        _setLastId = BoidComputeShader.FindKernel("SetLastCS");
        
        _threadGroupSize = Mathf.CeilToInt(BoidsNum / BLOCK_SIZE); //スレッドグループサイズが丁度良くなるようにしないと効率が悪い
    }

    protected override void InitializeBuffers()
    { 
        //Bufferの初期化(SetLastCS用にダミーデータが一つ必要)
        BoidBuffer = new ComputeBuffer(BoidsNum + 1, Marshal.SizeOf(typeof(BoidData)));
        _lastIdBuffer = new ComputeBuffer(_bucketSizeXYZ, Marshal.SizeOf(typeof(int)));

        var boidArray = new BoidData[BoidsNum + 1];
        
        for (var i = 0; i < BoidsNum; i++)
        {
            boidArray[i].Postion = Random.insideUnitSphere * SpawnSize;
            boidArray[i].Velocity = Random.insideUnitSphere * 1f;
            boidArray[i].Id = (int) ((boidArray[i].Postion.x - _bucketMin.x) / _bucketLength) +
                              (int) ((boidArray[i].Postion.y - _bucketMin.y) / _bucketLength * _bucketSize.x ) +
                              (int) ((boidArray[i].Postion.z - _bucketMin.z) / _bucketLength * _bucketSize.x * _bucketSize.y);
        }
		
        //ダミーデータの作成
        boidArray[BoidsNum].Postion = Vector3.positiveInfinity;
        boidArray[BoidsNum].Id = int.MaxValue;
        
        BoidBuffer.SetData(boidArray); 
              
        //バッファの設定
        BoidComputeShader.SetBuffer(KernelId, "_BoidBuffer", BoidBuffer);
        BoidComputeShader.SetBuffer(KernelId, "_LastIDBuffer", _lastIdBuffer);
        BoidComputeShader.SetBuffer(_sortId, "_BoidBuffer", BoidBuffer);
        BoidComputeShader.SetBuffer(_setLastId, "_BoidBuffer", BoidBuffer);
        BoidComputeShader.SetBuffer(_setLastId, "_LastIDBuffer", _lastIdBuffer);
    }

    protected void Sort()
    {
        //ソート
        for (var len = 2; len <= BoidsNum; len *= 2)//len==m
        {
            BoidComputeShader.SetInt("_Length", len);
            for(var tar = len / 2; tar > 0; tar /= 2)//tar==l
            {
                BoidComputeShader.SetInt("_Target", tar);
                BoidComputeShader.Dispatch(_sortId, _threadGroupSize, 1, 1);
            }
        }        
        
        //バケットの最後の要素を取り出す
        _lastIdBuffer.SetData(new int[_bucketSizeXYZ]);
        BoidComputeShader.Dispatch(_setLastId, _threadGroupSize, 1, 1);        
    }
    
    protected new struct BoidData
    {
       	public Vector3 Velocity;
      	public Vector3 Postion;
        public int Id;
    }
}
