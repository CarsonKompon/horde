using System;
using Sandbox;

public sealed class LightJitter : Component
{
	Vector3 StartPosition { get; set; }

	protected override void OnStart()
	{
		StartPosition = Transform.Position;
	}

	protected override void OnUpdate()
	{
		Transform.Position = StartPosition + Vector3.Random * Random.Shared.Float() / 4f;
	}
}