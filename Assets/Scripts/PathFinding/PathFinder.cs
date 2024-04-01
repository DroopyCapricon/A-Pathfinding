using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathMarker
{
    public MapLocation location;
    public float G, H, F;
    public GameObject marker;
    public PathMarker parent;

    public PathMarker(MapLocation location, float g, float h, float f, GameObject marker, PathMarker parent)
    {
        this.location = location;
        G = g;
        H = h;
        F = f;
        this.marker = marker;
        this.parent = parent;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || !this.GetType().Equals(obj.GetType())) return false;
        else return location.Equals(((PathMarker)obj).location);
    }

    public override int GetHashCode()
    {
        return 0;
    }
}

public class PathFinder : MonoBehaviour
{

    #region Variables
    public Maze maze;
    public GameObject start, end, pathMarker;
    public Material closedMaterial, openMaterial;
    private PathMarker goalNode, startNode, endNode;
    private List<PathMarker> closedMarker = new(), openMarker = new();
    private bool done;
    #endregion Variables

    private void RemoveAllMarkers()
    {
        GameObject[] markers = GameObject.FindGameObjectsWithTag("marker");
        foreach (GameObject marker in markers)
            Destroy(marker);
    }

    private void BeginSearch()
    {
        done = false;
        RemoveAllMarkers();

        List<MapLocation> locations = new();
        for (int z = 1; z < maze.depth - 1; z++)
            for (int x = 1; x < maze.depth - 1; x++)
                if (maze.map[x, z] != 1)
                    locations.Add(new MapLocation(x, z));

        locations.Shuffle();

        Vector3 startLocation = new(locations[0].x * maze.scale, 0, locations[0].z * maze.scale);
        startNode = new(new MapLocation(locations[0].x, locations[0].z), 0, 0, 0, Instantiate(start, startLocation, Quaternion.identity), null);


        Vector3 goalLocation = new(locations[1].x * maze.scale, 0, locations[1].z * maze.scale);
        goalNode = new(new MapLocation(locations[1].x, locations[1].z), 0, 0, 0, Instantiate(end, goalLocation, Quaternion.identity), null);

        openMarker.Clear();
        closedMarker.Clear();
        openMarker.Add(startNode);
        endNode = startNode;
    }

    private void Search(PathMarker node)
    {
        if (node == null) return;
        if (node.Equals(goalNode)) { done = true; return; }
        foreach (MapLocation dir in maze.directions)
        {
            MapLocation neighbour = dir + node.location;
            if (maze.map[neighbour.x, neighbour.z] == 1) continue;
            if (neighbour.x < 1 || neighbour.x >= maze.width || neighbour.z < 1 || neighbour.z >= maze.depth) continue;
            if (IsClose(neighbour)) continue;

            float G = Vector2.Distance(node.location.ToVector(), neighbour.ToVector()) + node.G;
            float H = Vector2.Distance(neighbour.ToVector(), goalNode.location.ToVector());
            float F = G + H;

            GameObject pathBlock = Instantiate(pathMarker, new Vector3(neighbour.x * maze.scale, 0, neighbour.z * maze.scale), Quaternion.identity);

            TextMesh[] values = pathBlock.GetComponentsInChildren<TextMesh>();
            values[0].text = "G: " + G.ToString("0.00");
            values[1].text = "H: " + H.ToString("0.00");
            values[2].text = "F: " + F.ToString("0.00");

            if (!UpdateMarker(neighbour, G, H, F, node))
                openMarker.Add(new PathMarker(neighbour, G, H, F, pathBlock, node));
        }

        openMarker = openMarker.OrderBy(p => p.F).ThenBy(n => n.H).ToList<PathMarker>();
        PathMarker pm = openMarker.ElementAt(0);
        closedMarker.Add(pm);
        openMarker.RemoveAt(0);
        pm.marker.GetComponent<Renderer>().material = closedMaterial;

        endNode = pm;
    }

    private bool UpdateMarker(MapLocation position, float g, float h, float f, PathMarker pathMarker)
    {
        foreach (PathMarker _pathMarker in openMarker)
        {
            if (_pathMarker.location.Equals(position))
            {
                _pathMarker.G = g;
                _pathMarker.H = h;
                _pathMarker.F = f;
                _pathMarker.parent = pathMarker;
                return true;
            }
        }
        return false;
    }

    private void GetPath()
    {
        RemoveAllMarkers();
        PathMarker begin = endNode;

        while (!startNode.Equals(begin) && begin != null)
        {
            Instantiate(pathMarker, new Vector3(begin.location.x * maze.scale, 0, begin.location.z * maze.scale), Quaternion.identity);
            begin = begin.parent;
        }

        Instantiate(pathMarker, new Vector3(startNode.location.x * maze.scale, 0, startNode.location.z * maze.scale), Quaternion.identity);
    }

    private bool IsClose(MapLocation marker)
    {
        foreach (PathMarker pathMarker in closedMarker)
            if (pathMarker.Equals(marker)) return true;
        return false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
            BeginSearch();
        if (Input.GetKeyDown(KeyCode.A) && !done)
            Search(endNode);
        if (Input.GetKeyDown(KeyCode.D))
            GetPath();
    }
}