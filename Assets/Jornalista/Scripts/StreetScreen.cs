using UnityEngine;
using UnityEngine.UI;

namespace Jornalista
{
    public class StreetScreen : MonoBehaviour
    {
        const float WorldWidth = 5200f;
        const float HouseDoorX = 420f;
        const float WorkDoorX = 4780f;
        const float Speed = 620f;
        const float Ground = 0.2f;
        const float DoorReach = 170f;

        GameManager gm;
        RectTransform view, world, playerRt;
        PlayerView player;
        HoldZone holdLeft, holdRight;
        Button houseDoor, workDoor;
        Pulse housePulse, workPulse;
        Text hint;
        Image sky;
        Billboard billboard;
        DangerSign danger;

        float x, moved;
        bool toWork;

        public void Build(GameManager gm)
        {
            this.gm = gm;
            view = (RectTransform)transform;
            var art = ArtConfig.I;

            sky = UI.Img(view, "Ceu", new Color(0.55f, 0.80f, 0.98f));
            sky.rectTransform.Stretch();

            holdLeft = HoldZone.Create(view, "SegurarEsquerda", 0f, 0f, 0.5f, 1f);
            holdRight = HoldZone.Create(view, "SegurarDireita", 0.5f, 0f, 1f, 1f);

            world = UI.NewRect(view, "Mundo");
            world.anchorMin = new Vector2(0, 0);
            world.anchorMax = new Vector2(0, 1);
            world.pivot = new Vector2(0, 0.5f);
            world.sizeDelta = new Vector2(WorldWidth, 0);
            world.anchoredPosition = Vector2.zero;

            if (art.ruaFundo != null)
            {
                var bg = UI.Img(world, "RuaArte", Color.white, art.ruaFundo);
                bg.rectTransform.Stretch();
            }
            else
            {
                BuildScenery();
            }

            W("OutdoorPosteA", 2400f, 18f, Ground, 0.52f, new Color(0.2f, 0.2f, 0.22f));
            W("OutdoorPosteB", 2800f, 18f, Ground, 0.52f, new Color(0.2f, 0.2f, 0.22f));
            billboard = Billboard.Create(world, 2600f, 900f, 0.5f, 0.88f);

            danger = DangerSign.Create(world, 1750f, Ground, () => player.Think("?"));

            houseDoor = BuildDoor("PortaCasa", HouseDoorX, new Color(0.55f, 0.36f, 0.20f), art.portaCasaRua, () => gm.ArriveHome());
            housePulse = houseDoor.gameObject.AddComponent<Pulse>();
            workDoor = BuildDoor("PortaJornal", WorkDoorX, new Color(0.25f, 0.30f, 0.38f), art.portaTrabalho, () => gm.EnterWork());
            workPulse = workDoor.gameObject.AddComponent<Pulse>();

            player = PlayerView.Create(world);
            playerRt = (RectTransform)player.transform;
            playerRt.anchorMin = playerRt.anchorMax = new Vector2(0, Ground);

            hint = UI.Txt(view, "Dica", "", 42, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).WithShadow();
            hint.rectTransform.Anchor(0.15f, 0.78f, 0.85f, 0.88f);
        }

        Image W(string name, float cx, float width, float yMin, float yMax, Color c)
        {
            var img = UI.Img(world, name, c);
            var r = img.rectTransform;
            r.anchorMin = new Vector2(0, yMin);
            r.anchorMax = new Vector2(0, yMax);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(width, 0);
            r.anchoredPosition = new Vector2(cx, 0);
            return img;
        }

