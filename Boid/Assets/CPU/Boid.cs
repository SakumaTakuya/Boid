using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;

public class Boid : MonoBehaviour
{
	public Vector3 Acceleration { get; private set; }
	public Vector3 Velocity { get; private set; }
	
	//シミュレーション範囲
	public Vector3 WallMin;
	public Vector3 WallMax;
	
	//Boidのレイヤ
	[SerializeField] private int _boidLayer = 1;

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
	//回転量
	[SerializeField] private float _rotationRate = 10;

	private void Update()
	{		
		var separateCols = Physics.OverlapSphere(transform.position, _separate.Radius, _boidLayer);
		var alignmentCols = Physics.OverlapSphere(transform.position, _alignment.Radius, _boidLayer);
		var cohesionCols = Physics.OverlapSphere(transform.position, _cohesion.Radius, _boidLayer);

		var sepSum = Vector3.zero;
		var aliSum = Vector3.zero;
		var cohSum = Vector3.zero;
		
		if (separateCols.Length > 0)
		{
			foreach (var s in separateCols)
			{
				sepSum += transform.position - s.transform.position;
			}
			sepSum /= separateCols.Length;
		}
		
		if (alignmentCols.Length > 0)
		{			
			foreach (var a in alignmentCols)
			{
				aliSum += a.transform.forward;
			}
			aliSum /= alignmentCols.Length;
		}

		
		if (cohesionCols.Length > 0)
		{
			foreach (var c in cohesionCols)
			{
				cohSum += c.transform.position - transform.position;
			}
			cohSum /= cohesionCols.Length;
		}

		var force = sepSum * _separate.Weight  + 
		            aliSum * _alignment.Weight + 
		            cohSum * _cohesion.Weight;

		if 
		(
			transform.position.x > WallMax.x || transform.position.x < WallMin.x ||
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
}