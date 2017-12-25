using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XOR : MonoBehaviour
{

	[SerializeField] private int _length = 16;
	
	
	// Use this for initialization
	void Start () {
		for (var len = 2; len <= _length; len *= 2)
		{
			for(var mlen = len / 2; mlen > 0; mlen /= 2)
			{
				var moji = "(" + len + "," + mlen + ")";
				for (int i = 0; i < _length; i++)
				{
					var xor = i ^ mlen;
/*					if (i >= xor)
					{
						moji += "-/";
						continue;
					}
*/					
					moji += i + "," + xor + "," + ((i & len) == 0) +"/";
				}
				print(moji);
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
