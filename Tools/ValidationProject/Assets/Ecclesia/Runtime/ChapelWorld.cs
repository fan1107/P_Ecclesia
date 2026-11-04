using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Ecclesia
{
    public sealed class ChapelWorld : MonoBehaviour
    {
        public WalkGrid Navigation { get; private set; }
        public readonly Dictionary<Vector2Int, int> Seats = new Dictionary<Vector2Int, int>();
        public bool Indoors { get; private set; }
        public Vector2Int Door => Indoors ? new Vector2Int(16, 1) : new Vector2Int(16, 12);
        public Vector2Int Spawn => Indoors ? new Vector2Int(16, 3) : new Vector2Int(16, 3);
        GameObject room;
        readonly List<Object> generated = new List<Object>();

        public Vector2Int ResolveTouch(Vector2 p)
        {
            if(!Indoors && p.x>=14.75f && p.x<=18.25f && p.y>=12 && p.y<=15)return Door;
            if(Indoors && p.x>=15 && p.x<=18 && p.y>=1 && p.y<=2)return Door;
            var result=new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y));
            float closest=float.MaxValue;
            foreach(var seat in Seats.Keys)
            {
                var delta=p-new Vector2(seat.x+.5f,seat.y+.5f);
                if(Mathf.Abs(delta.x)>1.5f||Mathf.Abs(delta.y)>.8f||delta.sqrMagnitude>=closest)continue;
                closest=delta.sqrMagnitude;result=seat;
            }
            return result;
        }

        public void Build(bool indoors)
        {
            if (room) { room.SetActive(false); Release(room); }
            foreach (var asset in generated) Release(asset);
            generated.Clear(); Seats.Clear(); Indoors = indoors; Navigation = new WalkGrid();
            room = new GameObject(indoors ? "Sanctuary" : "Courtyard"); room.transform.SetParent(transform);
            var grid = room.AddComponent<Grid>();
            var tiles = new GameObject("Pixel tile floor"); tiles.transform.SetParent(grid.transform);
            var map = tiles.AddComponent<Tilemap>(); tiles.AddComponent<TilemapRenderer>().sortingOrder = -20;
            var floorA = Tile(indoors ? "#69594A" : "#4A6650", indoors ? "#766453" : "#526F54");
            var floorB = Tile(indoors ? "#635446" : "#47624D", indoors ? "#6E5D4E" : "#506B52");
            var path = Tile("#B4A58A", "#C4B597");
            for (int x = 0; x < WalkGrid.Width; x++) for (int y = 0; y < WalkGrid.Height; y++)
            {
                map.SetTile(new Vector3Int(x, y), (x + y) % 2 == 0 ? floorA : floorB);
                Navigation.Set(x, y, x > 1 && x < 30 && y > 0 && y < (indoors ? 15 : 13));
                if (!indoors && x >= 14 && x <= 18 && y < 13) map.SetTile(new Vector3Int(x, y), path);
            }
            if (indoors) BuildInterior(); else BuildCourtyard();
        }
        void BuildInterior()
        {
            Rect("Back wall", 16, 16.2f, 32, 3.6f, "#303B40", 0);
            Rect("Wall trim", 16, 14.6f, 32, .18f, "#C0A875", 1);
            Rect("Left wall", .8f, 8, 2.2f, 16, "#303B40", 1);
            Rect("Right wall", 31.2f, 8, 2.2f, 16, "#303B40", 1);
            Rect("Aisle carpet", 16.5f, 8, 3, 12, "#5B4047", -10);
            Rect("Altar step", 16.5f, 13.4f, 10, 1.1f, "#B3A285", 0);
            Rect("Altar table", 16.5f, 14, 4.5f, .7f, "#D5C8A7", 2);
            Rect("Cross vertical", 16.5f, 16, .22f, 2, "#F3DDA3", 3);
            Rect("Cross horizontal", 16.5f, 16.4f, 1.25f, .22f, "#F3DDA3", 3);
            for (int side = 0; side < 2; side++)
            {
                var wx = side == 0 ? 6 : 26;
                Rect("Window frame", wx, 16.1f, 2.4f, 2.2f, "#192B32", 2);
                Rect("Amber glass", wx - .5f, 16.1f, .65f, 1.7f, "#B48D62", 3);
                Rect("Blue glass", wx + .5f, 16.1f, .65f, 1.7f, "#729999", 3);
                for (int row = 0; row < 5; row++)
                {
                    int y = 3 + row * 2;
                    int x = side == 0 ? 7 : 21;
                    Rect("Pew shadow", x + 1.5f, y + .35f, 5.6f, 1.1f, "#3C3532", 0);
                    Rect("Pew seat", x + 1.5f, y + .6f, 5.2f, .75f, "#A0784F", 1);
                    Rect("Pew back", x + 1.5f, y + 1.15f, 5.4f, .3f, "#C29662", 2);
                    for (int bx = x - 1; bx <= x + 3; bx++) Navigation.Set(bx, y + 1, false);
                    Seats[new Vector2Int(x, y)] = side * 10 + row * 2 + 1;
                    Seats[new Vector2Int(x + 2, y)] = side * 10 + row * 2 + 2;
                }
            }
            Rect("Exit mat", 16.5f, 1.5f, 3, 1, "#C2AD83", -1);
        }
        void BuildCourtyard()
        {
            Rect("Chapel facade", 16.5f, 15, 19, 4.8f, "#D0C3A6", 0);
            Rect("Roof shadow", 16.5f, 17, 21, 1, "#4A4B55", 1);
            Rect("Door surround", 16.5f, 13.5f, 3.5f, 3, "#84664C", 2);
            Rect("Open door", 16.5f, 13.3f, 2.5f, 2.6f, "#293239", 3);
            Rect("Door light", 16.5f, 12.25f, 2.5f, .25f, "#F0D89A", 4);
            Rect("Cross", 16.5f, 16.2f, .18f, 1.4f, "#7B6349", 4);
            Rect("Cross arms", 16.5f, 16.5f, .9f, .18f, "#7B6349", 4);
            foreach (int x in new[] { 10, 23 })
            {
                Rect("Window", x, 14.8f, 1.7f, 1.7f, "#758D8D", 2);
                Rect("Mullion", x, 14.8f, .12f, 1.7f, "#E3D6B7", 3);
            }
            foreach (var p in new[] { new Vector2Int(5, 5), new Vector2Int(26, 5), new Vector2Int(7, 10), new Vector2Int(25, 10) })
            {
                Rect("Tree trunk", p.x + .5f, p.y + .5f, .5f, 1.3f, "#6D5843", 3);
                Rect("Tree foliage", p.x + .5f, p.y + 1.7f, 2.7f, 2, "#294C40", 4);
                Rect("Tree highlight", p.x + .2f, p.y + 2.3f, 2, .7f, "#648067", 5);
                Navigation.Set(p.x, p.y, false);
            }
        }
        Tile Tile(string color, string detail)
        {
            var tex = new Texture2D(16,16) { filterMode = FilterMode.Point };
            var pixels = new Color[256]; var a = Hex(color); var b = Hex(detail);
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) pixels[y * 16 + x] = y == 0 || (x * 7 + y * 11) % 37 == 0 ? b : a;
            tex.SetPixels(pixels); tex.Apply();
            var sprite = Sprite.Create(tex,new Rect(0,0,16,16),Vector2.zero,16);
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = sprite;
            generated.Add(tex); generated.Add(sprite); generated.Add(tile); return tile;
        }
        void Rect(string label, float x,float y,float w,float h,string color,int order)
        {
            var go = new GameObject(label); go.transform.SetParent(room.transform); go.transform.position = new Vector3(x,y,0);
            go.transform.localScale = new Vector3(w,h,1);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = PixelArt.White; sr.color = Hex(color); sr.sortingOrder = order;
        }
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex,out var c); return c; }
        static void Release(Object obj) { if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj); }
        void OnDestroy() { foreach (var asset in generated) if(asset) Release(asset); }
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
