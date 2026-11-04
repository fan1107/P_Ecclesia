using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ecclesia
{
    public sealed class Visitor : MonoBehaviour
    {
        public Vector2Int Cell { get; private set; }
        public bool Moving => route.Count > 0;
        public bool Praying { get; private set; }
        public event Action Arrived;
        readonly Queue<Vector2Int> route = new Queue<Vector2Int>();
        SpriteRenderer body, beam;
        AudioSource footsteps;
        AudioReverbFilter reverb;
        AudioClip step;
        float stepTime;
        bool ownsBodyTexture;
        public void Initialize()
        {
            body=gameObject.AddComponent<SpriteRenderer>();body.sortingOrder=50;
            var portrait=Resources.Load<Texture2D>("Art/VisitorPortrait");
            if(portrait)
            {
                int minX=portrait.width,minY=portrait.height,maxX=0,maxY=0;
                var pixels=portrait.GetPixels32();
                for(int y=0;y<portrait.height;y++)for(int x=0;x<portrait.width;x++)
                    if(pixels[y*portrait.width+x].a>64){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
                body.sprite=Sprite.Create(portrait,new Rect(minX,minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,.08f),(maxY-minY+1)/1.5f);
            }
            else {body.sprite=PixelArt.Character();ownsBodyTexture=true;}
            var light=new GameObject("Prayer light");light.transform.SetParent(transform,false); light.transform.localPosition=new Vector3(0,2,0);light.transform.localScale=new Vector3(1.2f,1,1);
            beam=light.AddComponent<SpriteRenderer>();beam.sprite=CreatePrayerLight();beam.color=new Color(1,.9f,.6f,.3f);beam.sortingOrder=49;light.SetActive(false);
            footsteps=gameObject.AddComponent<AudioSource>();footsteps.playOnAwake=false;footsteps.volume=.18f;
            reverb=gameObject.AddComponent<AudioReverbFilter>();
            var samples=new float[4410]; var random=new System.Random(42);
            for(int i=0;i<samples.Length;i++) { float t=i/44100f; samples[i]=(float)((random.NextDouble()*2-1)*.6+Math.Sin(t*2*Math.PI*130)*.4)*Mathf.Exp(-t*65); }
            step=AudioClip.Create("Soft footstep prototype",samples.Length,1,44100,false);step.SetData(samples,0);
        }
        public void Place(Vector2Int p, bool indoors)
        {
            Stop();Cell=p;transform.position=new Vector3(p.x+.5f,p.y+.5f,0);
            reverb.reverbPreset=indoors?AudioReverbPreset.StoneCorridor:AudioReverbPreset.Off;
        }
        public bool MoveTo(WalkGrid grid,Vector2Int target)
        {
            // Finish the current segment before taking a new route: never cut a corner through a pew.
            var start=route.Count>0?route.Peek():Cell;
            var path=grid.FindPath(start,target); if(path==null)return false;
            bool inSegment=route.Count>0;route.Clear();if(inSegment)route.Enqueue(start);
            foreach(var p in path)route.Enqueue(p);
            SetPrayer(false);if(route.Count==0)Arrived?.Invoke();return true;
        }
        public void Stop() { route.Clear();SetPrayer(false); if(body)body.transform.localScale=Vector3.one; }
        public void SetPrayer(bool value) { Praying=value;if(beam)beam.gameObject.SetActive(value); }
        void Update()
        {
            if(Praying)beam.color=new Color(1,.9f,.6f,.3f+.045f*Mathf.Sin(Time.time*1.4f));
            if(!Moving)return;
            var p=route.Peek();var destination=new Vector3(p.x+.5f,p.y+.5f,0);
            body.flipX=destination.x<transform.position.x;
            transform.position=Vector3.MoveTowards(transform.position,destination,3.5f*Time.deltaTime);
            transform.localScale=new Vector3(1,1+.035f*Mathf.Sin(Time.time*20),1);
            stepTime-=Time.deltaTime;if(stepTime<=0){footsteps.pitch=UnityEngine.Random.Range(.9f,1.1f);footsteps.PlayOneShot(step);stepTime=.36f;}
            if(Vector3.Distance(transform.position,destination)<.001f){Cell=route.Dequeue();if(!Moving){transform.localScale=Vector3.one;Arrived?.Invoke();}}
        }
        void OnApplicationPause(bool paused) { if(paused){Stop();transform.position=new Vector3(Cell.x+.5f,Cell.y+.5f,0);} }
        static Sprite CreatePrayerLight()
        {
            var texture=new Texture2D(16,64){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var colors=new Color[16*64];
            for(int y=0;y<64;y++)for(int x=0;x<16;x++)
            {
                float edge=1-Mathf.Abs((x-7.5f)/8f);
                float fade=Mathf.Sin((y+.5f)/64f*Mathf.PI);
                colors[y*16+x]=new Color(1,1,1,edge*fade);
            }
            texture.SetPixels(colors);texture.Apply();
            return Sprite.Create(texture,new Rect(0,0,16,64),new Vector2(.5f,.5f),16);
        }
        void OnDestroy(){if(step)Destroy(step);if(beam&&beam.sprite){Destroy(beam.sprite.texture);Destroy(beam.sprite);}if(body&&body.sprite){if(ownsBodyTexture)Destroy(body.sprite.texture);Destroy(body.sprite);}}
    }
}
