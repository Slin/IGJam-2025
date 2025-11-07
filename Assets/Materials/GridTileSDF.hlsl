void hexSDF_float(float3 localPosition, float3 worldPosition, float radius, out float value)
{
    float3 k = float3(-0.866025404, 0.5, 0.577350269);
    float2 p = localPosition.xy + sin(worldPosition.xy * 20.0 + _Time.y * 5.0) * 0.005;
    radius *= 0.866025404;
    p = abs(p);
    p -= 2.0 * min(dot(k.xy, p), 0.0) * k.xy;
    p -= float2(clamp(p.x, -k.z * radius, k.z * radius), radius);
    float distance = length(p) * sign(p.y);

    if(distance <= 0.0)
    {
	    float t = distance / 0.1;
		t = saturate(1.0 + t);
		value = pow(t, 10);
	}
	else
	{
		value = 0.0;
	}
}