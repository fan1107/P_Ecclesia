using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Ecclesia
{
    public sealed class EcclesiaApp : MonoBehaviour
    {
        ChapelWorld world; Visitor visitor; AuthenticationService auth; Camera view; PixelViewport pixels;
        RectTransform safeArea; GameObject loginPanel, prayerPanel, hud;
        Text status, hint, location, seatLabel; Button googleButton, enterButton, prayerButton, signOutButton;
        InputField prayerInput; Font font; bool entered, transitioning;
        Vector2Int destination; string pendingPrayer="";

        void Awake()
        {
            Application.targetFrameRate=60;
            font=Resources.Load<Font>("NotoSansKR") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cameraObject=new GameObject("Ecclesia Camera");view=cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();view.orthographic=true;view.orthographicSize=9;
            view.backgroundColor=ChapelWorld.Hex("#282D25");view.clearFlags=CameraClearFlags.SolidColor;
            view.transform.position=new Vector3(9,16,-10);
            pixels=cameraObject.AddComponent<PixelViewport>();pixels.Initialize(view);
            world=new GameObject("Chapel World").AddComponent<ChapelWorld>();world.Build(false);
            visitor=new GameObject("Visitor").AddComponent<Visitor>();visitor.Initialize();visitor.Place(world.Spawn,false);
            visitor.Arrived+=OnArrived;visitor.gameObject.SetActive(false);
            auth=gameObject.AddComponent<AuthenticationService>();auth.Changed+=RefreshAuth;
            BuildUi();RefreshAuth();
        }
        void Update()
        {
            // Preserve the complete map on tablets as well as wide phones.
            view.orthographicSize=16f;
            var safe=Screen.safeArea;safeArea.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);
            safeArea.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
            if(!entered || prayerPanel.activeSelf)return;
            bool pressed=Touchscreen.current!=null&&Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            Vector2 screen=pressed?Touchscreen.current.primaryTouch.position.ReadValue():Vector2.zero;
            if(!pressed&&Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame){pressed=true;screen=Mouse.current.position.ReadValue();}
            if(!pressed)return;
            var pointer=new PointerEventData(EventSystem.current){position=screen};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);if(hits.Count>0)return;
            if(pixels.TryWorldPoint(screen,out var p))RequestMove(world.ResolveTouch(p));
        }
        public void EnterOffline() => Enter();
        void Enter()
        {
            if(auth.Busy)return;
            entered=true;loginPanel.SetActive(false);hud.SetActive(true);visitor.gameObject.SetActive(true);
            hint.text="길이나 잔디를 터치해 자유롭게 걸어가세요. 교회 문을 누르면 입장합니다.";
            location.text="교회 뜰  /  혼자 둘러보기";
        }
        public bool RequestMove(Vector2Int cell)
        {
            if(!entered||transitioning)return false;
            if(!world.Navigation.IsOpen(cell)){hint.text="이곳은 지나갈 수 없습니다. 길·잔디 또는 예배당 안쪽을 선택해주세요.";return false;}
            destination=cell;pendingPrayer="";prayerPanel.SetActive(false);prayerButton.interactable=false;seatLabel.text="";SetButtonText(prayerButton,"기도 시작");
            hint.text="천천히 걸어가고 있습니다.";
            if(visitor.MoveTo(world.Navigation,cell))return true;
            hint.text="그곳으로 이동할 수 없습니다. 통로를 선택해주세요.";return false;
        }
        void OnArrived()
        {
            if(visitor.Cell==world.Door){SwitchRoom();return;}
            bool nearby=CanPrayHere;
            prayerButton.interactable=nearby;
            seatLabel.text=nearby?"장의자 근처":"";
            hint.text=nearby?"이곳에서 기도할 수 있습니다. 준비되면 기도를 시작하세요.":world.Indoors?"장의자 근처로 이동하면 기도할 수 있습니다.":"교회 문을 터치해 들어가세요.";
        }
        bool CanPrayHere => entered && !transitioning && !visitor.Moving && world.IsNearPew(visitor.transform.position);

        public void SwitchRoom()
        {
            transitioning=true;visitor.Stop();world.Build(!world.Indoors);visitor.Place(world.Spawn,world.Indoors);
            pendingPrayer="";seatLabel.text="";prayerButton.interactable=false;
            location.text=(world.Indoors?"예배당":"교회 뜰")+"  /  혼자 둘러보기";
            hint.text=world.Indoors?"장의자 근처 어디에서든 기도할 수 있습니다. 아래쪽 문으로 나갈 수 있습니다.":"빛이 새어 나오는 교회 문으로 들어가세요.";
            transitioning=false;
        }
        void ShowPrayer()
        {
            if(!CanPrayHere)return;
            if(visitor.Praying){visitor.SetPrayer(false);pendingPrayer="";hint.text="기도를 마쳤습니다.";SetButtonText(prayerButton,"기도 시작");return;}
            prayerInput.text="";prayerPanel.SetActive(true);
        }
        public void BeginPrayer()
        {
            if(!CanPrayHere)return;
            pendingPrayer=prayerInput.text.Trim();prayerPanel.SetActive(false);visitor.SetPrayer(true);
            hint.text="기도 중입니다. 이 시제품의 기도제목은 서버에 저장하거나 공개하지 않습니다.";
            SetButtonText(prayerButton,"기도 마치기");
        }
        void Leave()
        {
            visitor.Stop();visitor.gameObject.SetActive(false);entered=false;pendingPrayer="";
            world.Build(false);visitor.Place(world.Spawn,false);hud.SetActive(false);prayerPanel.SetActive(false);loginPanel.SetActive(true);RefreshAuth();
        }
        void RefreshAuth()
        {
            if(!status)return;status.text=auth.Status;googleButton.interactable=auth.Ready&&!auth.Busy&&!auth.SignedIn;
            enterButton.gameObject.SetActive(auth.SignedIn);signOutButton.gameObject.SetActive(auth.SignedIn);RefreshOfflineButton();
        }
        Button offlineButton;
        void RefreshOfflineButton(){if(offlineButton)offlineButton.interactable=!auth.Busy;}
        void OnApplicationPause(bool pause)
        {
            if(!pause||!entered)return;visitor.Stop();pendingPrayer="";prayerPanel.SetActive(false);
            visitor.Place(world.Spawn,world.Indoors);prayerButton.interactable=false;SetButtonText(prayerButton,"기도 시작");
            hint.text="앱이 잠시 중단되어 기도를 마쳤습니다. 다시 장의자 근처로 이동해주세요.";
        }
        void BuildUi()
        {
            var canvas=new GameObject("Ecclesia UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.matchWidthOrHeight=0f;
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            safeArea=Rect("Safe area",canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            loginPanel=Panel("Welcome",safeArea,new Vector2(.5f,.5f),new Vector2(600,480),"#383B2E");
            Label(loginPanel.transform,"ECCLESIA",new Vector2(0,178),new Vector2(540,35),19,"#D7BC88");
            Label(loginPanel.transform,"에끌레시아",new Vector2(0,118),new Vector2(540,70),42,"#F4EADB");
            Label(loginPanel.transform,"고요한 공간에서 함께 드리는 기도",new Vector2(0,61),new Vector2(540,45),21,"#D4CCB7");
            status=Label(loginPanel.transform,"",new Vector2(0,3),new Vector2(520,62),17,"#D4CCB7");
            googleButton=Button(loginPanel.transform,"Google로 로그인",new Vector2(0,-65),new Vector2(460,52),()=>auth.SignIn());
            offlineButton=Button(loginPanel.transform,"오프라인 둘러보기",new Vector2(0,-127),new Vector2(460,52),EnterOffline);
            enterButton=Button(loginPanel.transform,"로그인 계정으로 둘러보기",new Vector2(0,-65),new Vector2(460,52),Enter);
            signOutButton=Button(loginPanel.transform,"로그아웃",new Vector2(211,202),new Vector2(136,38),()=>auth.SignOut());
            Label(loginPanel.transform,"개발 시제품 · 현재 다른 접속자는 표시되지 않습니다",new Vector2(0,-198),new Vector2(540,45),15,"#9FAFA9");
            hud=new GameObject("HUD",typeof(RectTransform));hud.transform.SetParent(safeArea,false);Stretch(hud.GetComponent<RectTransform>());
            var top=Rect("Top bar",hud.transform,new Vector2(0,1),Vector2.one,new Vector2(0,-100),Vector2.zero);
            top.gameObject.AddComponent<Image>().color=ChapelWorld.Hex("#383B2E");
            location=Label(top,"",new Vector2(-65,0),new Vector2(470,60),23,"#F4EADB");
            Button(top,"나가기",new Vector2(265,0),new Vector2(130,48),Leave);
            var bottom=Rect("Bottom bar",hud.transform,Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,150));
            bottom.gameObject.AddComponent<Image>().color=ChapelWorld.Hex("#383B2E");
            hint=Label(bottom,"",new Vector2(0,32),new Vector2(650,58),18,"#E5DCC8");
            seatLabel=Label(bottom,"",new Vector2(-155,-35),new Vector2(200,52),18,"#D7BC88");
            prayerButton=Button(bottom,"기도 시작",new Vector2(155,-35),new Vector2(230,52),ShowPrayer);prayerButton.interactable=false;
            hud.SetActive(false);
            prayerPanel=Panel("Prayer",safeArea,new Vector2(.5f,.5f),new Vector2(600,400),"#383B2E");
            Label(prayerPanel.transform,"기도를 준비합니다",new Vector2(0,140),new Vector2(530,55),30,"#F4EADB");
            Label(prayerPanel.transform,"기도제목은 비워도 괜찮습니다. 이 기기에서만 사용됩니다.",new Vector2(0,85),new Vector2(540,50),16,"#D4CCB7");
            var field=Rect("Prayer topic",prayerPanel.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-260,-65),new Vector2(260,55));
            field.gameObject.AddComponent<Image>().color=ChapelWorld.Hex("#E6E0D4");
            prayerInput=field.gameObject.AddComponent<InputField>();prayerInput.lineType=InputField.LineType.MultiLineNewline;prayerInput.characterLimit=300;
            var inputText=Label(field,"",Vector2.zero,new Vector2(490,100),19,"#383B2E");inputText.alignment=TextAnchor.UpperLeft;inputText.supportRichText=false;
            prayerInput.textComponent=inputText;
            Button(prayerPanel.transform,"취소",new Vector2(-135,-135),new Vector2(240,52),()=>prayerPanel.SetActive(false));
            Button(prayerPanel.transform,"기도 시작",new Vector2(135,-135),new Vector2(240,52),BeginPrayer);prayerPanel.SetActive(false);
        }
        RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;return r;}
        void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        GameObject Panel(string name,Transform parent,Vector2 anchor,Vector2 size,string color)
        {var r=Rect(name,parent,anchor,anchor,-size/2,size/2);r.gameObject.AddComponent<Image>().color=ChapelWorld.Hex(color);return r.gameObject;}
        Text Label(Transform parent,string text,Vector2 position,Vector2 size,int fontSize,string color)
        {var r=Rect("Text",parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),position-size/2,position+size/2);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=fontSize;t.color=ChapelWorld.Hex(color);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        Button Button(Transform parent,string title,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {var r=Rect(title,parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),position-size/2,position+size/2);var i=r.gameObject.AddComponent<Image>();i.color=ChapelWorld.Hex("#B9A075");var b=r.gameObject.AddComponent<Button>();b.targetGraphic=i;b.onClick.AddListener(action);Label(r,title,Vector2.zero,size,20,"#282D25");return b;}
        void SetButtonText(Button b,string title)=>b.GetComponentInChildren<Text>().text=title;
        void OnDestroy(){if(auth)auth.Changed-=RefreshAuth;}
    }
}
