using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Jornalista
{
    public class GameManager : MonoBehaviour
    {
        const string DayKey = "jornalista_dia";
        const string ReportKey = "jornalista_reportagem";
        const string PublishedKey = "jornalista_publicadas";
        const string StrikesKey = "jornalista_advertencias";
        const string DaysPublishedKey = "jornalista_dias_publicados";
        const string LastPublishedDayKey = "jornalista_ultimo_dia_publicado";
        const string GoodStreakKey = "jornalista_dias_corretos";

        public static GameManager I { get; private set; }

        public BancoDeNoticias Banco { get; private set; }
        public int Day { get; private set; }
        public bool Busy { get; private set; }
        public Reportagem LastReport { get; private set; }
        public int PublishedCount { get; private set; }
        public int Strikes { get; private set; }
        public int DaysPublished { get; private set; }
        public int GoodStreak { get; private set; }
        int lastPublishedDay;
        bool sentHomeToday;
        string hudTime = "07:00";
        public bool OverlayOpen => tv != null && tv.gameObject.activeSelf;

        RectTransform root;
        HouseScreen house;
        StreetScreen street;
        WorkScreen work;
        TVBroadcast tv;
        CanvasGroup fader;
        Text card, hud;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (FindAnyObjectByType<GameManager>() == null)
                new GameObject("GameManager").AddComponent<GameManager>();
        }

        void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            Banco = NewsLoader.Load();
            Day = Mathf.Max(1, PlayerPrefs.GetInt(DayKey, 1));
            LastReport = LoadReport();
            PublishedCount = PlayerPrefs.GetInt(PublishedKey, 0);
            Strikes = PlayerPrefs.GetInt(StrikesKey, 0);
            DaysPublished = PlayerPrefs.GetInt(DaysPublishedKey, 0);
            lastPublishedDay = PlayerPrefs.GetInt(LastPublishedDayKey, 0);
            GoodStreak = PlayerPrefs.GetInt(GoodStreakKey, 0);
            if (Strikes >= Banco.advertenciasParaDemissao) ResetProgress();

            EnsureCamera();
            EnsureEventSystem();
            BuildUI();
            Debug.Log($"[Jornalista] Jogo iniciado - dia {Day}, {CycleLength} dia(s) de notícias, " +
                      $"{Synonyms.GroupCount} grupo(s) de sinônimos." +
                      (NewsLoader.LastError != null ? " AVISO: " + NewsLoader.LastError : "") +
                      (Synonyms.LastError != null ? " AVISO: " + Synonyms.LastError : ""));
        }

        void Start() => StartCoroutine(Intro());

        static void EnsureCamera()
        {
            if (Camera.main != null) return;
            var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0, 0, -10);
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        void BuildUI()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            root = (RectTransform)go.transform;

            house = MakeScreen<HouseScreen>("Casa");
            house.Build(this);
            street = MakeScreen<StreetScreen>("Rua");
            street.Build(this);
            work = MakeScreen<WorkScreen>("Trabalho");
            work.Build(this);

            hud = UI.Txt(root, "HUD", "", 38, Color.white, TextAnchor.UpperLeft, FontStyle.Bold).WithShadow();
            hud.rectTransform.Anchor(0.015f, 0.88f, 0.5f, 0.985f);

            tv = MakeScreen<TVBroadcast>("TV");
            tv.Build();

            var f = UI.Img(root, "Fade", Color.black, null, true);
            f.rectTransform.Stretch();
            fader = f.gameObject.AddComponent<CanvasGroup>();
            card = UI.Txt(f.transform, "Cartela", "", 110, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            card.rectTransform.Stretch();
        }

        T MakeScreen<T>(string name) where T : Component
        {
            var r = UI.NewRect(root, name).Stretch();
            return r.gameObject.AddComponent<T>();
        }

        void ShowOnly(Component screen)
        {
            house.gameObject.SetActive(screen == house);
            street.gameObject.SetActive(screen == street);
            work.gameObject.SetActive(screen == work);
        }

        void SetHud(string time)
        {
            hudTime = time;
            string text = $"DIA {Day}  ·  {time}";
            if (Strikes > 0)
                text += $"\n<color=#FF6B5E>ADVERTÊNCIAS {Strikes}/{Banco.advertenciasParaDemissao}</color>" +
                        $"  <size=26>· {GoodStreak}/{Banco.diasCorretosParaRetirarAdvertencia} dias certos</size>";
            hud.text = text;
        }

        public void LeaveHouse() => Transition(() =>
        {
            ShowOnly(street);
            street.Enter(true);
            SetHud("07:40");
        });

        public void EnterWork() => Transition(() =>
        {
            ShowOnly(work);
            work.Enter(TodaysNews());
            SetHud("08:00");
        });

        public void LeaveWork() => Transition(() =>
        {
            ShowOnly(street);
            street.Enter(false);
            SetHud("18:00");
        });

        public void ArriveHome() => Transition(() =>
        {
            ShowOnly(house);
            house.Enter(false);
            SetHud("18:20");
        });

        void CloseDay()
        {
            bool goodDay = !sentHomeToday && lastPublishedDay == Day;
            sentHomeToday = false;
            if (!goodDay) return;
            GoodStreak++;
            if (GoodStreak >= Banco.diasCorretosParaRetirarAdvertencia)
            {
                GoodStreak = 0;
                if (Strikes > 0) Strikes--;
                PlayerPrefs.SetInt(StrikesKey, Strikes);
            }
            PlayerPrefs.SetInt(GoodStreakKey, GoodStreak);
        }

        public void Sleep() => Transition(() =>
        {
            CloseDay();
            Day++;
            PlayerPrefs.SetInt(DayKey, Day);
            PlayerPrefs.Save();
            if (CycleLength > 0 && (Day - 1) % CycleLength == 0) AiredHistory.Clear();
            ShowOnly(house);
            house.Enter(true);
            SetHud("07:00");
        }, () => $"DIA {Day}", 1.6f);

        public void RegisterReport(Noticia n, string frase)
        {
            var chamadas = new List<string>();
            if (n.chamadasSensacionalistas != null)
                foreach (var c in n.chamadasSensacionalistas) if (!string.IsNullOrWhiteSpace(c)) chamadas.Add(c);
            if (chamadas.Count == 0 && Banco.chamadasGenericas != null)
                foreach (var c in Banco.chamadasGenericas) if (!string.IsNullOrWhiteSpace(c)) chamadas.Add(c);

            LastReport = new Reportagem
            {
                titulo = n.titulo,
                texto = n.texto,
                local = n.local,
                frase = frase,
                chamadas = chamadas.ToArray(),
                dia = Day
            };
            PublishedCount++;
            PlayerPrefs.SetInt(PublishedKey, PublishedCount);
            if (lastPublishedDay != Day)
            {
                lastPublishedDay = Day;
                DaysPublished++;
                PlayerPrefs.SetInt(LastPublishedDayKey, lastPublishedDay);
                PlayerPrefs.SetInt(DaysPublishedKey, DaysPublished);
            }
            PlayerPrefs.SetString(ReportKey, JsonUtility.ToJson(LastReport));
            PlayerPrefs.Save();
        }

        public bool RegisterBadDay()
        {
            sentHomeToday = true;
            GoodStreak = 0;
            PlayerPrefs.SetInt(GoodStreakKey, GoodStreak);
            Strikes++;
            PlayerPrefs.SetInt(StrikesKey, Strikes);
            PlayerPrefs.Save();
            SetHud(hudTime);
            return Strikes >= Banco.advertenciasParaDemissao;
        }

        public void RestartAfterFiring() => Transition(() =>
        {
            ResetProgress();
            ShowOnly(house);
            house.Enter(true);
            SetHud("07:00");
        }, () => $"DIA {Day}", 1.6f);

        public void WatchTV()
        {
            if (Busy || LastReport == null || OverlayOpen) return;
            tv.Show(LastReport, Banco.chamadasGenericas, null);
        }

        static Reportagem LoadReport()
        {
            var json = PlayerPrefs.GetString(ReportKey, "");
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<Reportagem>(json); }
            catch { return null; }
        }

        bool UsesModels => Banco.modelos != null && Banco.modelos.Length > 0;

        int CycleLength => UsesModels ? NewsGenerator.CycleDays(Banco) : (Banco.dias != null ? Banco.dias.Length : 0);

        public List<Noticia> TodaysNews()
        {
            if (UsesModels) return NewsGenerator.ForDay(Banco, Day);
            var list = new List<Noticia>();
            if (CycleLength == 0) return list;
            var dia = Banco.dias[(Day - 1) % CycleLength];
            if (dia == null || dia.noticias == null) return list;
            foreach (var n in dia.noticias)
            {
                if (n == null) continue;
                if (list.Count >= Banco.manchetesPorDia) break;
                list.Add(n);
            }
            return list;
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(DayKey);
            PlayerPrefs.DeleteKey(ReportKey);
            PlayerPrefs.DeleteKey(PublishedKey);
            PlayerPrefs.DeleteKey(StrikesKey);
            PlayerPrefs.DeleteKey(DaysPublishedKey);
            PlayerPrefs.DeleteKey(LastPublishedDayKey);
            PlayerPrefs.DeleteKey(GoodStreakKey);
            DaysPublished = 0;
            lastPublishedDay = 0;
            GoodStreak = 0;
            sentHomeToday = false;
            PublishedCount = 0;
            Strikes = 0;
            AiredHistory.Clear();
            Day = 1;
            LastReport = null;
        }

        IEnumerator Intro()
        {
            Busy = true;
            fader.alpha = 1f;
            fader.blocksRaycasts = true;
            ShowOnly(house);
            house.Enter(true);
            SetHud("07:00");
            card.text = $"DIA {Day}";
            yield return new WaitForSeconds(1.4f);
            card.text = "";
            yield return Fade(0f, 0.5f);
            fader.blocksRaycasts = false;
            Busy = false;
        }

        void Transition(Action middle, Func<string> cardText = null, float hold = 0f)
        {
            if (Busy) return;
            StartCoroutine(TransitionRoutine(middle, cardText, hold));
        }

        IEnumerator TransitionRoutine(Action middle, Func<string> cardText, float hold)
        {
            Busy = true;
            fader.blocksRaycasts = true;
            yield return Fade(1f, 0.35f);
            middle?.Invoke();
            if (cardText != null)
            {
                card.text = cardText();
                yield return new WaitForSeconds(hold);
                card.text = "";
            }
            yield return Fade(0f, 0.35f);
            fader.blocksRaycasts = false;
            Busy = false;
        }

        IEnumerator Fade(float target, float duration)
        {
            float start = fader.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                fader.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            fader.alpha = target;
        }

#if UNITY_EDITOR
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            bool reset = k != null && k.f5Key.wasPressedThisFrame;
#else
            bool reset = Input.GetKeyDown(KeyCode.F5);
#endif
            if (reset)
            {
                ResetProgress();
                I = null;
                Destroy(gameObject);
                new GameObject("GameManager").AddComponent<GameManager>();
            }
        }
#endif
    }
}
