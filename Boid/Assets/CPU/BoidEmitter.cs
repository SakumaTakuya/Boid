using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidEmitter : MonoBehaviour
{
	[SerializeField] private Boid _boid;
	[SerializeField] private int _boisNum = 20;
	[SerializeField] private float _spawnSize = 10f;
	
	//壁の大きさ
	[SerializeField] private Vector3 _wallMin = new Vector3(-20, -20, -20);
	[SerializeField] private Vector3 _wallMax = new Vector3( 20,  20,  20);
	
	// Use this for initialization
	private void Start ()
	{
		_boid.WallMin = _wallMin;
		_boid.WallMax = _wallMax;
		for (var i = 0; i < _boisNum; i++)
		{
			var pos = Random.insideUnitSphere * _spawnSize;
			var rot = Random.rotation;
			Instantiate(_boid, pos, rot);
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube((_wallMin + _wallMax) / 2f, _wallMax - _wallMin);
	}
}
