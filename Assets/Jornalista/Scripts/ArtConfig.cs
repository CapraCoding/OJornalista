using UnityEngine;

namespace Jornalista
{
    [CreateAssetMenu(fileName = "ArtConfig", menuName = "Jornalista/Art Config")]
    public class ArtConfig : ScriptableObject
    {
        [Header("Casa")]
        public Sprite casaFundo;
        public Sprite cama;
        public Sprite portaCasa;

        [Header("Rua (uma imagem larga, esticada pelo trajeto todo)")]
        public Sprite ruaFundo;
        public Sprite portaCasaRua;
        public Sprite portaTrabalho;

        [Header("Trabalho")]
        public Sprite trabalhoFundo;

        [Header("Jornalista (quadros da animação)")]
        public Sprite[] jornalistaParado;
        public Sprite[] jornalistaAndando;
        public float fps = 8f;

        public bool HasCharacterFrames =>
            jornalistaParado != null && jornalistaParado.Length > 0 && jornalistaParado[0] != null;

        static ArtConfig instance;

        public static ArtConfig I
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ArtConfig>("ArtConfig");
                    if (instance == null) instance = CreateInstance<ArtConfig>();
                }
                return instance;
            }
        }
    }
}
