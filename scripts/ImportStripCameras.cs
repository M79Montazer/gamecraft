using Godot;

// Import script for DCC-exported FBX models (wired via import_script/path in
// the .import file). Artists' exports can carry a leftover scene camera that
// hijacks the viewport when instanced, so every Camera3D is stripped from the
// imported scene before it is saved. The source FBX itself is never modified.
#if TOOLS
public partial class ImportStripCameras : EditorScenePostImport
{
	public override GodotObject _PostImport(Node scene)
	{
		StripCameras(scene);
		return scene;
	}

	private static void StripCameras(Node node)
	{
		if (node is Camera3D)
		{
			node.GetParent().RemoveChild(node);
			node.QueueFree();
			return;
		}
		foreach (Node child in node.GetChildren())
			StripCameras(child);
	}
}
#endif
