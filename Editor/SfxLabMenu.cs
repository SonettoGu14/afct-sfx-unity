using UnityEditor;
using UnityEngine;

namespace AFCT.SfxLab.EditorTools
{
    internal static class SfxLabMenu
    {
        [MenuItem("Tools/AFCT SFX Lab", false, 2000)]
        private static void OpenSfxLab()
        {
            SfxLabWindow window = EditorWindow.GetWindow<SfxLabWindow>(false, "AFCT SFX Lab", true);
            window.minSize = new Vector2(900f, 600f);
            window.Show();
        }
    }
}
