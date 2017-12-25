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

    private int _sortId;
    private int _setLastId;
    private int _threadGroupSize;

    private void Start()
    {
        Initialize();
        InitBoid();
    }

    private void Update()
    {
        Simulate();
        Sort();
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
    
    protected override void Initialize()
    {
        base.Initialize();
        _bucketSize = WallSize / _bucketLength;
        BoidComputeShader.SetFloat("_BucketLength", _bucketLength);
        BoidComputeShader.SetInts("_BucketSizeX", (int)_bucketSize.x);
        BoidComputeShader.SetInts("_BucketSizeXY", (int)(_bucketSize.x * _bucketSize.y));
    }

    protected override void InitBoid()
    {
        _sortId = BoidComputeShader.FindKernel("SortCS");
        _setLastId = BoidComputeShader.FindKernel("SetLastCS");
        
        //Bufferの初期化
        BoidBuffer = new ComputeBuffer(BoidsNum + 1, Marshal.SizeOf(typeof(BoidData)));
        _lastIdBuffer = new ComputeBuffer(
            (int)(_bucketSize.x * _bucketSize.y * _bucketSize.z), 
            Marshal.SizeOf(typeof(int))
        );

        var boidArray = new BoidData[BoidsNum + 1];
        var idArray = new int[BoidsNum];
        
        for (var i = 0; i < BoidsNum; i++)
        {
            boidArray[i].Postion = Random.insideUnitSphere * SpawnSize;
            boidArray[i].Velocity = Random.insideUnitSphere * 1f;
            boidArray[i].Id = (int) ((boidArray[i].Postion.x - WallMin.x) / _bucketLength) +
                              (int) ((boidArray[i].Postion.y - WallMin.y) / _bucketLength * _bucketSize.x ) +
                              (int) ((boidArray[i].Postion.z - WallMin.z) / _bucketLength * _bucketSize.x * _bucketSize.y);
        }
		
        //ダミーデータの作成
        boidArray[BoidsNum].Postion = Vector3.positiveInfinity;
        boidArray[BoidsNum].Id = int.MaxValue;
        
        BoidBuffer.SetData(boidArray); 
        _lastIdBuffer.SetData(idArray);
        
        BoidComputeShader.SetBuffer(KernelId, "_BoidBuffer", BoidBuffer);
        BoidComputeShader.SetBuffer(KernelId, "_LastIDBuffer", _lastIdBuffer);
        BoidComputeShader.SetBuffer(_sortId, "_BoidBuffer", BoidBuffer);
        BoidComputeShader.SetBuffer(_setLastId, "_BoidBuffer", BoidBuffer);
        BoidComputeShader.SetBuffer(_setLastId, "_LastIDBuffer", _lastIdBuffer);

         
        var arr = new BoidData[BoidsNum + 1];
        BoidBuffer.GetData(arr);
        var mo = "";
        for (var i = 0; i < BoidsNum + 1; i++)
        {
            mo += "," + arr[i].Id;
        }
        print("srt:" + mo);
        
        _threadGroupSize = Mathf.CeilToInt(BoidsNum / BLOCK_SIZE); //スレッドグループサイズが丁度良くなるようにしないと効率が悪い
        Sort();
    }

    protected void Sort()
    {
        var arr = new BoidData[BoidsNum];
        var idarr = new int[BoidsNum];
        BoidBuffer.GetData(arr);
        _lastIdBuffer.GetData(idarr);
        var mo = "";
       /* for (var i = 0; i < BoidsNum; i++)
        {
            mo += "," + arr[i].Id + "(" + idarr[i] + ")";
        }
        print("pre:" + mo);*/
        var cnt = 0;
        for (var len = 2; len <= BoidsNum; len *= 2)
        {
            BoidComputeShader.SetInt("_Length", len);
            for(var tar = len / 2; tar > 0; tar /= 2)
            {
                BoidComputeShader.SetInt("_Target", tar);
                BoidComputeShader.Dispatch(_sortId, _threadGroupSize, 1, 1);
                cnt++;
            }
        }        

        BoidComputeShader.Dispatch(_setLastId, _threadGroupSize, 1, 1);
        
        BoidBuffer.GetData(arr);
        _lastIdBuffer.GetData(idarr);
        mo = "";
        for (var i = 0; i < BoidsNum; i++)
        {
            mo += "," + arr[i].Id + "(" + idarr[i] + ")";
        }
        print("res:" + mo + KernelId + "/" + _sortId + "/" + _setLastId + "_" + cnt);
    }
    
    protected new struct BoidData
    {
       	public Vector3 Velocity;
      	public Vector3 Postion;
        public int Id;
    }
}
