using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Heatmap))]
public class HeatmapEditor : Editor
{

    private HeatmapEditorTask Task;
    private string HeatmapFilename = "UntitledHeatmap";
    private string SessionFilename = "UntitledSession";

    private void Save(string filename, Heatmap heatmap)
    {
        // Serializes the heatmap data and saves the data to the given path (and overwrites if necessary)
        StreamWriter streamWriter = new StreamWriter("Assets/Resources/Heatmap/" + filename + ".txt", false);
        streamWriter.Write(Json.Serialize(heatmap.Data));
        streamWriter.Close();

        // Reimports the file because Unity doesn't realize changes made to it
        AssetDatabase.ImportAsset("Assets/Resources/Heatmap/" + filename + ".txt");

        HeatmapFilename = filename;
    }

    private bool SaveAs(string filename, Heatmap heatmap, ref bool shouldRebuild)
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Heatmap/" + filename);

        // Does the same thing as above, but asks if you want to overwrite the file in case it already exists
        if ((textAsset == null) || EditorUtility.DisplayDialog("Warning", "That file already exists, do you want to overwrite it?", "Yes", "No"))
        {
            if (shouldRebuild)
            {
                heatmap.RefreshData();
                shouldRebuild = false;
            }

            Save(filename, heatmap);

            // Also, assigns the file to the heatmap
            heatmap.File = Resources.Load<TextAsset>("Heatmap/" + filename);

            Task = HeatmapEditorTask.ViewOrUpdate;

            return true;
        }

        return false;
    }

    private void AutoSave()
    {
        // When the game is closed, saves a file named "AutoSave" if the user forgot to save the session
        Heatmap heatmap = (Heatmap)target;
        Save("AutoSave", heatmap);
    }

    public override void OnInspectorGUI()
    {
        Heatmap heatmap = (Heatmap)target;

        heatmap.AutoSave = AutoSave;

        Task = (HeatmapEditorTask)EditorGUILayout.EnumPopup("Mode", Task);

        TextAsset textAsset;
        bool shouldRebuild = false;

        switch (Task)
        {
            case HeatmapEditorTask.CreateEmpty:

                if (Application.isPlaying)
                    EditorGUILayout.HelpBox("Heatmaps can't be created while the game is being played!", MessageType.Warning);
                else
                {
                    heatmap.Pivot = EditorGUILayout.Vector2Field("Pivot", heatmap.Pivot);

                    Vector2 heatmapSize     = EditorGUILayout.Vector2Field("Size", heatmap.Size);
                    Vector2 heatmapDivision = EditorGUILayout.Vector2Field("Division", heatmap.Division);

                    HeatmapFilename = EditorGUILayout.TextField("Filename", HeatmapFilename);

                    if (
                        (heatmap.Size != heatmapSize) ||
                        (heatmap.Division != heatmapDivision) ||
                        ((heatmap.File != null) && (heatmap.File.name != HeatmapFilename))
                    )
                    {
                        heatmap.Size     = heatmapSize;
                        heatmap.Division = heatmapDivision;
                        heatmap.File     = null;

                        shouldRebuild = true;
                    }

                    if (GUILayout.Button("Save"))
                        SaveAs(HeatmapFilename, heatmap, ref shouldRebuild);
                }

                break;
            case HeatmapEditorTask.ViewOrUpdate:

                textAsset = (TextAsset)EditorGUILayout.ObjectField("File to view", heatmap.File, typeof(TextAsset));

                if (heatmap.File != textAsset)
                {
                    heatmap.File = textAsset;

                    shouldRebuild = true;
                }

                if (heatmap.File != null)
                    HeatmapFilename = heatmap.File.name;

                if (EditorApplication.isPlaying)
                {
                    if ((heatmap.File != null) && GUILayout.Button("Append session to the selected file"))
                        Save(heatmap.File.name, heatmap);

                    SessionFilename = EditorGUILayout.TextField("Filename", SessionFilename);

                    if (GUILayout.Button("Save session to a different file"))
                    {
                        SaveAs(SessionFilename, heatmap, ref shouldRebuild);
                        SessionFilename = string.Empty;
                    }
                }

                // Draws the default inspector for the heatmap
                base.OnInspectorGUI();

                break;
        }

        // Some of the changes done required to rebuild the heatmap, but it should always update the position
        if (shouldRebuild)
        {
            heatmap.RefreshData();
            SceneView.RepaintAll();
        }
        else
        {
            heatmap.RefreshPosition();
            SceneView.RepaintAll();
        }
    }

}