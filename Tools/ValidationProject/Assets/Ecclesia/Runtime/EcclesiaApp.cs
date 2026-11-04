using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Ecclesia
{
    public sealed class EcclesiaApp : MonoBehaviour
    {
        ChapelWorld world; Visitor visitor; AuthenticationService auth; Camera view;
        RectTransform safeArea; GameObject loginPanel, prayerPanel, hud;
        Text status, hint, location, seatLabel; Button googleButton, enterButton, prayerButton, signOutButton;
        InputField prayerInput; Font font; bool entered, transitioning; int selectedSeat;
        Vector2Int destination; string pendingPrayer="";

        void Awake()
        {
            Application.targetFrameRate=60;
            font=Resources.Load<Font>("NotoSansKR") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cameraObject=new GameObject("Ecclesia Camera");view=cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();view.orthographic=true;view.orthographicSize=9;
            view.backgroundColor=ChapelWorld.Hex("#202D32");view.clearFlags=CameraClearFlags.SolidColor;
            view.transform.position=new Vector3(16,9,-10);
            world=new GameObject("Chapel World").AddComponent<ChapelWorld>();world.Build(false);
            visitor=new GameObject("Visitor").AddComponent<Visitor>();visitor.Initialize();visitor.Place(world.Spawn,false);
            visitor.Arrived+=OnArrived;visitor.gameObject.SetActive(false);
            auth=gameObject.AddComponent<AuthenticationService>();auth.Changed+=RefreshAuth;
            BuildUi();RefreshAuth();
        }
        void Update()
        {
            // Preserve the complete map on tablets as well as wide phones.
            view.orthographicSize=Mathf.Max(10.8f,17.5f/view.aspect);
            var safe=Screen.safeArea;safeArea.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);
            safeArea.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
            if(!entered || prayerPanel.activeSelf)return;
            bool pressed=Touchscreen.current!=null&&Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            Vector2 screen=pressed?Touchscreen.current.primaryTouch.position.ReadValue():Vector2.zero;
            if(!pressed&&Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame){pressed=true;screen=Mouse.current.position.ReadValue();}
            if(!pressed)return;
            var pointer=new PointerEventData(EventSystem.current){position=screen};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);if(hits.Count>0)return;
            var p=view.ScreenToWorldPoint(screen);RequestMove(world.ResolveTouch(p));
        }
        public void EnterOffline() => Enter();
        void Enter()
        {
            if(auth.Busy)return;
            entered=true;loginPanel.SetActive(false);hud.SetActive(true);visitor.gameObject.SetActive(true);
            hint.text="길을 터치해 걸어가세요. 빛이 새어 나오는 교회 문으로 들어갑니다.";
            location.text="교회 뜰  /  혼자 둘러보기";
        }
        public bool RequestMove(Vector2Int cell)
        {
            if(!entered||transitioning)return false;
            if(!world.Navigation.IsOpen(cell)){hint.text="통로 또는 장의자의 빈자리를 터치해주세요.";return false;}
            destination=cell;selectedSeat=0;pendingPrayer="";prayerPanel.SetActive(false);prayerButton.interactable=false;seatLabel.text="";SetButtonText(prayerButton,"기도 시작");
            hint.text="천천히 걸어가고 있습니다.";
            if(visitor.MoveTo(world.Navigation,cell))return true;
            hint.text="그곳으로 이동할 수 없습니다. 통로를 선택해주세요.";return false;
        }
        void OnArrived()
        {
            if(visitor.Cell==world.Door){SwitchRoom();return;}
            if(world.Seats.TryGetValue(visitor.Cell,out selectedSeat))
            {seatLabel.text=$"{selectedSeat:00}번 자리";hint.text="자리에 도착했습니다. 준비되면 기도를 시작하세요.";prayerButton.interactable=true;}
            else{selectedSeat=0;seatLabel.text="";hint.text=world.Indoors?"장의자의 좌석을 터치해 자리를 잡으세요.":"교회 문을 터치해 들어가세요.";}
        }
        public void SwitchRoom()
        {
            transitioning=true;visitor.Stop();world.Build(!world.Indoors);visitor.Place(world.Spawn,world.Indoors);
            selectedSeat=0;pendingPrayer="";seatLabel.text="";prayerButton.interactable=false;
            location.text=(world.Indoors?"예배당":"교회 뜰")+"  /  혼자 둘러보기";
            hint.text=world.Indoors?"장의자의 좌석을 터치하세요. 아래쪽 문으로 나갈 수 있습니다.":"빛이 새어 나오는 교회 문으로 들어가세요.";
            transitioning=false;
        }
        void ShowPrayer()
        {
            if(selectedSeat==0||visitor.Moving)return;
            if(visitor.Praying){visitor.SetPrayer(false);pendingPrayer="";hint.text="기도를 마쳤습니다.";SetButtonText(prayerButton,"기도 시작");return;}
            prayerInput.text="";prayerPanel.SetActive(true);
        }
        public void BeginPrayer()
        {
            if(selectedSeat==0||visitor.Moving)return;
            pendingPrayer=prayerInput.text.Trim();prayerPanel.SetActive(false);visitor.SetPrayer(true);
            hint.text="기도 중입니다. 이 시제품의 기도제목은 서버에 저장하거나 공개하지 않습니다.";
            SetButtonText(prayerButton,"기도 마치기");
        }
        void Leave()
        {
            visitor.Stop();visitor.gameObject.SetActive(false);entered=false;selectedSeat=0;pendingPrayer="";
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
            if(!pause||!entered)return;visitor.Stop();pendingPrayer="";selectedSeat=0;prayerPanel.SetActive(false);
            visitor.Place(world.Spawn,world.Indoors);prayerButton.interactable=false;SetButtonText(prayerButton,"기도 시작");
            hint.text="앱이 잠시 중단되어 기도를 마쳤습니다. 다시 자리를 선택해주세요.";
        }
        void BuildUi()
        {
            var canvas=new GameObject("Ecclesia UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            safeArea=Rect("Safe area",canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            loginPanel=Panel("Welcome",safeArea,new Vector2(.5f,.5f),new Vector2(600,480),"#203137");
            Label(loginPanel.transform,"ECCLESIA",new Vector2(0,178),new Vector2(540,35),19,"#D7BC88");
            Label(loginPanel.transform,"에끌레시아",new Vector2(0,118),new Vector2(540,70),42,"#F4EADB");
            Label(loginPanel.transform,"고요한 공간에서 함께 드리는 기도",new Vector2(0,61),new Vector2(540,45),21,"#C4D0C9");
            status=Label(loginPanel.transform,"",new Vector2(0,3),new Vector2(520,62),17,"#C4D0C9");
            googleButton=Button(loginPanel.transform,"Google로 로그인",new Vector2(0,-65),new Vector2(460,52),()=>auth.SignIn());
            offlineButton=Button(loginPanel.transform,"오프라인 둘러보기",new Vector2(0,-127),new Vector2(460,52),EnterOffline);
            enterButton=Button(loginPanel.transform,"로그인 계정으로 둘러보기",new Vector2(0,-65),new Vector2(460,52),Enter);
            signOutButton=Button(loginPanel.transform,"로그아웃",new Vector2(211,202),new Vector2(136,38),()=>auth.SignOut());
            Label(loginPanel.transform,"개발 시제품 · 현재 다른 접속자는 표시되지 않습니다",new Vector2(0,-198),new Vector2(540,45),15,"#9FAFA9");
            hud=new GameObject("HUD",typeof(RectTransform));hud.transform.SetParent(safeArea,false);Stretch(hud.GetComponent<RectTransform>());
            var top=Rect("Top bar",hud.transform,new Vector2(0,1),Vector2.one,new Vector2(0,-76),Vector2.zero);
            top.gameObject.AddComponent<Image>().color=ChapelWorld.Hex("#203137");
            location=Label(top,"",new Vector2(-350,0),new Vector2(480,60),23,"#F4EADB");
            Button(top,"나가기",new Vector2(500,0),new Vector2(150,48),Leave);
            var bottom=Rect("Bottom bar",hud.transform,Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,92));
            bottom.gameObject.AddComponent<Image>().color=ChapelWorld.Hex("#203137");
            hint=Label(bottom,"",new Vector2(-160,0),new Vector2(790,70),17,"#D9E0D8");
            seatLabel=Label(bottom,"",new Vector2(300,0),new Vector2(130,60),18,"#D7BC88");
            prayerButton=Button(bottom,"기도 시작",new Vector2(490,0),new Vector2(170,52),ShowPrayer);prayerButton.interactable=false;
            hud.SetActive(false);
            prayerPanel=Panel("Prayer",safeArea,new Vector2(.5f,.5f),new Vector2(600,400),"#203137");
            Label(prayerPanel.transform,"기도를 준비합니다",new Vector2(0,140),new Vector2(530,55),30,"#F4EADB");
            Label(prayerPanel.transform,"기도제목은 비워도 괜찮습니다. 이 기기에서만 사용됩니다.",new Vector2(0,85),new Vector2(540,50),16,"#C4D0C9");
            var field=Rect("Prayer topic",prayerPanel.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-260,-65),new Vector2(260,55));
            field.gameObject.AddComponent<Image>().color=ChapelWorld.Hex("#E6E0D4");
            prayerInput=field.gameObject.AddComponent<InputField>();prayerInput.lineType=InputField.LineType.MultiLineNewline;prayerInput.characterLimit=300;
            var inputText=Label(field,"",Vector2.zero,new Vector2(490,100),19,"#203137");inputText.alignment=TextAnchor.UpperLeft;inputText.supportRichText=false;
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
        {var r=Rect(title,parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),position-size/2,position+size/2);var i=r.gameObject.AddComponent<Image>();i.color=ChapelWorld.Hex("#B9A075");var b=r.gameObject.AddComponent<Button>();b.targetGraphic=i;b.onClick.AddListener(action);Label(r,title,Vector2.zero,size,20,"#202D32");return b;}
        void SetButtonText(Button b,string title)=>b.GetComponentInChildren<Text>().text=title;
        void OnDestroy(){if(auth)auth.Changed-=RefreshAuth;}
    }
}