        void BuildScenery()
        {
            float[] heights = { 0.62f, 0.48f, 0.72f, 0.55f };
            Color[] colors =
            {
                new Color(0.62f, 0.58f, 0.55f), new Color(0.55f, 0.60f, 0.66f),
                new Color(0.70f, 0.64f, 0.52f), new Color(0.58f, 0.55f, 0.62f)
            };
            int i = 0;
            for (float bx = 950f; bx < 4300f; bx += 330f, i++)
            {
                var b = W("Predio", bx, 300f, Ground, heights[i % 4], colors[i % 4]);
                for (int col = 0; col < 2; col++)
                for (int row = 0; row < 4; row++)
                {
                    float x0 = col == 0 ? 0.18f : 0.58f;
                    float y0 = 0.12f + row * 0.2f;
                    UI.Img(b.transform, "Janela", new Color(0.85f, 0.88f, 0.70f, 0.9f))
                        .rectTransform.Anchor(x0, y0, x0 + 0.24f, y0 + 0.12f);
                }
            }

            W("Asfalto", WorldWidth / 2f, WorldWidth, 0f, 0.13f, new Color(0.22f, 0.22f, 0.24f));
            for (float dx = 120f; dx < WorldWidth; dx += 260f)
                W("Faixa", dx, 120f, 0.06f, 0.07f, new Color(0.9f, 0.85f, 0.5f));
            W("Meiofio", WorldWidth / 2f, WorldWidth, 0.13f, 0.14f, new Color(0.55f, 0.55f, 0.55f));
            W("Calcada", WorldWidth / 2f, WorldWidth, 0.14f, Ground, new Color(0.72f, 0.72f, 0.70f));

            for (float lx = 750f; lx < WorldWidth - 400f; lx += 700f)
            {
                W("Poste", lx, 12f, Ground, 0.64f, new Color(0.2f, 0.2f, 0.22f));
                W("Lampada", lx, 46f, 0.62f, 0.67f, new Color(1f, 0.95f, 0.7f)).MakeCircle();
            }

            var house = W("Casa", HouseDoorX, 560f, Ground, 0.64f, new Color(0.85f, 0.75f, 0.58f));
            UI.Img(house.transform, "JanelaCasa", new Color(0.6f, 0.8f, 0.95f)).rectTransform.Anchor(0.08f, 0.45f, 0.32f, 0.8f);
            UI.Img(house.transform, "JanelaCasa2", new Color(0.6f, 0.8f, 0.95f)).rectTransform.Anchor(0.68f, 0.45f, 0.92f, 0.8f);
            W("Telhado", HouseDoorX, 620f, 0.64f, 0.73f, new Color(0.55f, 0.22f, 0.18f));

            var office = W("Jornal", WorkDoorX, 760f, Ground, 0.96f, new Color(0.42f, 0.47f, 0.55f));
            for (int col = 0; col < 4; col++)
            for (int row = 0; row < 4; row++)
            {
                float x0 = 0.06f + col * 0.235f;
                float y0 = 0.40f + row * 0.11f;
                UI.Img(office.transform, "Vidro", new Color(0.7f, 0.85f, 0.95f, 0.85f))
                    .rectTransform.Anchor(x0, y0, x0 + 0.18f, y0 + 0.08f);
            }
            var sign = UI.Img(office.transform, "Letreiro", new Color(0.12f, 0.12f, 0.14f));
            sign.rectTransform.Anchor(0.05f, 0.86f, 0.95f, 0.97f);
            UI.Txt(sign.transform, "Nome", "O DIÁRIO", 52, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Stretch();
        }

        Button BuildDoor(string name, float cx, Color c, Sprite art, UnityEngine.Events.UnityAction onClick)
        {
            var b = UI.Btn(world, name, null, c, Color.white, 30, onClick, art);
            var r = (RectTransform)b.transform;
            r.anchorMin = new Vector2(0, Ground);
            r.anchorMax = new Vector2(0, 0.47f);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(150f, 0);
            r.anchoredPosition = new Vector2(cx, 0);
            if (art == null)
                UI.Img(b.transform, "Macaneta", new Color(0.9f, 0.8f, 0.3f)).MakeCircle()
                    .rectTransform.Anchor(0.7f, 0.45f, 0.86f, 0.55f);
            return b;
        }

        public void Enter(bool goingToWork)
        {
            toWork = goingToWork;
            x = toWork ? HouseDoorX : WorkDoorX;
            moved = 0f;
            player.Facing = toWork ? 1 : -1;
            player.Walking = false;
            sky.color = toWork ? new Color(0.55f, 0.80f, 0.98f) : new Color(0.93f, 0.55f, 0.38f);

            billboard.SetReport(gm.LastReport);
            danger.gameObject.SetActive(gm.PublishedCount > 0);
            danger.SetLevel(gm.PublishedCount);

            Refresh(false);
        }

        void Update()
        {
            float dir = 0f;
            if (!gm.Busy && !gm.OverlayOpen)
            {
                if (holdRight.IsHeld) dir += 1f;
                if (holdLeft.IsHeld) dir -= 1f;
                dir = Mathf.Clamp(dir + DevInput.Horizontal(), -1f, 1f);
            }

            float before = x;
            if (Mathf.Abs(dir) > 0.01f)
            {
                x = Mathf.Clamp(x + dir * Speed * Time.deltaTime, HouseDoorX, WorkDoorX);
                player.Facing = dir > 0 ? 1 : -1;
            }
            moved += Mathf.Abs(x - before);
            Refresh(Mathf.Abs(x - before) > 0.01f);
        }

        void Refresh(bool walking)
        {
            player.Walking = walking;
            playerRt.anchoredPosition = new Vector2(x, 0);

            float viewW = view.rect.width;
            float cam = Mathf.Clamp(x - viewW * 0.5f, 0f, Mathf.Max(0f, WorldWidth - viewW));
            world.anchoredPosition = new Vector2(-cam, 0);

            bool nearHouse = !toWork && Mathf.Abs(x - HouseDoorX) < DoorReach;
            bool nearWork = toWork && Mathf.Abs(x - WorkDoorX) < DoorReach;
            SetDoor(houseDoor, housePulse, nearHouse);
            SetDoor(workDoor, workPulse, nearWork);

            if (nearHouse || nearWork) hint.text = "Toque na porta para entrar";
            else if (moved < 250f)
                hint.text = toWork ? "Segure o lado DIREITO da tela para andar"
                                   : "Segure o lado ESQUERDO da tela para voltar";
            else hint.text = "";
        }

        static void SetDoor(Button b, Pulse p, bool on)
        {
            b.interactable = on;
            b.targetGraphic.raycastTarget = on;
            p.On = on;
        }
    }
}
