#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Farm2Shelf.Environment;
using Farm2Shelf.Utils;

// Explicit batch test: no scene/save changes and no duplicate Unity project.
public static class MobileStartupRegressionProbe
{
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch mode only.");
        int failures = 0;
        string report = "Logs/TrafficCompile/mobile-startup-check.txt";
        Directory.CreateDirectory("Logs/TrafficCompile");
        File.WriteAllText(report, "Mobile mesh compatibility checks\n");
        void Check(bool ok, string message)
        {
            File.AppendAllText(report, (ok ? "PASS " : "FAIL ") + message + "\n");
            if (!ok) failures++;
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject wall = null, door = null;
        Material material = null;
        Mesh unreadable = null;
        try
        {
            Mesh cube = PrimitiveFactory.GetReadableCubeMesh();
            Check(cube.isReadable && cube.vertexCount == 24 && cube.triangles.Length == 36, "Owned cube retains CPU data");
            Vector3[] vertices = cube.vertices;
            Vector3[] normals = cube.normals;
            int[] triangles = cube.triangles;
            bool outward = true;
            for (int i = 0; i < triangles.Length; i += 3)
                outward &= Vector3.Dot(Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]],
                    vertices[triangles[i + 2]] - vertices[triangles[i]]), normals[triangles[i]]) > 0;
            Check(outward && cube.bounds.size == Vector3.one, "Six faces have outward winding and original unit dimensions");
            unreadable = UnityEngine.Object.Instantiate(cube);
            unreadable.UploadMeshData(true);
            Check(!unreadable.isReadable, "Test source simulates non-readable player mesh");
            material = ShaderHelper.CreateLitMaterial(Color.gray, "Probe_Wall");
            wall = new GameObject("Front_Probe", typeof(MeshFilter), typeof(MeshRenderer));
            wall.transform.position = Vector3.up * 1.5f;
            wall.transform.localScale = new Vector3(3, 3, .38f);
            wall.GetComponent<MeshFilter>().sharedMesh = unreadable;
            wall.GetComponent<MeshRenderer>().sharedMaterial = material;
            var visual = wall.AddComponent<StoreWallVisual>();
            visual.Initialize(material);
            var surface = wall.transform.Find("Wall_Surface").GetComponent<MeshFilter>().sharedMesh;
            Check(surface.isReadable && surface.subMeshCount == 2 && surface.GetTriangles(1).Length == 6,
                "Wall cutaway and exterior face work with non-readable source");
            door = new GameObject("Probe_Door", typeof(MeshFilter), typeof(MeshRenderer));
            door.GetComponent<MeshFilter>().sharedMesh = unreadable;
            door.GetComponent<MeshRenderer>().sharedMaterial = material;
            door.AddComponent<StoreDoorVisual>().Initialize(visual);
            Check(door.GetComponent<MeshFilter>().sharedMesh == unreadable, "Unknown non-readable door meshes safely use visibility fallback");
            var left = new GameObject("Left").transform;
            left.SetParent(door.transform);
            var right = new GameObject("Right").transform;
            right.SetParent(door.transform);
            door.AddComponent<StoreEntranceModel>().Build(unreadable, material, left, right, true, 3, true);
            bool readable = true;
            foreach (var filter in door.GetComponentsInChildren<MeshFilter>())
                if (filter.transform != door.transform) readable &= filter.sharedMesh.isReadable;
            Check(readable, "All generated entrance fittings use readable geometry");
        }
        catch (Exception ex) { failures++; File.AppendAllText(report, ex + "\n"); }
        // Exiting batch discards this unsaved scene and releases all test objects.
        File.AppendAllText(report, "Failures: " + failures + "\n");
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }
}
#endif
