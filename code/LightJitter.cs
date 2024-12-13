using System;
using Sandbox;

public sealed class LightJitter : Component
{
	Vector3 StartPosition { get; set; }

	protected override void OnStart()
	{
		StartPosition = WorldPosition;
	}

	protected override void OnUpdate()
	{
		WorldPosition = StartPosition + Vector3.Random * Random.Shared.Float() / 4f;
	}
}