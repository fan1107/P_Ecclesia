using System.Collections.Generic;
using UnityEngine;

namespace Ecclesia
{
    public sealed class ChapelWorld : MonoBehaviour
    {
        public WalkGrid Navigation { get; private set; }
        public readonly Dictionary<Vector2Int,int> Seats = new Dictionary<Vector2Int,int>();
        public bool Indoors { get; private set; }
        public Vector2Int Door => Indoors ? new Vector2Int(9,2) : new Vector2Int(9,19);
        public Vector2Int Spawn => new Vector2Int(9,4);
        GameObject room;
        Sprite backgroundSprite;

        public Vector2Int ResolveTouch(Vector2 p)
        {
            if(!Indoors && p.x>=7 && p.x<=11 && p.y>=18 && p.y<=24)return Door;
            if(Indoors && p.x>=7 && p.x<=11 && p.y<4)return Door;
            return new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y));
        }
        public bool IsNearPew(Vector2 position) => Indoors && PrayerArea.Contains(position.x,position.y);
        public void Build(bool indoors)
        {
            if(room){room.SetActive(false);Release(room);}
            if(backgroundSprite)Release(backgroundSprite);
            Seats.Clear();Indoors=indoors;Navigation=new WalkGrid();
            room=new GameObject(indoors?"Sanctuary portrait":"Courtyard portrait");room.transform.SetParent(transform);
            var texture=Resources.Load<Texture2D>(indoors?"Art/SanctuaryConcept":"Art/CourtyardOrthographic");
            if(texture)
            {
                backgroundSprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),16);
                var art=new GameObject("Approved concept environment");art.transform.SetParent(room.transform);
                art.transform.position=new Vector3(9,16,0);
                art.transform.localScale=new Vector3(18/backgroundSprite.bounds.size.x,32/backgroundSprite.bounds.size.y,1);
                var renderer=art.AddComponent<SpriteRenderer>();renderer.sprite=backgroundSprite;renderer.sortingOrder=-20;
            }
            else Debug.LogError("Missing portrait environment art");
            if(!indoors){CourtyardLayout.Apply(Navigation);return;}
            for(int x=0;x<WalkGrid.Width;x++)for(int y=0;y<WalkGrid.Height;y++)
                Navigation.Set(x,y,x>=2 && x<=15 && y>=2 && y<=22);
            for(int side=0;side<2;side++)for(int row=0;row<5;row++)
            {
                int y=new[]{7,10,12,15,18}[row];int x=side==0?4:11;
                Seats[new Vector2Int(x,y)]=side*10+row*2+1;
                Seats[new Vector2Int(x+2,y)]=side*10+row*2+2;
            }
        }
        public static Color Hex(string hex){ColorUtility.TryParseHtmlString(hex,out var color);return color;}
        static void Release(Object obj){if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}
        void OnDestroy(){if(backgroundSprite)Release(backgroundSprite);}
    }
    public static class PixelArt
    {
        static Sprite white;
        public static Sprite White
        {
            get
            {
                if (white) return white;
                var t = new Texture2D(1,1) { filterMode = FilterMode.Point }; t.SetPixel(0,0,Color.white); t.Apply();
                return white = Sprite.Create(t,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
            }
        }
        public static Sprite Character()
        {
            string[] rows = { "....hhhh....", "...hhhhhh...", "...hssss h..".Replace(" ",""), "...ssssss...", "....ssss....", "...bbbbbb...", "..sbbbb bbs.".Replace(" ",""), "..sbbbbbbs..", "...bbbbbb...", "....bbbb....", "....d.dd....", "...dd.dd...." };
            var t = new Texture2D(12,12) { filterMode = FilterMode.Point };
            var colors = new Color[144];
            for(int y=0;y<12;y++) for(int x=0;x<12;x++)
            {
                char c = x < rows[y].Length ? rows[y][x] : '.';
                colors[(11-y)*12+x] = c=='h'?ChapelWorld.Hex("#453B36"):c=='s'?ChapelWorld.Hex("#D9B18B"):c=='b'?ChapelWorld.Hex("#B7C7C1"):c=='d'?ChapelWorld.Hex("#303D45"):Color.clear;
            }
            t.SetPixels(colors);t.Apply(); return Sprite.Create(t,new Rect(0,0,12,12),new Vector2(.5f,.15f),12);
        }
    }
}
