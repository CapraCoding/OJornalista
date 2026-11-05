using UnityEngine;
using UnityEngine.UI;

namespace Jornalista
{
    public class HouseScreen : MonoBehaviour
    {
        GameManager gm;
        Image wall, sky, moon, sun;
        Button door, bed;
        Pulse doorPulse, bedPulse;
        HomeTV homeTv;
        Text hint, calendar;
        PlayerView player;

        static readonly Color WallDay = new Color(0.86f, 0.80f, 0.68f);
        static readonly Color WallNight = new Color(0.36f, 0.38f, 0.50f);

        public void Build(GameManager gm)
        {
            this.gm = gm;
            var rt = (RectTransform)transform;
            var art = ArtConfig.I;

            wall = UI.Img(rt, "Fundo", WallDay, art.casaFundo);
            wall.rectTransform.Stretch();

            if (art.casaFundo == null)
            {
                UI.Img(rt, "Chao", new Color(0.45f, 0.32f, 0.22f)).rectTransform.Anchor(0, 0, 1, 0.2f);
                UI.Img(rt, "Rodape", new Color(0.32f, 0.22f, 0.15f)).rectTransform.Anchor(0, 0.2f, 1, 0.23f);

                var frame = UI.Img(rt, "Janela", new Color(0.95f, 0.95f, 0.92f));
                frame.rectTransform.Anchor(0.38f, 0.48f, 0.60f, 0.86f);
                sky = UI.Img(frame.transform, "Ceu", Color.cyan);
                sky.rectTransform.Anchor(0.06f, 0.05f, 0.94f, 0.95f);
                sun = UI.Img(sky.transform, "Sol", new Color(1f, 0.85f, 0.3f)).MakeCircle();
                sun.rectTransform.Anchor(0.12f, 0.55f, 0.42f, 0.9f);
                moon = UI.Img(sky.transform, "Lua", new Color(0.95f, 0.95f, 0.85f)).MakeCircle();
                moon.rectTransform.Anchor(0.58f, 0.58f, 0.85f, 0.9f);
                UI.Img(frame.transform, "BarraV", new Color(0.95f, 0.95f, 0.92f)).rectTransform.Anchor(0.485f, 0, 0.515f, 1);
                UI.Img(frame.transform, "BarraH", new Color(0.95f, 0.95f, 0.92f)).rectTransform.Anchor(0, 0.485f, 1, 0.515f);

                var cal = UI.Img(rt, "Calendario", Color.white);
                cal.rectTransform.Anchor(0.66f, 0.55f, 0.75f, 0.80f);
                UI.Img(cal.transform, "Topo", new Color(0.75f, 0.15f, 0.15f)).rectTransform.Anchor(0, 0.78f, 1, 1);
                calendar = UI.Txt(cal.transform, "Numero", "1", 80, new Color(0.15f, 0.15f, 0.15f),
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                calendar.rectTransform.Anchor(0, 0, 1, 0.78f);

                var quadro = UI.Img(rt, "Quadro", new Color(0.35f, 0.25f, 0.18f));
                quadro.rectTransform.Anchor(0.08f, 0.6f, 0.2f, 0.8f);
                quadro.rectTransform.localRotation = Quaternion.Euler(0, 0, 3f);
                UI.Img(quadro.transform, "Tela", new Color(0.55f, 0.7f, 0.6f)).rectTransform.Anchor(0.08f, 0.08f, 0.92f, 0.92f);
            }

            BuildBed(rt, art);
            BuildDoor(rt, art);
            BuildTV(rt);

            player = PlayerView.Create(rt);
            var p = (RectTransform)player.transform;
            p.anchorMin = p.anchorMax = new Vector2(0.70f, 0.1f);
            p.anchoredPosition = Vector2.zero;

            hint = UI.Txt(rt, "Dica", "", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).WithShadow();
            hint.rectTransform.Anchor(0.2f, 0.01f, 0.8f, 0.09f);
        }

        void BuildBed(RectTransform rt, ArtConfig art)
        {
            bed = UI.Btn(rt, "Cama", null, new Color(0.40f, 0.26f, 0.16f), Color.white, 30, () => gm.Sleep(), art.cama);
            bed.GetComponent<RectTransform>().Anchor(0.03f, 0.07f, 0.36f, 0.42f);
            if (art.cama == null)
            {
                UI.Img(bed.transform, "Cabeceira", new Color(0.32f, 0.20f, 0.12f)).Round().rectTransform.Anchor(0, 0.3f, 0.08f, 1f);
                UI.Img(bed.transform, "Colchao", new Color(0.95f, 0.95f, 0.95f)).Round().rectTransform.Anchor(0.08f, 0.45f, 1f, 0.72f);
                UI.Img(bed.transform, "Coberta", new Color(0.25f, 0.40f, 0.65f)).Round().rectTransform.Anchor(0.35f, 0.4f, 1f, 0.76f);
                UI.Img(bed.transform, "Travesseiro", Color.white).Round().rectTransform.Anchor(0.1f, 0.68f, 0.3f, 0.88f);
            }
            bedPulse = bed.gameObject.AddComponent<Pulse>();
            bedPulse.Amount = 0.025f;
        }

        void BuildDoor(RectTransform rt, ArtConfig art)
        {
            if (art.portaCasa == null)
            {
                UI.Img(rt, "Batente", new Color(0.30f, 0.20f, 0.12f)).rectTransform.Anchor(0.805f, 0.2f, 0.965f, 0.8f);
            }
            door = UI.Btn(rt, "Porta", null, new Color(0.55f, 0.36f, 0.20f), Color.white, 30, () => gm.LeaveHouse(), art.portaCasa);
            door.GetComponent<RectTransform>().Anchor(0.815f, 0.2f, 0.955f, 0.78f);
            if (art.portaCasa == null)
            {
                UI.Img(door.transform, "Painel1", new Color(0.48f, 0.31f, 0.17f)).rectTransform.Anchor(0.15f, 0.55f, 0.85f, 0.92f);
                UI.Img(door.transform, "Painel2", new Color(0.48f, 0.31f, 0.17f)).rectTransform.Anchor(0.15f, 0.08f, 0.85f, 0.45f);
                UI.Img(door.transform, "Macaneta", new Color(0.9f, 0.8f, 0.3f)).MakeCircle().rectTransform.Anchor(0.72f, 0.46f, 0.86f, 0.54f);
            }
            doorPulse = door.gameObject.AddComponent<Pulse>();
            doorPulse.Amount = 0.03f;
        }

        void BuildTV(RectTransform rt)
        {
            homeTv = HomeTV.Create(rt, 0.42f, 0.10f, 0.62f, 0.60f, () =>
            {
                homeTv.StopPulse();
                gm.WatchTV();
            });
        }

        public void Enter(bool morning)
        {
            if (ArtConfig.I.casaFundo == null) wall.color = morning ? WallDay : WallNight;
            if (sky != null)
            {
                sky.color = morning ? new Color(0.55f, 0.80f, 0.98f) : new Color(0.06f, 0.08f, 0.22f);
                sun.gameObject.SetActive(morning);
                moon.gameObject.SetActive(!morning);
            }
            if (calendar != null) calendar.text = gm.Day.ToString();

            door.interactable = morning;
            doorPulse.On = morning;
            bed.interactable = !morning;
            bedPulse.On = !morning;

            player.Facing = morning ? 1 : -1;
            player.Walking = false;

            var report = gm.LastReport;
            bool hasReport = report != null && !string.IsNullOrEmpty(report.frase);
            bool fresh = hasReport && report.dia == gm.Day && !morning;
            homeTv.SetState(hasReport ? report : null, fresh);

            string main = morning ? "Toque na porta para ir trabalhar" : "Toque na cama para dormir";
            hint.text = hasReport ? main + "  ·  TV para assistir" : main;
        }
    }
}
