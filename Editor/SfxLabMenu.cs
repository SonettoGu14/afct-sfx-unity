using UnityEditor;
using UnityEngine;

namespace AFCT.SfxLab.EditorTools
{
    internal static class SfxLabMenu
    {
        private const string SizedKey = "afct-sfx-lab.window-sized";

        [MenuItem("Tools/AFCT SFX Lab", false, 2000)]
        private static void OpenSfxLab()
        {
            SfxLabWindow window = EditorWindow.GetWindow<SfxLabWindow>(false, "AFCT SFX Lab", true);

            // The first time the tool is opened, size it to the content. Later opens keep
            // whatever size the dock or the user gave it.
            if (!EditorPrefs.GetBool(SizedKey, false))
            {
                Rect rect = window.position;
                window.position = new Rect(rect.x, rect.y, 900f, 780f);
                EditorPrefs.SetBool(SizedKey, true);
            }

            window.Show();
        }
    }
}
