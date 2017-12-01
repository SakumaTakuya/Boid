using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Boid : MonoBehaviour
{
	public Vector3 Acceleration { get; private set; }
	public Vector3 Velocity { get; private set; }
	
	//シミュレーション範囲
	public Vector3 WallMin;
	public Vector3 WallMax;
	
	//Boidのレイヤ
	[SerializeField] private int _boidLayer = 1;

	[SerializeField] private float _mass = 1f;
	//分離
	[SerializeField] private Setting _separate = new Setting(1f, 3f);
	//整列
	[SerializeField] private Setting _alignment = new Setting(2f, 1f);
	//結合
	[SerializeField] private Setting _cohesion = new Setting(2f, 1f);
	//壁を避ける力
	[SerializeField] private float _avoidingPower = 10f;
	
	//速度の最大値
	[SerializeField] private float _maxSpeed = 5f;
	//操舵力の最大値
	[SerializeField] private float _maxSteerForce = 0.5f;
	//回転量
	[SerializeField] private float _rotationRate = 10;

	private void FixedUpdate()
	{
		var separateCols = Physics.OverlapSphere(transform.position, _separate.Radius, _boidLayer);
		var alignmentCols = Physics.OverlapSphere(transform.position, _alignment.Radius, _boidLayer);
		var cohesionCols = Physics.OverlapSphere(transform.position, _cohesion.Radius, _boidLayer);

		var sepPosSum = Vector3.zero;
		if (separateCols.Length > 0)
		{
			foreach (var s in separateCols)
			{
				sepPosSum += transform.position - s.transform.position;
			}
			sepPosSum /= separateCols.Length;
		}

		var aliFSum = Vector3.zero;
		if (alignmentCols.Length > 0)
		{			
			foreach (var a in alignmentCols)
			{
				aliFSum += a.transform.forward;
			}
			aliFSum /= alignmentCols.Length;
		}

		var cohPosSum = Vector3.zero;
		if (cohesionCols.Length > 0)
		{
			foreach (var c in cohesionCols)
			{
				cohPosSum += c.transform.position - transform.position;
			}
			cohPosSum /= cohesionCols.Length;
		}

		var force = sepPosSum * _separate.Weight  + 
		            aliFSum   * _alignment.Weight + 
		            cohPosSum * _cohesion.Weight;

		if (transform.position.x > WallMax.x || transform.position.x < WallMin.x ||
		    transform.position.y > WallMax.y || transform.position.y < WallMin.y ||
		    transform.position.z > WallMax.z || transform.position.z < WallMin.z
		    )
		{
			force -= transform.position.normalized * _avoidingPower;
		}
		
		Acceleration = Vector3.ClampMagnitude(force, _maxSteerForce) / _mass;
		Velocity = Vector3.ClampMagnitude(Acceleration * Time.deltaTime + Velocity, _maxSpeed);
		transform.position += Velocity * Time.deltaTime;
		
		var rot = Quaternion.FromToRotation(transform.forward, Velocity);
		transform.rotation = Quaternion.Slerp(transform.rotation, rot * transform.rotation, Time.deltaTime * _rotationRate);
	}
	
	[Serializable]
	private struct Setting
	{
		public float Radius;
		public float Weight;

		public Setting(float radius, float weight)
		{
			Radius = radius;
			Weight = weight;
		}
	}
}
