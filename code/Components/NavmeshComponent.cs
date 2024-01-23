using System.Collections.Generic;
using Sandbox;

public sealed class NavmeshComponent : Component
{
    public static NavmeshComponent Instance { get; private set; }
    public NavigationMesh NavMesh { get; private set; } = null;

    MapInstance map;

    protected override void OnStart()
    {
        Instance = this;

        map = Components.Get<MapInstance>();
        map.OnMapLoaded += GenerateMesh;
    }

    protected override void OnUpdate()
    {
        if ( NavMesh is null )
        {
            GenerateMesh();
            return;
        }

        Log.Info( "we have a navmesh" );

        using ( Gizmo.Scope( "navmesh" ) )
        {
            Gizmo.Draw.Color = Color.Blue;
            Gizmo.Draw.LineNavigationMesh( NavMesh );
        }
    }

    async void GenerateMesh()
    {
        NavMesh?.Dispose();

        NavMesh = new();
        NavMesh.Generate( Scene.PhysicsWorld );
        Log.Info( $"Generated navmesh... Node count: {NavMesh?.Nodes?.Count ?? 0}" );
    }

    public List<NavigationPath.Segment> GeneratePath( Vector3 point1, Vector3 point2 )
    {
        if ( NavMesh is null )
        {
            GenerateMesh();
        }

        var path = new NavigationPath( NavMesh );
        path.StartPoint = point1;
        path.EndPoint = point2;
        path.Build();

        return path.Segments;
    }
}