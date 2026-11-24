using UnityEngine;

namespace MythicChronicles
{
    /// <summary>
    /// Device-safe shader lookup. In stripped Android builds Shader.Find can
    /// return null for any shader not referenced by the build; new Material(null)
    /// throws ArgumentNullException and used to kill battle creation on device.
    /// Every runtime material creation must resolve through here and tolerate a
    /// null result (skip or keep the renderer's default material).
    /// </summary>
    public static class ChroniclesShaders
    {
        public static Shader FindAny(params string[] names)
        {
            foreach (string name in names)
            {
                Shader shader = Shader.Find(name);
                if (shader != null) return shader;
            }
            return null;
        }
    }
}
