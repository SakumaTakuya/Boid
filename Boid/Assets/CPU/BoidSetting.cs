using System;

[Serializable]
public struct BoidSetting
{
    public float Radius;
    public float Weight;

    public BoidSetting(float radius, float weight)
    {
        Radius = radius;
        Weight = weight;
    }
}